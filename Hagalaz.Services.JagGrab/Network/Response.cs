using System.IO;

namespace Hagalaz.Services.JagGrab.Network
{
    /// <summary>
    /// Represents a response for a file request.
    /// </summary>
    public class Response
    {
        #region Properties
        /// <summary>
        /// The data in the file.
        /// </summary>
        public MemoryStream FileData { get; set; }
        #endregion Properties

        #region Constructors
        /// <summary>
        /// Constructs a new response object.
        /// </summary>
        /// <param name="data">The data from the file.</param>
        public Response(byte[] data) => FileData = new MemoryStream(data);

        #endregion Constructors
    }
}
