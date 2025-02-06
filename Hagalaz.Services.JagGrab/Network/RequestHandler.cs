using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.JagGrab.Network
{
    /// <summary>
    /// Handles update requests.
    /// </summary>
    public class RequestHandler
    {
        /// <summary>
        /// The options
        /// </summary>
        private readonly JagGrabConfig _config;
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger<RequestHandler> _logger;
        
        public RequestHandler(IOptions<JagGrabConfig> options, ILogger<RequestHandler> logger)
        {
            _config = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Handles a single request.
        /// </summary>
        /// <param name="request">The request to handle.</param>
        /// <returns>
        /// The response.
        /// </returns>
        public Response? Handle(Request request)
        {
            try
            {
                string fileUrl = _config.DataPath + request.Path;
                if (!fileUrl.Contains("..")) // possible exploit
                {
                    if (File.Exists(fileUrl))
                    {
                        return new Response(File.ReadAllBytes(fileUrl));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send response");
            }
            return null;
        }
    }
}
