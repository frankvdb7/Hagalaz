using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Database.Migrations.Seeding;

/// <summary>
///     Seeds the Gnome Stronghold Agility course: the experience and required level of every obstacle object and the completion experience
///     of the basic (course id 1) and advanced (course id 2) course. These are the values the obstacle scripts used to
///     hard-code. A definition is only created when its key is absent; an existing row is never changed.
/// </summary>
public sealed class GnomeAgilityCourseSeeder : IGameWorldSeeder
{
    public const byte BasicCourseId = 1;
    public const byte AdvancedCourseId = 2;

    private static readonly (uint ObjectId, decimal Experience, byte RequiredLevel)[] _obstacles =
    [
        (69526, 7.5m, 1), // log balance
        (69383, 7.5m, 1), // first obstacle net
        (69508, 5m, 1), // first tree branch
        (2312, 7.5m, 1), // balancing rope
        (4059, 7.5m, 1), // balancing rope
        (69507, 5m, 1), // tree branch down
        (69384, 7.5m, 1), // second obstacle net
        (69377, 7.5m, 1), // obstacle pipe
        (69378, 7.5m, 1), // obstacle pipe
        (69506, 25m, 85), // tree
        (69514, 25m, 1), // signpost
        (43529, 25m, 1), // pole
        (69389, 25m, 1) // barrier
    ];

    private static readonly (byte Id, decimal CompletionExperience)[] _courses =
    [
        (BasicCourseId, 39m),
        (AdvancedCourseId, 605m)
    ];

    private readonly HagalazDbContext _context;
    private readonly ILogger<GnomeAgilityCourseSeeder> _logger;

    public GnomeAgilityCourseSeeder(HagalazDbContext context, ILogger<GnomeAgilityCourseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var objectIds = _obstacles.Select(obstacle => obstacle.ObjectId).ToArray();
        var existingObstacles = (await _context.SkillsAgilityObstacleDefinitions
            .AsNoTracking()
            .Where(obstacle => objectIds.Contains(obstacle.ObjectId))
            .Select(obstacle => obstacle.ObjectId)
            .ToListAsync(cancellationToken)).ToHashSet();

        foreach (var (objectId, experience, requiredLevel) in _obstacles.Where(obstacle => !existingObstacles.Contains(obstacle.ObjectId)))
        {
            _context.SkillsAgilityObstacleDefinitions.Add(new SkillsAgilityObstacleDefinition { ObjectId = objectId, Experience = experience, RequiredLevel = requiredLevel });
            _logger.LogInformation("Seeded agility obstacle definition {ObjectId}.", objectId);
        }

        var existingCourses = (await _context.SkillsAgilityCourseDefinitions
            .AsNoTracking()
            .Select(course => course.Id)
            .ToListAsync(cancellationToken)).ToHashSet();

        foreach (var (id, completionExperience) in _courses.Where(course => !existingCourses.Contains(course.Id)))
        {
            _context.SkillsAgilityCourseDefinitions.Add(new SkillsAgilityCourseDefinition { Id = id, CompletionExperience = completionExperience });
            _logger.LogInformation("Seeded agility course definition {Id}.", id);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
