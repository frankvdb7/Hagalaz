namespace Hagalaz.Services.GameWorld
{
    public static class Constants
    {
        public const string ServiceName = "hagalaz-services-gameworld";
        public static class OAuth
        {
            public const string WorldClientId = "hagalaz-services-gameworld";
            public const string LobbyClientId = "hagalaz-services-gamelobby";
        }

        public static class Pipeline
        {
            public const string AuthSignInPipeline = "auth-signin";
            public const string AuthSignOutPipeline = "auth-signout";
        }

        public static class Cache
        {
            public const string WorldInfoCachePrefix = "world-info:";
            public static readonly string[] FishingTags = ["fishing"];
            public const string FishingSpotTableCachePrefix = "fishing-spot-definition:";
            public static readonly string[] SlayerTags = ["slayer"];
            public const string SlayerMasterTableCachePrefix = "slayer-master-table:";
            public const string SlayerTaskDefinitionCachePrefix = "slayer-task-definition:";
            public static readonly string[] GameObjectTags = ["game-object"];
            public const string GameObjectDefinitionCachePrefix = "game-object-definition:";
        }
    }
}