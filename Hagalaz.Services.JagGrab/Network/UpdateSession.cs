using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.JagGrab.Network
{
    /// <summary>
    /// Represents a JAGGRAB update session.
    /// </summary>
    public class UpdateSession : IDisposable
    {
        /// <summary>
        /// The file request.
        /// </summary>
        private Request _request;
        /// <summary>
        /// The logger
        /// </summary>
        private ILogger _logger;
        /// <summary>
        /// The request handler
        /// </summary>
        private RequestHandler _requestHandler;
        
        /// <summary>
        /// Constructs a new <code>UpdateSession</code> object.
        /// </summary>
        /// <param name="client">The client being served.</param>
        /// <param name="requestHandler">The request handler.</param>
        /// <param name="connectionHandler">The connection handler.</param>
        /// <param name="logger">The logger.</param>
        public UpdateSession(RequestHandler requestHandler, ILogger<UpdateSession> logger)
        {
            _logger = logger;
            _requestHandler = requestHandler;
        }

        #region Methods
        /// <summary>
        /// Disconnects the client.
        /// </summary>
        public void Disconnect()
        {

        }

        /// <summary>
        /// Process incoming data.
        /// </summary>
        /// <param name="data">The data to be processed.</param>
        public void ProcessData(byte[] data)
        {
            try
            {
                const string start = "JAGGRAB ";
                string path = Encoding.ASCII.GetString(data);
                if (path.StartsWith(start))
                {
                    _request = new Request(path.Substring(start.Length).Trim());
                    Serve();
                }
                else
                {
                    Disconnect();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process data");
                Disconnect();
            }
        }

        /// <summary>
        /// Serves the requested file.
        /// </summary>
        private void Serve()
        {
            if (_request == null)
            {
                Disconnect();
                return;
            }

            var resp = _requestHandler.Handle(_request);
            if (resp == null)
            {
                Disconnect();
                return;
            }

            //_client.SendDataAsync(resp.FileData.ToArray()).Wait();
        }

        #region IDispose Members
        /// <summary>
        /// Attempts to dispose the session.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed code.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //if (_client != null)
                //{
                //    _client.Dispose();
                //    _client = null;
                //}
            }
        }
        #endregion IDispose Memebrs
        #endregion Methods
    }
}
