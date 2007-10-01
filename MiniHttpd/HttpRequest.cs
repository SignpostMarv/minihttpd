using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;

namespace MiniHttpd
{
    /// <summary>
    /// Represents an HTTP request received from a client.
    /// </summary>
    public class HttpRequest : MarshalByRefObject, IDisposable
    {
        ///<summary>
        ///</summary>
        ///<param name="client"></param>
        public HttpRequest(HttpClient client)
        {
            _dataMode = DataMode.Text;
            state = ProcessingState.RequestLine;
            connMode = ConnectionMode.KeepAlive;
            _client = client;
            response = new HttpResponse(this);
        }

        #region State

        private readonly HttpClient _client;
        private DataMode _dataMode;
        private string _errorMessage;
        private bool _isRequestError;
        private bool _isRequestFinished;
        private string _statusCode = "200";

        internal DataMode Mode
        {
            get { return _dataMode; }
        }

        internal bool IsRequestFinished
        {
            get { return _isRequestFinished; }
        }

        /// <summary>
        /// Gets the associated <see cref="Client"/>.
        /// </summary>
        public HttpClient Client
        {
            get { return _client; }
        }

        /// <summary>
        /// Gets the server to which this request was sent.
        /// </summary>
        public HttpServer Server
        {
            get
            {
                if (_client == null)
                    return null;
                return _client.server;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this request is a syntactically valid HTTP/1.x reuest.
        /// </summary>
        public bool IsValidRequest
        {
            get { return !_isRequestError; }
        }

        /// <summary>
        /// Gets the status code of the request.
        /// </summary>
        public string StatusCode
        {
            get { return _statusCode; }
        }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        internal void RequestError(string httpStatusCode, string message)
        {
            if (_statusCode == null)
                throw new ArgumentNullException("httpStatusCode");
            connMode = ConnectionMode.Close;
            _isRequestFinished = true;
            _statusCode = httpStatusCode;
            _errorMessage = message;
            _isRequestError = true;
        }

        internal enum DataMode
        {
            /// <summary>
            /// Text mode transmission
            /// </summary>
            Text,
            /// <summary>
            /// Binary mode transmission
            /// </summary>
            Binary
        }

        #endregion

        #region Processing

        private static readonly string[] httpDateTimeFormats = new string[]
            {
                "ddd, d MMM yyyy H:m:s GMT",
                "dddd, d-MMM-yy H:m:s GMT",
                "ddd MMM d H:mm:s yy"
            };

        private string requestUri;
        private ProcessingState state;

        private static DateTime ParseHttpTime(string str)
        {
            DateTime dt;
            try
            {
                dt = DateTime.ParseExact(str, httpDateTimeFormats, DateTimeFormatInfo.InvariantInfo,
                                         DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal);
            }
            catch (FormatException)
            {
                dt = DateTime.Parse(str, CultureInfo.InvariantCulture);
            }
            return dt;
        }

        private void PostProcessHeaders()
        {
            if (httpVersion == "1.1" && host == null)
            {
                RequestError("400", "HTTP/1.1 requests must include Host header");
                return;
            }

            if (_client.server.RequireAuthentication && Server.Authenticator.Authenticate(username, password) == false)
            {
                Response.SetHeader("WWW-Authenticate", "Basic realm=\"" + _client.server.AuthenticateRealm + "\"");
                RequestError("401", StatusCodes.GetDescription("401"));
                return;
            }

            try
            {
                // Try parsing a relative URI
                //uri = new Uri(client.server.ServerUri, requestUri);
                uri = _client.server.GetRelUri(requestUri);
            }
            catch
            {
                try
                {
                    // Try parsing an absolute URI
                    //uri = new Uri(requestUri);
                    uri = _client.server.GetAbsUri(requestUri);
                }
                catch (UriFormatException)
                {
                    RequestError("400", "Invalid URI");
                    return;
                }
                catch (IndexOutOfRangeException) // System.Uri in .NET 1.1 throws this exception in certain cases
                {
                    RequestError("400", "Invalid URI");
                    return;
                }
            }

            if (host != null)
                uri = _client.server.GetHostUri(host, requestUri);

            // Try to determine the time difference between the client and this computer; adjust ifModifiedSince and ifUnmodifiedSince accordingly
            if (date != DateTime.MinValue)
            {
                if (ifModifiedSince != DateTime.MinValue)
                    ifModifiedSince.Add(DateTime.UtcNow.Subtract(date));
                if (ifUnmodifiedSince != DateTime.MinValue)
                    ifUnmodifiedSince.Add(DateTime.UtcNow.Subtract(date));
            }

            if (method == "POST")
            {
                if (contentLength == long.MinValue)
                {
                    RequestError("411", StatusCodes.GetDescription("411"));
                    return;
                }
                _dataMode = DataMode.Binary;
            }
            else
                _isRequestFinished = true;
        }

        internal void ProcessLine(string line)
        {
            switch (state)
            {
                case ProcessingState.RequestLine:
                    {
                        string[] protocols = line.Split(' ');
                        if (protocols.Length != 3)
                        {
                            RequestError("400", "Invalid protocol string");
                            return;
                        }

                        switch (protocols[0])
                        {
                            case "GET":
                            case "POST":
                            case "HEAD":
                                method = protocols[0];
                                break;
                            case "PUT":
                            case "DELETE":
                            case "OPTIONS":
                            case "TRACE":
                            default:
                                RequestError("501", StatusCodes.GetDescription("501"));
                                return;
                        }

                        if (protocols[1].Length > 2500)
                        {
                            RequestError("414", StatusCodes.GetDescription("414"));
                            return;
                        }
                        requestUri = protocols[1];

                        if (!protocols[2].StartsWith("HTTP/") || !(protocols[2].Length > "HTTP/".Length))
                        {
                            RequestError("400", "Invalid protocol string");
                            return;
                        }

                        httpVersion = protocols[2].Substring("HTTP/".Length);

                        date = DateTime.Now;

                        connMode = httpVersion == "1.0" ? ConnectionMode.Close : ConnectionMode.KeepAlive;

                        state = ProcessingState.Headers;
                        break;
                    }
                case ProcessingState.Headers:
                    {
                        if (headers.Count > maxHeaderLines)
                        {
                            RequestError("400", "Maximum header line count exceeded");
                            return;
                        }

                        if (line.Length == 0)
                        {
                            PostProcessHeaders();
                            return;
                        }

                        int colonIndex = line.IndexOf(":");
                        if (colonIndex <= 1)
                            return;
                        string val = line.Substring(colonIndex + 1).Trim();
                        string name = line.Substring(0, colonIndex);

                        try
                        {
                            headers.Add(name, val);
                        }
                        catch (NotSupportedException)
                        {
                        }

                        switch (name.ToLower(CultureInfo.InvariantCulture))
                        {
                            case "host":
                                host = val;
                                break;
                            case "authorization":
                                {
                                    if (val.Length < 6)
                                        break;

                                    string encoded = val.Substring(6, val.Length - 6);
                                    byte[] byteAuth;
                                    try
                                    {
                                        byteAuth = Convert.FromBase64String(encoded);
                                    }
                                    catch (FormatException)
                                    {
                                        break;
                                    }

                                    string[] strings = Encoding.UTF8.GetString(byteAuth).Split(':');
                                    if (strings.Length != 2)
                                        break;

                                    username = strings[0];
                                    password = strings[1];

                                    break;
                                }
                            case "content-type":
                                contentType = val;
                                break;
                            case "content-length":
                                try
                                {
                                    contentLength = long.Parse(val, NumberStyles.Integer, CultureInfo.InvariantCulture);
                                }
                                catch (FormatException)
                                {
                                }
                                if (contentLength > _client.server.MaxPostLength)
                                {
                                    RequestError("413", StatusCodes.GetDescription("413"));
                                    return;
                                }
                                else if (contentLength < 0)
                                {
                                    RequestError("400", StatusCodes.GetDescription("400"));
                                    return;
                                }
                                break;
                            case "accept":
                                accept = val;
                                break;
                            case "accept-language":
                                acceptLanguage = val;
                                break;
                            case "user-agent":
                                userAgent = val;
                                break;
                            case "connection":
                                if (string.Compare(val, "close", true, CultureInfo.InvariantCulture) == 0)
                                    connMode = ConnectionMode.Close;
                                else
                                    connMode = ConnectionMode.KeepAlive;
                                break;
                            case "if-modified-since":
                                try
                                {
                                    ifModifiedSince = ParseHttpTime(val);
                                }
                                catch (FormatException)
                                {
                                }
                                break;
                            case "if-unmodified-since":
                                try
                                {
                                    ifUnmodifiedSince = ParseHttpTime(val);
                                }
                                catch (FormatException)
                                {
                                }
                                break;
                            case "range":
                                try
                                {
                                    string[] rangeStrings = val.Split(',');
                                    ranges = new ByteRange[rangeStrings.Length];
                                    for (int i = 0; i < rangeStrings.Length; i++)
                                        ranges[i] = new ByteRange(rangeStrings[i]);
                                }
                                catch (FormatException)
                                {
                                    ranges = null;
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    }
            }
        }

        private enum ProcessingState
        {
            RequestLine = 0,
            Headers,
        }

        #endregion

        #region POST data processing

        private readonly MemoryStream postData = new MemoryStream();
        private long dataRemaining = -1;

        /// <summary>
        /// Returns the POST data received from the client.
        /// </summary>
        public MemoryStream PostData
        {
            get { return postData; }
        }

        internal void ProcessData(byte[] buffer, int offset, int length)
        {
            if (dataRemaining == -1)
            {
                dataRemaining = contentLength;

                // Trim the leading LF.
                offset++;
                length--;
            }
            if (dataRemaining == 0)
            {
                _isRequestFinished = true;
                postData.Seek(0, SeekOrigin.Begin);
                return;
            }

            length = (int) (dataRemaining < length ? dataRemaining : length);
            if (postData.Length + length >= Server.MaxPostLength)
            {
                _isRequestFinished = true;
                length = (int) (Server.MaxPostLength - postData.Length);
            }

            postData.Write(buffer, offset, length);
            dataRemaining -= length;
            if (dataRemaining <= 0)
            {
                _isRequestFinished = true;
                postData.Seek(0, SeekOrigin.Begin);
            }
        }

        #endregion

        #region Response

        private readonly NameValueCollection headers = new NameValueCollection(new CaseInsensitiveEqualityComparer());

        private readonly HttpResponse response;

        /// <summary>
        /// Gets the collection of HTTP headers received from the client.
        /// </summary>
        public NameValueCollection Headers
        {
            get { return headers; }
        }

        /// <summary>
        /// Gets the <see cref="HttpResponse"/> to this request.
        /// </summary>
        public HttpResponse Response
        {
            get { return response; }
        }

        internal void SendResponse()
        {
            if (response.ResponseContent == null)
            {
                //Default page
                MemoryStream stream = new MemoryStream(512);
                StreamWriter writer = new StreamWriter(stream);

                //string message = response.ResponseCode + " " + StatusCodes.GetDescription(response.ResponseCode);
                string message = response.ResponseCode + " " +
                                 (_errorMessage ?? StatusCodes.GetDescription(response.ResponseCode));
                writer.WriteLine("<html><head><title>" + message + "</title></head>");
                writer.WriteLine("<body><h2>" + message + "</h2>");
                if (_errorMessage != null)
                    writer.WriteLine(_errorMessage);
                writer.WriteLine("<hr>" + _client.server.ServerName);
                writer.WriteLine("</body></html>");

                writer.Flush();
                response.ContentType = ContentTypes.GetExtensionType(".html");
                response.ResponseContent = stream;
            }

            response.WriteOutput();
        }

        #endregion

        #region Headers

        private static int maxHeaderLines = 30;
        private readonly HttpProtocol protocol = HttpProtocol.Http;
        private string accept;
        private string acceptLanguage;

        private ConnectionMode connMode;
        private long contentLength = 0;
        private string contentType;
        private DateTime date = DateTime.MinValue;
        private string host;
        private string httpVersion = "1.1";
        private DateTime ifModifiedSince = DateTime.MinValue;
        private DateTime ifUnmodifiedSince = DateTime.MinValue;

        private string method;
        private string password;
        private NameValueCollection query;
        private ByteRange[] ranges;

        private Uri uri;
        private string userAgent;
        private string username;

        /// <summary>
        /// Gets or sets the maximum allowed headers per each request.
        /// </summary>
        public static int MaxHeaderLines
        {
            get { return maxHeaderLines; }
            set { maxHeaderLines = value; }
        }

        /// <summary>
        /// Gets the <see cref="ConnectionMode"/> of the request.
        /// </summary>
        public ConnectionMode ConnectionMode
        {
            get { return connMode; }
        }

        /// <summary>
        /// Gets the HTTP <see cref="Method"/> of the request.
        /// </summary>
        public string Method
        {
            get { return method; }
        }

        /// <summary>
        /// Gets the <see cref="Uri"/> requested by the client.
        /// </summary>
        public Uri Uri
        {
            get { return uri; }
        }

        /// <summary>
        /// Gets the parsed URI queries.
        /// </summary>
        public NameValueCollection Query
        {
            get
            {
                if (query == null)
                    query = new UriQuery(uri);

                return query;
            }
        }

        /// <summary>
        /// Gets the HTTP version of the request.
        /// </summary>
        public string HttpVersion
        {
            get { return httpVersion; }
        }

        /// <summary>
        /// Gets the time the request was received, as noted by the client.
        /// </summary>
        public DateTime Date
        {
            get { return date; }
        }

        /// <summary>
        /// Gets the host requested by the client.
        /// </summary>
        public string Host
        {
            get { return host; }
        }

        /// <summary>
        /// Gets the MIME content-type of the POST data of the request.
        /// </summary>
        public string ContentType
        {
            get { return contentType; }
        }

        /// <summary>
        /// Gets the length of the POST data in bytes.
        /// </summary>
        public long ContentLength
        {
            get { return contentLength; }
        }

        /// <summary>
        /// Gets a list of MIME types accepted by the client.
        /// </summary>
        public string Accept
        {
            get { return accept; }
        }

        /// <summary>
        /// Gets the list of languages accepted by the client.
        /// </summary>
        public string AcceptLanguage
        {
            get { return acceptLanguage; }
        }

        /// <summary>
        /// Gets the client software used by the client.
        /// </summary>
        public string UserAgent
        {
            get { return userAgent; }
        }

        /// <summary>
        /// Gets the time to which the request should be cancelled if the requested resource has not been modified since.
        /// </summary>
        public DateTime IfModifiedSince
        {
            get { return ifModifiedSince; }
        }

        /// <summary>
        /// Gets the time to which the request should be cancelled if the requested resource has been modified since.
        /// </summary>
        public DateTime IfUnmodifiedSince
        {
            get { return ifUnmodifiedSince; }
        }

        /// <summary>
        /// Gets the requested response content ranges.
        /// </summary>
        public ByteRange[] Ranges
        {
            get { return ranges; }
        }

        /// <summary>
        /// Gets a value specifying the protocol (HTTP or HTTPS).
        /// </summary>
        public HttpProtocol Protocol
        {
            get { return protocol; }
        }

        /// <summary>
        /// Gets the client's username specified in the request.
        /// </summary>
        public string Username
        {
            get { return username; }
        }

        /// <summary>
        /// Gets the client's password specified in the request.
        /// </summary>
        public string Password
        {
            get { return password; }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the request.
        /// </summary>
        public void Dispose()
        {
            postData.Close();
        }

        #endregion
    }

    /// <summary>
    /// Defines connection mode options
    /// </summary>
    public enum ConnectionMode
    {
        /// <summary>
        /// Persist the connection after the response has been sent to the client.
        /// </summary>
        KeepAlive,
        /// <summary>
        /// Disconnect the client after the response has been sent.
        /// </summary>
        Close
    }

    /// <summary>
    /// Defines available HTTP protocols.
    /// </summary>
    public enum HttpProtocol
    {
        /// <summary>
        /// Normal HTTP.
        /// </summary>
        Http,
        /// <summary>
        /// HTTP with secure extensions.
        /// </summary>
        Https
    }
}