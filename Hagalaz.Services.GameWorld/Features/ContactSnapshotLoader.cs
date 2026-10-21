using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Features;

internal sealed class ContactSnapshotLoader
{
    private readonly IRequestClient<GetContactsRequest> _getContactsRequestClient;
    private readonly IGameConnectionService _connectionService;
    private readonly IMapper _mapper;

    public ContactSnapshotLoader(
        IRequestClient<GetContactsRequest> getContactsRequestClient,
        IMapper mapper,
        IGameConnectionService connectionService)
    {
        _getContactsRequestClient = getContactsRequestClient;
        _mapper = mapper;
        _connectionService = connectionService;
    }

    public async Task<(IContactsFeature Contacts, long ObservationBoundary)?> OpenObservationWindowAsync(
        IGameSession session)
    {
        var connection = await _connectionService.FindById(session.ConnectionId);
        var contacts = connection?.Features.Get<IContactsFeature>();
        return contacts is null ? null : (contacts, contacts.BeginObservationWindow());
    }

    public async Task LoadAsync(
        IGameSession session,
        (IContactsFeature Contacts, long ObservationBoundary)? observationWindow,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (observationWindow is null)
        {
            return;
        }

        var (contacts, observationBoundary) = observationWindow.Value;
        try
        {
            var response = await _getContactsRequestClient.GetResponse<GetContactsResponse>(
                new GetContactsRequest(
                    session.MasterId,
                    session.SessionGeneration,
                    session.ConnectionId,
                    observationBoundary),
                cancellationToken);
            if (!ContactSnapshotApplicator.TryApply(response.Message, session, contacts, _mapper))
            {
                logger.LogWarning(
                    "Discarded contacts snapshot for stale session '{ConnectionId}' and account '{MasterId}'.",
                    session.ConnectionId,
                    session.MasterId);
            }
        }
        catch (RequestTimeoutException exception)
        {
            logger.LogWarning(
                exception,
                "Contacts snapshot timed out for account '{MasterId}'; continuing sign-in without replacing contacts.",
                session.MasterId);
        }
        catch (RequestFaultException exception)
        {
            logger.LogWarning(
                exception,
                "Contacts snapshot failed for account '{MasterId}'; continuing sign-in without replacing contacts.",
                session.MasterId);
        }
        finally
        {
            contacts.CompleteInitialSnapshot();
            contacts.EndObservationWindow();
        }
    }
}
