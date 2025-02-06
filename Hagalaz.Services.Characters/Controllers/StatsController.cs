using System.Threading.Tasks;
using Hagalaz.Services.Characters.Mediator.Queries;
using Hagalaz.Services.Characters.Model;
using Hagalaz.Services.Common.Model;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace Hagalaz.Services.Characters.Controllers
{
    [Route("api/v1/characters/stats")]
    [ApiController]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class StatsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IRequestClient<GetCharacterStatisticsQuery> _getCharacterStatisticsQuery;
        private readonly IRequestClient<GetAllCharacterStatisticsQuery> _getAllCharacterStatisticsQuery;

        public StatsController(IMediator mediator)
        {
            _mediator = mediator;
            _getCharacterStatisticsQuery = _mediator.CreateRequestClient<GetCharacterStatisticsQuery>();
            _getAllCharacterStatisticsQuery = _mediator.CreateRequestClient<GetAllCharacterStatisticsQuery>();
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CharacterStatisticCollectionDto>> Get(int id)
        {
            var response = await _getCharacterStatisticsQuery.GetResponse<GetCharacterStatisticsResult>(new GetCharacterStatisticsQuery { MasterId = (uint)id });
            var message = response.Message;
            if (message.Result == null)
            {
                return NotFound();
            }

            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<GetAllCharacterStatisticsResult>> GetAll([FromBody] GetAllCharacterStatisticsRequest request)
        {
            var (sort, filter) = request;
            var response = await _getAllCharacterStatisticsQuery.GetResponse<GetAllCharacterStatisticsResult>(new GetAllCharacterStatisticsQuery
            {
                Sort = sort != null ? new GetAllCharacterStatisticsQuery.SortModel(sort.Experience) : null,
                Paging = filter != null ? new PagingModel(filter.Page, filter.Limit) : null,
                Filter = filter != null ? new GetAllCharacterStatisticsQuery.FilterModel(filter.Type) : null
            });
            var message = response.Message;
            return Ok(message);
        }
    }
}