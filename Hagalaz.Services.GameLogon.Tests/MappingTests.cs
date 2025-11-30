using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.GameLogon.Tests
{
    [TestClass]
    public class MappingTests
    {
        [TestMethod]
        public void AutoMapper_Configuration_IsValid()
        {
            var config = new MapperConfiguration(action => action.AddMaps(typeof(Startup)), LoggerFactory.Create(_ => { }));
            config.AssertConfigurationIsValid();
        }
    }
}