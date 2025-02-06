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
    }
}