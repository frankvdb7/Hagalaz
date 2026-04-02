using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using Hagalaz.Services.Characters.Model;
using Hagalaz.Services.Common.Model;
using System.Text.Json.Serialization;
using Hagalaz.Services.Characters.Controllers;
using Moq;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Hagalaz.Services.Characters.Mediator.Queries;

namespace Hagalaz.Services.Characters.Tests
{
    [TestClass]
    public class HighscoresCorrectnessTests
    {
        [TestMethod]
        public void TestRequestSerialization_ExperienceIsPopulated()
        {
            var json = "{\"sort\":{\"experience\":\"Desc\"},\"filter\":{\"page\":1,\"limit\":10,\"type\":\"Overall\"}}";
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            options.Converters.Add(new JsonStringEnumConverter());
            var request = JsonSerializer.Deserialize<GetAllCharacterStatisticsRequest>(json, options);

            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Sort);
            Assert.AreEqual(SortType.Desc, request.Sort.Experience);
        }

        [TestMethod]
        public async Task GetAll_NullRequest_ReturnsBadRequest()
        {
            var mediatorMock = new Mock<IMediator>();
            var controller = new StatsController(mediatorMock.Object);

            var result = await controller.GetAll(null!);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestResult));
        }
    }
}
