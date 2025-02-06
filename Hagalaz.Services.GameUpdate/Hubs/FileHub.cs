using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hagalaz.Services.GameUpdate.Network;
using Hagalaz.Services.GameUpdate.Network.Messages;
using Hagalaz.Services.GameUpdate.Services;
using Raido.Common.Protocol;
using Raido.Server;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameUpdate.Hubs
{
    public class FileHub : RaidoHub
    {
        private readonly ICacheService _cacheService;

        public FileHub(ICacheService cacheService) => _cacheService = cacheService;

        [RaidoMessageHandler(typeof(PriorityFileRequest))]
        public async ValueTask<PriorityFileResponse?> ServeFile(PriorityFileRequest fileRequest)
        {
            if (!Context.TryGetItem(FileSyncSession.Key, out FileSyncSession? session) || !_cacheService.IsValidFile(fileRequest.IndexId, fileRequest.FileId))
            {
                Context.Abort();
                return null;
            }
            
            var data = await _cacheService.ReadFileAsync(fileRequest.IndexId, fileRequest.FileId);
            return new PriorityFileResponse()
            {
                Data = data,
                IndexId = fileRequest.IndexId,
                FileId = fileRequest.FileId,
                HighPriority = fileRequest.HighPriority,
                EncryptionFlag = session!.EncryptionFlag
            };
        }

        [RaidoMessageHandler(typeof(AuthStatusChangedMessage))]
        public void AuthStatusChanged(AuthStatusChangedMessage statusChangedMessage)
        {
            var session = GetSession();
            session.Authenticated = statusChangedMessage.Authenticated;
        }

        [RaidoMessageHandler(typeof(EncryptionFlagMessage))]
        public void EncryptionFlagSet(EncryptionFlagMessage encryptionFlagMessage)
        {
            var session = GetSession();
            session.EncryptionFlag = encryptionFlagMessage.EncryptionFlag;
        }

        private FileSyncSession GetSession()
        {
            if (!Context.TryGetItem(FileSyncSession.Key, out FileSyncSession? session))
            {
                throw new ArgumentNullException(nameof(session));
            }
            return session!;
        }
    }
}