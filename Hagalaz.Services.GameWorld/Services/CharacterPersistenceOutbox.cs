using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;

namespace Hagalaz.Services.GameWorld.Services
{
    public sealed record PendingCharacterPersistence(string FilePath, UpdateCharacterRequest Request);

    public sealed class CharacterPersistenceOutbox
    {
        private readonly string _directoryPath;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public CharacterPersistenceOutbox(string directoryPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
            _directoryPath = directoryPath;
        }

        public async Task EnqueueAsync(UpdateCharacterRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            await _gate.WaitAsync(cancellationToken);
            try
            {
                Directory.CreateDirectory(_directoryPath);
                var filePath = Path.Combine(
                    _directoryPath,
                    $"{request.MasterId}-{request.SnapshotRevision}-{request.CorrelationId:N}.json");
                var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";

                try
                {
                    await using (var stream = new FileStream(
                                     temporaryPath,
                                     FileMode.CreateNew,
                                     FileAccess.Write,
                                     FileShare.None,
                                     4096,
                                     FileOptions.Asynchronous | FileOptions.WriteThrough))
                    {
                        await JsonSerializer.SerializeAsync(stream, request, cancellationToken: cancellationToken);
                        await stream.FlushAsync(cancellationToken);
                        stream.Flush(flushToDisk: true);
                    }

                    File.Move(temporaryPath, filePath, overwrite: true);
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<IReadOnlyList<PendingCharacterPersistence>> ReadAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                return await ReadCoreAsync(cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task RemoveUpToAsync(uint masterId, long snapshotRevision, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                foreach (var pending in await ReadCoreAsync(cancellationToken))
                {
                    if (pending.Request.MasterId == masterId && pending.Request.SnapshotRevision <= snapshotRevision)
                    {
                        File.Delete(pending.FilePath);
                    }
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<IReadOnlyList<PendingCharacterPersistence>> ReadCoreAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(_directoryPath))
            {
                return Array.Empty<PendingCharacterPersistence>();
            }

            var pending = new List<PendingCharacterPersistence>();
            foreach (var filePath in Directory.EnumerateFiles(_directoryPath, "*.json"))
            {
                await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                var request = await JsonSerializer.DeserializeAsync<UpdateCharacterRequest>(stream,
                    cancellationToken: cancellationToken);
                if (request is null)
                {
                    throw new InvalidDataException($"Character persistence outbox file '{filePath}' is empty.");
                }

                pending.Add(new PendingCharacterPersistence(filePath, request));
            }

            return pending
                .OrderBy(item => item.Request.MasterId)
                .ThenBy(item => item.Request.SnapshotRevision)
                .ToArray();
        }
    }
}
