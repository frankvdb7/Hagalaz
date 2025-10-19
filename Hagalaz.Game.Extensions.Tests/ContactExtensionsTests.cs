using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Features.FriendsChat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Extensions.Tests;

[TestClass]
public class ContactExtensionsTests
{
    [TestMethod]
    public void GetWorldId_WhenAvailabilityIsNobody_ShouldReturnNull()
    {
        // Arrange
        var friend = new Friend { MasterId = 1, Rank = FriendsChatRank.Friend, Availability = Availability.Nobody, AreMutualFriends = false };

        // Act
        var result = friend.GetWorldId(1);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetWorldId_WhenAvailabilityIsFriendsAndNotMutual_ShouldReturnNull()
    {
        // Arrange
        var friend = new Friend { MasterId = 1, Rank = FriendsChatRank.Friend, Availability = Availability.Friends, AreMutualFriends = false };

        // Act
        var result = friend.GetWorldId(1);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetWorldId_WhenAvailabilityIsFriendsAndMutual_ShouldReturnWorldId()
    {
        // Arrange
        var friend = new Friend { MasterId = 1, Rank = FriendsChatRank.Friend, Availability = Availability.Friends, AreMutualFriends = true };
        const int expectedWorldId = 2;

        // Act
        var result = friend.GetWorldId(expectedWorldId);

        // Assert
        Assert.AreEqual(expectedWorldId, result);
    }

    [TestMethod]
    public void GetWorldId_WhenAvailabilityIsEverybody_ShouldReturnWorldId()
    {
        // Arrange
        var friend = new Friend { MasterId = 1, Rank = FriendsChatRank.Friend, Availability = Availability.Everyone, AreMutualFriends = false };
        const int expectedWorldId = 3;

        // Act
        var result = friend.GetWorldId(expectedWorldId);

        // Assert
        Assert.AreEqual(expectedWorldId, result);
    }

    [TestMethod]
    public void GetWorldId_WithNullWorldId_ShouldReturnNull()
    {
        // Arrange
        var friend = new Friend { MasterId = 1, Rank = FriendsChatRank.Friend, Availability = Availability.Everyone, AreMutualFriends = false };

        // Act
        var result = friend.GetWorldId(null);

        // Assert
        Assert.IsNull(result);
    }
}
