using System;

namespace MiniHttpd
{
    /// <summary>
    /// Provides data for events that are raised by the client of an HTTP transaction.
    /// </summary>
    public class ClientEventArgs : EventArgs
    {
        private readonly HttpClient _client;

        /// <summary>
        /// Creates a new <see cref="ClientEventArgs"/> with the specified client.
        /// </summary>
        /// <param name="client">The client to which the event belongs.</param>
        public ClientEventArgs(HttpClient client)
        {
            this._client = client;
        }

        /// <summary>
        /// Gets the client to which the event belongs.
        /// </summary>
        public HttpClient HttpClient
        {
            get { return _client; }
        }
    }

    /// <summary>
    /// Provides access to the information of the events that occur when a request is received.
    /// </summary>
    public class RequestEventArgs : ClientEventArgs
    {
        private readonly HttpRequest request;
        private bool isAuthenticated = true;

        internal RequestEventArgs(HttpClient client, HttpRequest request) : base(client)
        {
            this.request = request;
        }

        /// <summary>
        /// Gets the request which the client sent.
        /// </summary>
        public HttpRequest Request
        {
            get { return request; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user has been authenticated for the transaction.
        /// </summary>
        public bool IsAuthenticated
        {
            get { return isAuthenticated; }
            set { isAuthenticated = value; }
        }
    }

    /// <summary>
    /// Provides data to an event raised by an <see>HttpResponse</see> object.
    /// </summary>
    public class ResponseEventArgs : ClientEventArgs
    {
        private readonly long contentLength;
        private readonly HttpResponse response;

        internal ResponseEventArgs(HttpClient client, HttpResponse response) : this(client, response, -1)
        {
        }

        internal ResponseEventArgs(HttpClient client, HttpResponse response, long contentLength) : base(client)
        {
            this.response = response;
            this.contentLength = contentLength;
        }

        /// <summary>
        /// Gets the <see>HttpResponse</see> object that triggered this event.
        /// </summary>
        public HttpResponse Response
        {
            get { return response; }
        }

        /// <summary>
        /// Gets the content length of the response. This value is negative when the content length is unknown.
        /// </summary>
        public long ContentLength
        {
            get { return contentLength; }
        }
    }
}