using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Services.Characters.Data;
using Hagalaz.Services.Characters.Mediator.Queries;
using Hagalaz.Services.Characters.Model;
using Microsoft.EntityFrameworkCore;
using MassTransit;

namespace Hagalaz.Services.Characters.Mediator.Consumers
{
    public class GetCharacterStatisticsQueryConsumer : IConsumer<GetCharacterStatisticsQuery>
    {
        private readonly ICharacterUnitOfWork _characterUnitOfWork;
        private readonly IMapper _mapper;

        public GetCharacterStatisticsQueryConsumer(ICharacterUnitOfWork characterUnitOfWork, IMapper mapper)
        {
            _characterUnitOfWork = characterUnitOfWork;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<GetCharacterStatisticsQuery> context)
        {
            var message = context.Message;
            var dto = await _mapper
                .ProjectTo<CharacterStatisticCollectionDto>(_characterUnitOfWork.CharacterStatisticsRepository.FindById(message.MasterId).AsNoTracking())
                .FirstOrDefaultAsync(context.CancellationToken);

            await context.RespondAsync(new GetCharacterStatisticsResult
            {
                Result = dto
            });
        }
    }
}