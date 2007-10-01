using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;

namespace MiniHttpd
{
    /// <summary>
    /// Represents an HTTP response to be sent to the client in response to a request.
    /// </summary>
    public class HttpResponse : MarshalByRefObject
    {
        #region Delegates

        /// <summary>
        /// Represents an event that is triggered by an <see>HttpResponse</see> object.
        /// </summary>
        public delegate void ResponseEventHandler(object sender, ResponseEventArgs e);

        #endregion

        private static readonly UTF8Encoding utf8 = new UTF8Encoding(false);
        private readonly Stream _outputStream;

        private readonly HttpRequest _request;
        private long _bytesSent;
        private bool _headSent;
        private bool _isChunked;
        private bool _isImmediate;

        private Stream responseContent;

        ///<summary>
        ///</summary>
        ///<param name="request">The request that this is a response for.</param>
        public HttpResponse(HttpRequest request)
        {
            _request = request;
            _outputStream = request.Client.stream;
        }

        /// <summary>
        /// Gets or sets a stream containing the content to send to the client.
        /// </summary>
        public Stream ResponseContent
        {
            get { return responseContent; }
            set
            {
                if (_headSent)
                    throw new InvalidOperationException("Response headers cannot be changed after they are sent");
                responseContent = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the header has already been sent.
        /// </summary>
        public bool HeadersSent
        {
            get { return _headSent; }
        }

        /// <summary>
        /// Gets the number of bytes of the response object have been sent so far.
        /// </summary>
        public long BytesSent
        {
            get
            {
                ImmediateResponseStream stream = responseContent as ImmediateResponseStream;
                if (stream == null)
                    return _bytesSent;
                return stream.BytesSent;
            }
        }

        /// <summary>
        /// Gets the <see>HttpRequest</see> of which this object is a response to.
        /// </summary>
        public HttpRequest Request
        {
            get { return _request; }
        }

        /// <summary>
        /// Event that is triggered before the response content is sent.
        /// </summary>
        public event ResponseEventHandler SendingResponse;

        /// <summary>
        /// Event that is triggered after the response content is sent.
        /// </summary>
        public event ResponseEventHandler SentResponse;

        private void BeginResponse()
        {
            Encoding enc = Encoding.Default;
            if (enc.Equals(Encoding.UTF8))
                enc = utf8;
            StreamWriter writer = new StreamWriter(_outputStream, enc);
            writer.NewLine = "\r\n";

            SetHeader("Date", DateTime.Now.ToString("r", CultureInfo.InvariantCulture));
            SetHeader("Server", _request.Server.ServerName);
            if (responseContent != null)
            {
                if (GetHeader("Content-Type") == null)
                    SetHeader("Content-Type", "application/octet-stream");
            }

            writer.WriteLine("HTTP/" + _request.HttpVersion + " " + ResponseCode + " " +
                             StatusCodes.GetDescription(ResponseCode));

            foreach (string header in headers)
            {
                if (headers[header] == null)
                    continue;
                writer.WriteLine(header + ": " + headers[header]);
            }

            writer.WriteLine();
            writer.Flush();

            _headSent = true;

            if (SendingResponse != null)
                SendingResponse(this, new ResponseEventArgs(_request.Client, this, ContentLength));
        }

        /// <summary>
        /// Begins an immediate response to the client. This is recommended only for streaming data to HTTP/1.0 clients.
        /// </summary>
        public void BeginImmediateResponse()
        {
//			if(GetHeader("Content-Length") == null)
//			{
//				string transferEncoding = GetHeader("Transfer-Encoding");
//				if(transferEncoding ==  null || string.Compare(transferEncoding, "chunked", true) != 0)
//					throw new HttpRequestException("500", "The content length must be set or the transfer encoding must be set to 'chunked' prior to beginning a response.");
//			}
            BeginResponse();
            _isImmediate = true;
            responseContent = _outputStream;
        }

        /// <summary>
        /// Begins a chunked response and sets <see cref="ResponseContent"/> to a chunked stream to which data can be written and sent immediately to the client.
        /// </summary>
        /// <returns>True if a chunked transmission has begun; otherwise false if ResponseContent will write directly to the response and disconnect at the end.</returns>
        public bool BeginChunkedOutput()
        {
            if (_isChunked)
                return true;

            if (_request.HttpVersion == "1.0")
            {
                BeginImmediateResponse();
                return false;
            }

            _isChunked = true;

            if (_request.Ranges != null && _request.Ranges.Length != 0)
                responseCode = "416";

            SetHeader("Transfer-Encoding", "chunked");

            BeginResponse();

            responseContent = new ChunkedStream(_outputStream);
            return true;
        }

        /// <summary>
        /// A method used to make text responses easier.
        /// Simply just invoke this method ONCE with your response,
        /// it will take care of everything.
        /// </summary>
        /// <param name="response">HTML or just plain text that should be sent back</param>
        /// <param name="isHtml">True if this is html</param>
        public void WriteText(string response, bool isHtml)
        {
            if (isHtml)
                headers.Add("Content-Type", "text/html");
            else
                Headers.Add("Content-Type", "text/plain");

            /*BeginChunkedOutput();
            using (StreamWriter writer = new StreamWriter(responseContent))
            {
                writer.Write(response);
                writer.Flush();
            }*/
            _request.Response.ResponseContent = new MemoryStream();
            StreamWriter writer =
                new StreamWriter(_request.Response.ResponseContent);
            writer.Write(response);
            writer.Flush();
        }

        /// <summary>
        /// A method used to make text responses easier.
        /// Simply just invoke this method ONCE with your response,
        /// it will take care of everything.
        /// </summary>
        /// <param name="response"></param>
        public void WriteXml(string response)
        {
            headers.Add("Content-Type", "application/xml");
            /*
            BeginChunkedOutput();
            using (StreamWriter writer = new StreamWriter(responseContent))
            {
                writer.Write(response);
                writer.Flush();
            }*/
            _request.Response.ResponseContent = new MemoryStream();
            StreamWriter writer =
                new StreamWriter(_request.Response.ResponseContent);
            writer.Write(response);
            writer.Flush();
        }

        private static long GetRangeLen(ByteRange range, Stream stream)
        {
            if (!stream.CanSeek)
                return -1;

            if (range.Last > stream.Length)
                return -1;
            if (range.First > stream.Length)
                return -1;

            if (range.Last == -1)
                return stream.Length - range.First;

            if (range.First == -1)
                return range.Last;

            return (range.Last - range.First) + 1;
        }

        internal void WriteOutput()
        {
            //Finished chunked data, write footer.
            if (_isChunked || _isImmediate)
            {
                if (_isChunked && _request.HttpVersion != "1.0")
                {
                    byte[] data = Encoding.UTF8.GetBytes("0;\r\n\r\n");
                    _outputStream.Write(data, 0, data.Length);
                    _outputStream.Flush();
                }
                if (SentResponse != null)
                    SentResponse(this, new ResponseEventArgs(_request.Client, this));
                return;
            }

            bool rangesValid = false;
            long totalLen = 0;
            if (responseContent != null && _request.Ranges != null && _request.Ranges.Length != 0)
            {
                if (responseCode == null || responseCode[0] == '2')
                    responseCode = "206"; //Partial Content

                foreach (ByteRange range in _request.Ranges)
                {
                    long len = GetRangeLen(range, responseContent);
                    if (len != -1)
                    {
                        rangesValid = true;
                        totalLen += len;
                    }
                }

                if (!rangesValid && responseCode != null && responseCode == "206")
                    responseCode = "416"; //Requested range not satisfiable
            }

            if (responseContent != null)
            {
                if (rangesValid)
                    ContentLength = totalLen;
                else
                    ContentLength = responseContent.Length;
            }

            BeginResponse();

            if (_request.Method == "HEAD")
            {
                responseContent.Close();
                return;
            }

            if (responseContent != null)
            {
                try
                {
                    byte[] buffer;
                    if (responseContent.CanSeek)
                    {
                        if (responseContent.Length < 1024 * 4)
                            buffer = new byte[responseContent.Length];
                        else
                            buffer = new byte[1024 * 4];
                    }
                    else
                        buffer = new byte[1024 * 4];

                    if (rangesValid)
                    {
                        foreach (ByteRange range in _request.Ranges)
                        {
                            long len = GetRangeLen(range, responseContent);
                            if (len == -1)
                                continue;

                            if (range.Last == -1)
                                responseContent.Seek(range.First, SeekOrigin.Begin);
                            else if (range.First == -1)
                                responseContent.Seek(-range.Last, SeekOrigin.End);
                            else
                                responseContent.Seek(range.First, SeekOrigin.Begin);

                            int bufLen;
                            while (
                                (bufLen =
                                 responseContent.Read(buffer, 0, (int) len < buffer.Length ? (int) len : buffer.Length)) !=
                                0)
                            {
                                _outputStream.Write(buffer, 0, bufLen);
                                len -= bufLen;
                            }
                        }
                    }
                    else
                    {
                        responseContent.Seek(0, SeekOrigin.Begin);
                        try
                        {
                            int len;
                            while ((len = responseContent.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                _outputStream.Write(buffer, 0, len);
                                _bytesSent += len;
                            }
                        }
                        catch (IOException)
                        {
                            Request.Client.Disconnect();
                        }
                    }
                }
                finally
                {
                    if (SentResponse != null)
                        SentResponse(this, new ResponseEventArgs(Request.Client, this));
                    responseContent.Close();
                }
            }
        }

        #region Headers

        private readonly NameValueCollection headers = new NameValueCollection(new CaseInsensitiveEqualityComparer());

        private string responseCode;

        private NameValueCollection Headers
        {
            get { return headers; }
        }

        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        public string ResponseCode
        {
            get
            {
                if (responseCode != null)
                    return responseCode;
                else
                    return _request.StatusCode;
            }
            set
            {
                if (_headSent)
                    throw new InvalidOperationException("Response code cannot be changed after they are sent");
                responseCode = value;
            }
        }

        /// <summary>
        /// Gets or sets the MIME content-type of the response content.
        /// </summary>
        public string ContentType
        {
            get { return GetHeader("Content-Type"); }
            set { SetHeader("Content-Type", value); }
        }

        /// <summary>
        /// Gets or sets the length of the response content.
        /// </summary>
        public long ContentLength
        {
            get
            {
                try
                {
                    string lengthString = GetHeader("Content-Length");
                    if (lengthString == null)
                        return -1;
                    return long.Parse(lengthString, NumberStyles.Number, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    return -1;
                }
            }
            set { SetHeader("Content-Length", value.ToString()); }
        }

        /// <summary>
        /// Returns the value of the specified header.
        /// </summary>
        /// <param name="name">The name of the header value to be returned.</param>
        /// <returns>The value of the specified header.</returns>
        public string GetHeader(string name)
        {
            return headers[name];
        }

        /// <summary>
        /// Sets the value of a specified header.
        /// </summary>
        /// <param name="name">The name of the header to which to assign a value.</param>
        /// <param name="value">The value to assign to the header.</param>
        public void SetHeader(string name, string value)
        {
            if (_headSent)
                throw new InvalidOperationException("Response headers cannot be changed after they are sent");

            headers[name] = value;
        }

        #endregion
    }
}