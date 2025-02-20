﻿using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Data
{
    public interface ICraftingGemDefinitionRepository
    {
        public IQueryable<SkillsCraftingGemDefinition> FindAll();
    }
}
