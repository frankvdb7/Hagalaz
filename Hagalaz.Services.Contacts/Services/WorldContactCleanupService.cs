using System;
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
            var offlineMessages = await _contactSessions.ToAsyncEnumerable()
                .Where(session => session.WorldId == worldId)
                .Select(new Func<ContactSessionContext, CancellationToken, ValueTask<ContactSignOutMessage?>>(async (session, ct) =>
                {
                    var contact = await _characterService.FindCharacterByIdAsync(session.MasterId);
                    if (contact == null)
                    {
                        return null;
                    }

                    return new ContactSignOutMessage(new ContactDto
                    {
                        MasterId = contact.MasterId,
                        DisplayName = contact.DisplayName,
                        PreviousDisplayName = contact.PreviousDisplayName
                    });
                }))
                .OfType<ContactSignOutMessage>()
                .ToListAsync(cancellationToken);

            await publishEndpoint.PublishBatch(offlineMessages, cancellationToken);
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
