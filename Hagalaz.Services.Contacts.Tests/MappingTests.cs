using AutoMapper;

namespace Hagalaz.Services.Contacts.Tests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void AutoMapper_Configuration_IsValid()
    {
        var config = new MapperConfiguration(action => action.AddMaps(typeof(Program)));
        config.AssertConfigurationIsValid();
    }
}