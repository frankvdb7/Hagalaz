using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Messages.Model;
using Hagalaz.Services.GameLogon.Services;
using Hagalaz.Services.GameLogon.Services.Model;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameLogon.Consumers;

public class FriendsChatConsumer : IConsumer<AddFriendsChatMemberRequest>, IConsumer<RemoveFriendsChatMemberRequest>, IConsumer<GetFriendsChatRequest>,
    IConsumer<DoFriendsChatMemberKickRequest>, IConsumer<AddFriendsChatMessageRequest>
{
    private readonly IFriendsChatService _friendsChatService;
    private readonly ICharacterService _characterService;
    private readonly ILogger<FriendsChatConsumer> _logger;

    public FriendsChatConsumer(IFriendsChatService friendsChatService, ICharacterService characterService, ILogger<FriendsChatConsumer> logger)
    {
        _friendsChatService = friendsChatService;
        _characterService = characterService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AddFriendsChatMemberRequest> context)
    {
        var message = context.Message;
        var responseCode = AddFriendsChatMemberResponse.ResponseCode.Failed;
        var result = await _friendsChatService.RegisterMemberAsync(message.OwnerDisplayName, message.CharacterId);
        var chat = await _friendsChatService.FindChatSessionBySessionIdAsync(message.CharacterId);
        var chatName = chat?.Name ?? string.Empty;

        if (result.Succeeded)
        {
            responseCode = AddFriendsChatMemberResponse.ResponseCode.Success;
            await NotifyFriendsChatMemberJoin(context, chatName, message.CharacterId);
        }
        else if (result.IsNotFound)
        {
            responseCode = AddFriendsChatMemberResponse.ResponseCode.NotFound;
        }
        else if (result.IsFull)
        {
            responseCode = AddFriendsChatMemberResponse.ResponseCode.Full;
        }
        else if (result.IsUnauthorized)
        {
            responseCode = AddFriendsChatMemberResponse.ResponseCode.Unauthorized;
        }
        else if (result.IsBanned)
        {
            responseCode = AddFriendsChatMemberResponse.ResponseCode.Banned;
        }

        await context.RespondAsync(new AddFriendsChatMemberResponse()
        {
            CharacterId = message.CharacterId, ChatName = chatName, Response = responseCode
        });
    }

    /**
     * TODO - Move to friendschatservice
     */
    private async Task NotifyFriendsChatMemberJoin(IPublishEndpoint publishEndpoint, string chatName, uint characterId)
    {
        var member = await _friendsChatService.FindMemberBySessionIdAsync(chatName, characterId);
        if (member == null)
        {
            return;
        }

        await publishEndpoint.Publish(new NotifyFriendsChatMemberJoin
        {
            ChatName = chatName,
            DisplayName = member.DisplayName,
            PreviousDisplayName = member.PreviousDisplayName,
            Rank = (FriendsChatRank)member.Rank,
            WorldId = member.WorldId
        });
    }

    public async Task Consume(ConsumeContext<RemoveFriendsChatMemberRequest> context)
    {
        var message = context.Message;
        var chat = await _friendsChatService.FindChatSessionBySessionIdAsync(message.CharacterId);
        if (chat == null)
        {
            await context.RespondAsync(new RemoveFriendsChatMemberResponse()
            {
                CharacterId = message.CharacterId, Succeeded = false
            });
            return;
        }

        var character = await _characterService.FindCharacterBySessionIdAsync(message.CharacterId);
        if (character == null)
        {
            await context.RespondAsync(new RemoveFriendsChatMemberResponse()
            {
                CharacterId = message.CharacterId, Succeeded = false
            });
            return;
        }

        var member = await _friendsChatService.FindMemberBySessionIdAsync(chat.Name, character.Id);
        if (member == null)
        {
            await context.RespondAsync(new RemoveFriendsChatMemberResponse()
            {
                CharacterId = message.CharacterId, Succeeded = false
            });
            return;
        }

        var result = await _friendsChatService.UnregisterMemberAsync(chat.Name, message.CharacterId);
        if (!result.Succeeded)
        {
            _logger.LogWarning($"Failed to remove '{member.DisplayName}' from friends chat");
        }
        else
        {
            await NotifyFriendsChatMemberLeave(context, chat.Name, member);
        }

        await context.RespondAsync(new RemoveFriendsChatMemberResponse()
        {
            CharacterId = message.CharacterId, Succeeded = result.Succeeded
        });
    }

    /// <summary>
    /// TODO - move to friendschat service
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="chatName"></param>
    /// <param name="member"></param>
    /// <returns></returns>
    private Task NotifyFriendsChatMemberLeave(IPublishEndpoint publishEndpoint, string chatName, FriendsChatDto.Member member) =>
        publishEndpoint.Publish(new NotifyFriendsChatMemberLeave()
        {
            ChatName = chatName,
            Members = new[]
            {
                new NotifyFriendsChatMemberLeave.Member()
                {
                    DisplayName = member.DisplayName, PreviousDisplayName = member.PreviousDisplayName, WorldId = member.WorldId
                }
            }
        });

    public async Task Consume(ConsumeContext<GetFriendsChatRequest> context)
    {
        var message = context.Message;
        var chat = await _friendsChatService.FindChatBySessionIdAsync(message.CharacterId);
        if (chat == null)
        {
            await context.RespondAsync(new GetFriendsChatResponse()
            {
                CharacterId = message.CharacterId, Response = GetFriendsChatResponse.ResponseCode.NotFound
            });
            return;
        }

        await context.RespondAsync(new GetFriendsChatResponse()
        {
            CharacterId = message.CharacterId,
            Response = GetFriendsChatResponse.ResponseCode.Success,
            ChatName = chat.ChatName,
            OwnerDisplayName = chat.OwnerDisplayName,
            OwnerPreviousDisplayName = chat.OwnerPreviousDisplayName,
            RankToKick = (FriendsChatRank)chat.RankToKick,
            Members = chat.Members.Select(member => new GetFriendsChatResponse.Member()
                {
                    DisplayName = member.DisplayName,
                    PreviousDisplayName = member.PreviousDisplayName,
                    WorldId = member.WorldId,
                    Rank = (FriendsChatRank)member.Rank
                })
                .ToList()
        });
    }

    public async Task Consume(ConsumeContext<DoFriendsChatMemberKickRequest> context)
    {
        var message = context.Message;
        var chat = await _friendsChatService.FindChatSessionBySessionIdAsync(message.CharacterId);
        if (chat == null)
        {
            await context.RespondAsync(new DoFriendsChatMemberKickResponse()
            {
                CharacterId = message.CharacterId, Response = DoFriendsChatMemberKickResponse.ResponseCode.NotFound
            });
            return;
        }

        var memberSession = await _characterService.FindSessionByDisplayName(message.MemberDisplayName);
        if (memberSession == null)
        {
            await context.RespondAsync(new DoFriendsChatMemberKickResponse()
            {
                CharacterId = message.CharacterId, Response = DoFriendsChatMemberKickResponse.ResponseCode.NotFound
            });
            return;
        }

        var member = await _friendsChatService.FindMemberBySessionIdAsync(chat.Name, memberSession.Id);
        if (member == null)
        {
            await context.RespondAsync(new DoFriendsChatMemberKickResponse()
            {
                CharacterId = message.CharacterId, Response = DoFriendsChatMemberKickResponse.ResponseCode.NotFound
            });
            return;
        }

        var kickerMember = await _friendsChatService.FindMemberBySessionIdAsync(chat.Name, message.CharacterId);
        if (kickerMember == null)
        {
            await context.RespondAsync(new DoFriendsChatMemberKickResponse()
            {
                CharacterId = message.CharacterId, Response = DoFriendsChatMemberKickResponse.ResponseCode.NotFound
            });
            return;
        }

        var chatSettings = await _friendsChatService.FindChatSettingsByNameAsync(chat.Name);
        if (chatSettings == null)
        {
            await context.RespondAsync(new DoFriendsChatMemberKickResponse()
            {
                CharacterId = message.CharacterId, Response = DoFriendsChatMemberKickResponse.ResponseCode.NotFound
            });
            return;
        }

        if (kickerMember.Rank < chatSettings.RankKick || kickerMember.Rank < member.Rank)
        {
            await context.RespondAsync(new DoFriendsChatMemberKickResponse()
            {
                CharacterId = message.CharacterId, Response = DoFriendsChatMemberKickResponse.ResponseCode.LowRank
            });
        }


        /* TODO FIX 
         var result = await context.Req(new RemoveFriendsChatMemberRequest()
        {
            CharacterId = memberSession.Id
        });
        

        await context.RespondAsync(new DoFriendsChatMemberKickResponse()
        {
            Response = result.Succeeded ? DoFriendsChatMemberKickResponse.ResponseCode.Success : DoFriendsChatMemberKickResponse.ResponseCode.Failed
        });*/
    }

    public async Task Consume(ConsumeContext<AddFriendsChatMessageRequest> context)
    {
        var message = context.Message;
        var chat = await _friendsChatService.FindChatSessionBySessionIdAsync(message.CharacterId);
        if (chat == null)
        {
            await context.RespondAsync(new AddFriendsChatMessageResponse()
            {
                CharacterId = message.CharacterId, Response = AddFriendsChatMessageResponse.ResponseCode.NotFound
            });
            return;
        }

        var settings = await _friendsChatService.FindChatSettingsByNameAsync(chat.Name);
        if (settings == null)
        {
            await context.RespondAsync(new AddFriendsChatMessageResponse()
            {
                CharacterId = message.CharacterId, Response = AddFriendsChatMessageResponse.ResponseCode.NotFound
            });
            return;
        }

        var chatMember = await _friendsChatService.FindMemberBySessionIdAsync(chat.Name, message.CharacterId);
        if (chatMember == null)
        {
            await context.RespondAsync(new AddFriendsChatMessageResponse()
            {
                CharacterId = message.CharacterId, Response = AddFriendsChatMessageResponse.ResponseCode.NotFound
            });
            return;
        }

        if (chatMember.Rank < settings.RankTalk)
        {
            await context.RespondAsync(new AddFriendsChatMessageResponse()
            {
                CharacterId = message.CharacterId, Response = AddFriendsChatMessageResponse.ResponseCode.NotAllowed
            });
            return;
        }

        await context.Publish(new NotifyFriendsChatMessage()
        {
            ChatName = chat.Name,
            DisplayName = chatMember.DisplayName,
            PreviousDisplayName = chatMember.PreviousDisplayName,
            Claims = chatMember.Claims.Select(claim => new NotifyFriendsChatMessage.Claim()
                {
                    Name = claim.Name
                })
                .ToList(),
            MessageLength = message.MessageLength,
            MessagePayload = message.MessagePayload
        });

        await context.RespondAsync(new AddFriendsChatMessageResponse()
        {
            CharacterId = message.CharacterId, Response = AddFriendsChatMessageResponse.ResponseCode.Success
        });
    }
}