using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Tests
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
    }
}