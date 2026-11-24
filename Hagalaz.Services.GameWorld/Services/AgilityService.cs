using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Services
{
    public class AgilityService : IAgilityService
    {
        private readonly IAgilityObstacleDefinitionRepository _obstacleRepository;
        private readonly IAgilityCourseDefinitionRepository _courseRepository;
        private readonly IMapper _mapper;

        public AgilityService(IAgilityObstacleDefinitionRepository obstacleRepository, IAgilityCourseDefinitionRepository courseRepository, IMapper mapper)
        {
            _obstacleRepository = obstacleRepository;
            _courseRepository = courseRepository;
            _mapper = mapper;
        }

        public Task<AgilityObstacleDto?> FindObstacleByObjectId(int objectId)
        {
            var id = (uint)objectId;
            return _mapper.ProjectTo<AgilityObstacleDto>(_obstacleRepository.FindAll().Where(o => o.ObjectId == id)).FirstOrDefaultAsync();
        }

        public Task<AgilityCourseDto?> FindCourseById(AgilityCourseId courseId)
        {
            var id = (byte)courseId;
            return _mapper.ProjectTo<AgilityCourseDto>(_courseRepository.FindAll().Where(c => c.Id == id)).FirstOrDefaultAsync();
        }
    }
}
