using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Contacts.Services.Model;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.Contacts.Tests;

[TestClass]
public class UnitTest1
{
    private const string GameModeratorRole = "GameModerator";

    [TestMethod]
    public void AutoMapper_Configuration_IsValid()
    {
        var config = new MapperConfiguration(action => action.AddMaps(typeof(Program)), LoggerFactory.Create(_ => { }));
        config.AssertConfigurationIsValid();
    }

    [TestMethod]
    public void CharacterProjection_UsesAssignedIdentityRolesAsClaims()
    {
        var mapper = CreateMapper();
        var character = new Character { Id = 1, DisplayName = "Character" };
        var role = new Aspnetrole { Id = 1, Name = GameModeratorRole };
        character.Aspnetuserroles.Add(new Aspnetuserrole { UserId = character.Id, RoleId = role.Id, Role = role, User = character });

        var dto = mapper.ProjectTo<CharacterDto>(new[] { character }.AsQueryable()).Single();

        Assert.IsNotNull(dto.Claims);
        CollectionAssert.AreEqual(new[] { GameModeratorRole }, dto.Claims!.Select(claim => claim.Name).ToArray());
    }

    [TestMethod]
    public void CharacterProjection_ReturnsNoClaimsWhenNoIdentityRolesAreAssigned()
    {
        var mapper = CreateMapper();
        var character = new Character { Id = 1, DisplayName = "Character" };

        var dto = mapper.ProjectTo<CharacterDto>(new[] { character }.AsQueryable()).Single();

        Assert.IsNotNull(dto.Claims);
        Assert.AreEqual(0, dto.Claims!.Count);
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(action => action.AddMaps(typeof(Program)), LoggerFactory.Create(_ => { }));
        return config.CreateMapper();
    }
}
