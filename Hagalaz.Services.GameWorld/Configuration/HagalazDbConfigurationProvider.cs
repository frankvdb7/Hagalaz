using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameWorld.Configuration
{
    /// <summary>
    /// 
    /// </summary>
    public class HagalazDbConfigurationProvider : ConfigurationProvider
    {
        Action<DbContextOptionsBuilder> OptionsAction { get; }  
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="optionsAction"></param>
        public HagalazDbConfigurationProvider(Action<DbContextOptionsBuilder> optionsAction)
        {
            OptionsAction = optionsAction;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Load()
        {
            var builder = new DbContextOptionsBuilder<HagalazDbContext>();
            OptionsAction(builder);

            using var dbContext = new HagalazDbContext(builder.Options);
            Data = (!dbContext.WorldConfigurations.Any()
                ? CreateAndSaveDefaultValues(dbContext)
                : dbContext.WorldConfigurations.ToDictionary(c => c.Id, c => c.Value))!;
        }
        
        private static Dictionary<string, string> CreateAndSaveDefaultValues(HagalazDbContext dbContext)
        {
            var configValues = new Dictionary<string, string>
            {
                { "Combat:ExpRate", "1" },
                { "Combat:CharacterAttackTickDelay", "8" },
                { "Combat.NpcAttackTickDelay", "4" },
                { "Skill:ExpRate", "1" },
                { "Item:CoinProbabilityRate", "1" },
                { "Item:CoinCountRate", "1" },
                { "GroundItem:PublicTickTime", "200" },
                { "GroundItem:PrivateTickTime", "100" },
                { "GroundItem:NonTradableTickTime", "300" },
                { "World:Name", "World 1" },
                { "World:SpawnPoint:X", "3222" },
                { "World:SpawnPoint:Y", "3222" },
                { "World:SpawnPoint:Z", "0" },
                { "World:WelcomeMessage", "Welcome to this server!" },
                { "World:MessageOfTheWeek", "" }
            };

            dbContext.WorldConfigurations.AddRange(configValues
                .Select(kvp => new WorldConfiguration
                {
                    Id = kvp.Key,
                    Value = kvp.Value
                })
                .ToArray());

            dbContext.SaveChanges();

            return configValues;
        }
    }
}