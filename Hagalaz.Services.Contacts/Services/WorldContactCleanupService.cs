using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.Services.Contacts.Store.Model;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.Contacts.Services;

public sealed class WorldContactCleanupService
{
    private readonly ICharacterService _characterService;
    private readonly ContactSessionStore _contactSessions;
    private readonly ILogger<WorldContactCleanupService> _logger;

    public WorldContactCleanupService(
        ICharacterService characterService,
        ContactSessionStore contactSessions,
        ILogger<WorldContactCleanupService> logger)
    {
        _characterService = characterService;
        _contactSessions = contactSessions;
        _logger = logger;
    }

    public async Task SignOutWorldContactsAsync(
        int worldId,
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await _contactSessions.ToAsyncEnumerable()
                .Where(session => session.WorldId == worldId)
                .ToListAsync(cancellationToken);
            var offlineMessages = new List<ContactSignOutMessage>(sessions.Count);

            foreach (var session in sessions)
            {
                var contact = await _characterService.FindCharacterByIdAsync(session.MasterId);
                if (contact == null)
                {
                    continue;
                }

                offlineMessages.Add(new ContactSignOutMessage(new ContactDto
                {
                    MasterId = contact.MasterId,
                    DisplayName = contact.DisplayName,
                    PreviousDisplayName = contact.PreviousDisplayName
                }));
            }

            await publishEndpoint.PublishBatch(offlineMessages, cancellationToken);

            foreach (var session in sessions)
            {
                if (_contactSessions.TryGetValue(session.MasterId, out var current) && current == session)
                {
                    _contactSessions.TryRemove(session.MasterId);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (MassTransitException exception)
        {
            _logger.LogError(exception, "Failed to publish offline contacts for world {WorldId}", worldId);
        }
        catch (TimeoutException exception)
        {
            _logger.LogError(exception, "Timed out publishing offline contacts for world {WorldId}", worldId);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "Invalid contact state while publishing offline contacts for world {WorldId}", worldId);
        }
    }
}
