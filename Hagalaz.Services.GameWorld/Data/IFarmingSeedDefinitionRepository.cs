﻿using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface IFarmingSeedDefinitionRepository
    {
        public IQueryable<SkillsFarmingSeedDefinition> FindAll();
    }
}
