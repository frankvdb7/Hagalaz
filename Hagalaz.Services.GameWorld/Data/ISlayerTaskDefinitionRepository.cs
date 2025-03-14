﻿using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ISlayerTaskDefinitionRepository
    {
        public IQueryable<SkillsSlayerTaskDefinition> FindAll();
    }
}
