using AutoMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Characters.Tests
{
    [TestClass]
    public class MappingTests
    {
        [TestMethod]
        public void AutoMapper_Configuration_IsValid()
        {
            var config = new MapperConfiguration(action => action.AddMaps(typeof(Program)));
            config.AssertConfigurationIsValid();
        }
    }
}