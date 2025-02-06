namespace Hagalaz.Services.GameUpdate.Network
{
    public class FileSyncSession
    {
        public const string Key = "Session";
        public bool Authenticated { get; set; }
        public byte EncryptionFlag { get; set; }
    }
}