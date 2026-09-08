using AutoMapper;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Characters.Profiles;
using Hagalaz.Services.Characters.Services.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Characters.Tests
{
    [TestClass]
    public class MappingTests
    {
        [TestMethod]
        public void AutoMapper_Configuration_IsValid()
        {
            var config = new MapperConfiguration(action => action.AddMaps(typeof(Program)), LoggerFactory.Create(_ => { }));
            config.AssertConfigurationIsValid();
        }

        [TestMethod]
        public async Task CharacterStateProjection_PreservesStringStateIdentifier()
        {
            var options = new DbContextOptionsBuilder<HagalazDbContext>()
                .UseInMemoryDatabase(nameof(CharacterStateProjection_PreservesStringStateIdentifier))
                .Options;
            await using var context = new HagalazDbContext(options);
            context.CharactersStates.Add(new CharactersState
            {
                MasterId = 1,
                StateId = "default-skulled-state",
                TicksLeft = 42
            });
            await context.SaveChangesAsync();

            var mapper = new MapperConfiguration(
                action => action.AddProfile<CharacterStateProfile>(),
                LoggerFactory.Create(_ => { }))
                .CreateMapper();

            var state = await mapper.ProjectTo<State.StateEx>(context.CharactersStates).SingleAsync();

            Assert.AreEqual("default-skulled-state", state.Id);
            Assert.AreEqual(42, state.TicksLeft);
        }
    }
}
