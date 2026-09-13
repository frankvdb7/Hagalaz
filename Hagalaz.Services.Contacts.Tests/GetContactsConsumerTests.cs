using Hagalaz.Contacts.Messages;
using Hagalaz.Services.Contacts.Consumers;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Services.Model;
using MassTransit;
using Moq;

namespace Hagalaz.Services.Contacts.Tests;

[TestClass]
public sealed class GetContactsConsumerTests
{
    [TestMethod]
    public async Task Consume_CarriesOnlineSessionIdentityAndLeavesOfflineIdentityNull()
    {
        var contactService = new Mock<IContactService>();
        contactService
            .Setup(service => service.FindFriendsByIdAsync(42))
            .Returns(new ValueTask<IReadOnlyList<ContactDto>>(
                new List<ContactDto>
                {
                    new()
                    {
                        MasterId = 7,
                        DisplayName = "Online",
                        WorldId = 1,
                        WorldName = "World 1",
                        SessionGeneration = 11,
                        SessionConnectionId = "new"
                    },
                    new()
                    {
                        MasterId = 8,
                        DisplayName = "Offline"
                    }
                }));
        contactService
            .Setup(service => service.FindIgnoresByIdAsync(42))
            .Returns(new ValueTask<IReadOnlyList<ContactDto>>(Array.Empty<ContactDto>()));

        GetContactsResponse? response = null;
        var context = new Mock<ConsumeContext<GetContactsRequest>>();
        context.SetupGet(item => item.Message).Returns(new GetContactsRequest(42));
        context
            .Setup(item => item.RespondAsync(It.IsAny<GetContactsResponse>()))
            .Callback<GetContactsResponse>(message => response = message)
            .Returns(Task.CompletedTask);

        await new GetContactsConsumer(contactService.Object).Consume(context.Object);

        Assert.IsNotNull(response);
        var online = response.Friends.Single(friend => friend.MasterId == 7);
        var offline = response.Friends.Single(friend => friend.MasterId == 8);
        Assert.AreEqual(1, online.WorldId);
        Assert.AreEqual("World 1", online.WorldName);
        Assert.AreEqual(11L, online.SessionGeneration);
        Assert.AreEqual("new", online.SessionConnectionId);
        Assert.IsNull(offline.SessionGeneration);
        Assert.IsNull(offline.SessionConnectionId);
    }
}
