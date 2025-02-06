namespace Hagalaz.Services.JagGrab.Network
{
    /// <summary>
    /// Represents a request for a file.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// The path of the requested file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Constructs a new request object.
        /// </summary>
        /// <param name="path">The path of the file being requested.</param>
        public Request(string path) => Path = path;
    }
}
