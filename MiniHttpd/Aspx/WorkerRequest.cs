using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web;
using Microsoft.Win32.SafeHandles;
using MiniHttpd.FileSystem;

namespace MiniHttpd.Aspx
{
    /// <summary>
    /// Summary description for WorkerRequest.
    /// </summary>
    internal class WorkerRequest : HttpWorkerRequest
    {
        private readonly DriveFile _file;
        private readonly string _physicalDir;
        private readonly HttpRequest _request;
        private readonly string _virtualDir;
        private bool _firstSend = true;

        public WorkerRequest(HttpRequest request, DriveFile file, string virtualDir, string physicalDir)
        {
            _request = request;
            _file = file;
            _virtualDir = virtualDir;
            _physicalDir = physicalDir;
        }

        public override string MachineConfigPath
        {
            get
            {
                return
                    Path.Combine(
                        Path.Combine(Path.GetDirectoryName(typeof (object).Assembly.Location.Replace('/', '\\')),
                                     "Config"), "machine.config");
            }
        }

        public override string MachineInstallDirectory
        {
            get { return Thread.GetDomain().GetData(".hostingInstallDir").ToString(); }
        }

        public override void EndOfRequest()
        {
        }

        public override void FlushResponse(bool finalFlush)
        {
        }

        public override string GetHttpVerbName()
        {
            return _request.Method;
        }

        public override string GetHttpVersion()
        {
            return "HTTP/" + _request.HttpVersion;
        }

        public override string GetLocalAddress()
        {
            return _request.Server.ServerUri.Host;
        }

        public override int GetLocalPort()
        {
            return _request.Server.Port;
        }

        public override string GetQueryString()
        {
            return _request.Uri.Query;
        }

        public override byte[] GetQueryStringRawBytes()
        {
            return Encoding.Default.GetBytes(_request.Uri.Query);
        }

        public override string GetRawUrl()
        {
            return _request.Uri.PathAndQuery;
        }

        public override string GetRemoteAddress()
        {
            return _request.Client.RemoteAddress;
        }

        public override int GetRemotePort()
        {
            return _request.Client.RemotePort;
        }

        public override string GetServerVariable(string name)
        {
            return string.Empty;
        }

        public override string GetUriPath()
        {
            return _request.Uri.AbsolutePath;
        }

        public override void SendKnownResponseHeader(int index, string value)
        {
            _request.Response.SetHeader(GetKnownResponseHeaderName(index), value);
        }

        private void SendResponseFromFile(Stream stream, long offset, long length)
        {
//			if(firstSend)
//			{
//				request.Response.BeginImmediateResponse();
//				firstSend = false;
//			}

            stream.Seek(offset, SeekOrigin.Begin);
            byte[] buffer = new byte[1024];
            int readLength;

            while ((readLength = stream.Read(buffer, 0, (int) (length < buffer.Length ? length : buffer.Length))) != 0)
            {
                _request.Response.ResponseContent.Write(buffer, 0, readLength);
                length -= readLength;
            }
        }

        public override void SendResponseFromFile(string filename, long offset, long length)
        {
            SendResponseFromFile(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), offset,
                                 length);
        }

        public override void SendResponseFromFile(IntPtr handle, long offset, long length)
        {
            SendResponseFromFile(new FileStream(new SafeFileHandle(handle, false), FileAccess.Read, 1024), offset,
                                 length);
        }

        public override void SendResponseFromMemory(byte[] data, int length)
        {
            if (_firstSend)
            {
                _request.Response.BeginImmediateResponse();
                _firstSend = false;
            }
            _request.Response.ResponseContent.Write(data, 0, length);
        }

        public override void SendResponseFromMemory(IntPtr data, int length)
        {
            if (_firstSend)
            {
                _request.Response.BeginImmediateResponse();
                _firstSend = false;
            }

            byte[] buf = new byte[length];
            Marshal.Copy(data, buf, 0, length);
            SendResponseFromMemory(buf, length);
        }

        public override void SendStatus(int statusCode, string statusDescription)
        {
            _request.Response.ResponseCode = statusCode.ToString();
        }

        public override void SendUnknownResponseHeader(string name, string value)
        {
            _request.Response.SetHeader(name, value);
        }

        public override void SendCalculatedContentLength(int contentLength)
        {
            _request.Response.SetHeader("Content-Length", contentLength.ToString(CultureInfo.InvariantCulture));
        }

        public override string GetAppPath()
        {
            return _virtualDir;
        }

        public override string GetAppPathTranslated()
        {
            return _physicalDir;
        }

        public override void CloseConnection()
        {
            _request.Client.Disconnect();
        }

        public override string GetFilePath()
        {
            return _request.Uri.AbsolutePath;
        }

        public override string GetFilePathTranslated()
        {
            return _file.Path;
        }

        public override string GetKnownRequestHeader(int index)
        {
            return _request.Headers[GetKnownRequestHeaderName(index)];
        }

        public override byte[] GetPreloadedEntityBody()
        {
            return _request.PostData.ToArray();
        }

        public override int ReadEntityBody(byte[] buffer, int size)
        {
            //TODO: implement this
            return 0;
        }

        public override string GetProtocol()
        {
            return _request.Protocol.ToString().ToUpper(CultureInfo.InvariantCulture);
        }

        public override string GetUnknownRequestHeader(string name)
        {
            return _request.Headers[name];
        }

        public override string[][] GetUnknownRequestHeaders()
        {
            ArrayList ret = new ArrayList();
            string[] keys = _request.Headers.AllKeys;

            foreach (string key in keys)
            {
                if (GetKnownRequestHeaderIndex(key) < 0)
                    ret.Add(new string[] {key, _request.Headers[key]});
            }

            return ret.ToArray(typeof (string[])) as string[][];
        }

        public override bool HeadersSent()
        {
            return _request.Response.HeadersSent;
        }

        public override bool IsClientConnected()
        {
            return _request.Client.IsConnected;
        }

        public override bool IsEntireEntityBodyIsPreloaded()
        {
            return _request.ContentLength == _request.PostData.Length;
        }

        public override bool IsSecure()
        {
            //TODO: HTTPS
            return false;
        }

        public override string MapPath(string virtualPath)
        {
            IPhysicalResource resource =
                ((AspxWebServer) _request.Server).NavigateToUrl(virtualPath) as IPhysicalResource;
            if (resource == null)
                return null;
            return resource.Path;
        }

        public override string GetPathInfo()
        {
            //TODO: implement this once HttpHandlers is ready
            return string.Empty;
        }
    }
}