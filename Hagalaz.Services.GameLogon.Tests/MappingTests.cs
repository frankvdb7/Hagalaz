using AutoMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.GameLogon.Tests
{
    [TestClass]
    public class MappingTests
    {
        [TestMethod]
        public void AutoMapper_Configuration_IsValid()
        {
            var config = new MapperConfiguration(action => action.AddMaps(typeof(Startup)));
            config.AssertConfigurationIsValid();
        }
    }
}