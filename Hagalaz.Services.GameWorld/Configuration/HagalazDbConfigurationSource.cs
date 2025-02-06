using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Hagalaz.Services.GameWorld.Configuration
{
    public class HagalazDbConfigurationSource : IConfigurationSource
    {
        private readonly Action<DbContextOptionsBuilder> _optionsAction;

        public HagalazDbConfigurationSource(Action<DbContextOptionsBuilder> optionsAction) => _optionsAction = optionsAction;

        public IConfigurationProvider Build(IConfigurationBuilder builder) => new HagalazDbConfigurationProvider(_optionsAction);
    }
}