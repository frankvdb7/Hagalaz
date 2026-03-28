using System.Threading.Tasks;
using Hagalaz.Services.Characters.Controllers;
using Hagalaz.Services.Common.Model;
using Hagalaz.Services.Characters.Mediator.Queries;
using Hagalaz.Services.Characters.Model;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hagalaz.Services.Characters.Tests.Controllers
{
    [TestClass]
    public class StatsControllerTests
    {
        private Mock<IMediator> _mediatorMock;
        private Mock<IRequestClient<GetCharacterStatisticsQuery>> _getCharacterStatisticsQueryMock;
        private Mock<IRequestClient<GetAllCharacterStatisticsQuery>> _getAllCharacterStatisticsQueryMock;
        private StatsController _controller;

        [TestInitialize]
        public void Initialize()
        {
            _mediatorMock = new Mock<IMediator>();
            _getCharacterStatisticsQueryMock = new Mock<IRequestClient<GetCharacterStatisticsQuery>>();
            _getAllCharacterStatisticsQueryMock = new Mock<IRequestClient<GetAllCharacterStatisticsQuery>>();

            _mediatorMock.Setup(m => m.CreateRequestClient<GetCharacterStatisticsQuery>(default))
                .Returns(_getCharacterStatisticsQueryMock.Object);
            _mediatorMock.Setup(m => m.CreateRequestClient<GetAllCharacterStatisticsQuery>(default))
                .Returns(_getAllCharacterStatisticsQueryMock.Object);

            _controller = new StatsController(_mediatorMock.Object);
        }

        [TestMethod]
        public async Task Get_WithValidId_ReturnsOkWithMessage()
        {
            // Arrange
            var id = 1;
            var expectedResult = new GetCharacterStatisticsResult
            {
                Result = new CharacterStatisticCollectionDto
                {
                    DisplayName = "TestPlayer",
                    Statistics = []
                }
            };
            var responseMock = new Mock<Response<GetCharacterStatisticsResult>>();
            responseMock.Setup(r => r.Message).Returns(expectedResult);

            _getCharacterStatisticsQueryMock.Setup(c => c.GetResponse<GetCharacterStatisticsResult>(
                It.Is<GetCharacterStatisticsQuery>(q => q.MasterId == (uint)id), default, default))
                .ReturnsAsync(responseMock.Object);

            // Act
            var result = await _controller.Get(id);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            var okResult = (OkObjectResult)result.Result;
            Assert.AreEqual(expectedResult.Result, okResult.Value);
        }

        [TestMethod]
        public async Task Get_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var id = 1;
            var expectedResult = new GetCharacterStatisticsResult
            {
                Result = null
            };
            var responseMock = new Mock<Response<GetCharacterStatisticsResult>>();
            responseMock.Setup(r => r.Message).Returns(expectedResult);

            _getCharacterStatisticsQueryMock.Setup(c => c.GetResponse<GetCharacterStatisticsResult>(
                It.Is<GetCharacterStatisticsQuery>(q => q.MasterId == (uint)id), default, default))
                .ReturnsAsync(responseMock.Object);

            // Act
            var result = await _controller.Get(id);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task GetAll_ReturnsOkWithMessage()
        {
            // Arrange
            var request = new GetAllCharacterStatisticsRequest(
                new GetAllCharacterStatisticsRequest.SortModel(),
                new GetAllCharacterStatisticsRequest.FilterModel
                {
                    Page = 1,
                    Limit = 10,
                    Type = CharacterStatisticType.Overall
                }
            );
            var expectedResult = new GetAllCharacterStatisticsResult
            {
                Results = [],
                MetaData = new PagingMetaDataModel(0, 1, 10)
            };
            var responseMock = new Mock<Response<GetAllCharacterStatisticsResult>>();
            responseMock.Setup(r => r.Message).Returns(expectedResult);

            _getAllCharacterStatisticsQueryMock.Setup(c => c.GetResponse<GetAllCharacterStatisticsResult>(
                It.IsAny<GetAllCharacterStatisticsQuery>(), default, default))
                .ReturnsAsync(responseMock.Object);

            // Act
            var result = await _controller.GetAll(request);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            var okResult = (OkObjectResult)result.Result;
            Assert.AreEqual(expectedResult, okResult.Value);
        }
    }
}
