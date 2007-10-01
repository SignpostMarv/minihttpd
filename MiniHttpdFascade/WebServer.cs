using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Specialized;
using MiniHttpd;

namespace WebServer
{
    /// <summary>
    /// Represents a web server with a tree-file structure.
    /// </summary>
    public class HttpServer
    {
        private HttpServer m_server;
        private Hashtable m_urls;

        public delegate void RequestHandler(HttpRequest request, HttpResponse response);

        /// <summary>
        /// Creates a web server on the default port, default address and default <see cref="VirtualDirectory"/> root.
        /// </summary>
        public HttpServer(int port)
        {
            m_server = new HttpServer(port);
            m_server.RequestReceived += this.server_RequestReceived;
            m_urls = new Hashtable();
        }


        /// <summary>
        /// Provides a list of characters deemed invalid in Windows NT file strings.
        /// </summary>
        public static readonly char[] InvalidFileChars = new char[] { '?', '"', '\\', '<', '>', '*', '|', ':' };

        public void AddUrlHandler(string url, RequestHandler handler)
        {
            if (m_urls.ContainsKey(url))
                throw new ArgumentException("Url '" + url + "' is already handled.");

            m_urls.Add(url, handler);
        }

        public void Start()
        {
            m_server.Start();
        }

        public void Stop()
        {
            m_server.Stop();
        }

        /// <summary>
        /// Checks whether a file is a valid filename on the current OS.
        /// </summary>
        /// <param name="filename">The filename to be examined.</param>
        /// <returns>True if the filename is safe, otherwise false.</returns>
        public static bool IsValidFilename(string filename)
        {
            if (filename.IndexOfAny(InvalidFileChars) >= 0 || filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || filename.IndexOf("..") >= 0)
                return false;
            return true;
        }

        private void server_RequestReceived(object sender, RequestEventArgs e)
        {

            if (e.Request.Uri.AbsolutePath.IndexOfAny(new char[] { '*', '?' }) >= 0)
            {
                throw new HttpRequestException("401");
            }

            string pathOnly = PathOnly(e.Request.Uri.AbsolutePath);
            RequestHandler handler = null;
            if (m_urls.ContainsKey(e.Request.Uri.AbsolutePath))
                handler = (RequestHandler)m_urls[e.Request.Uri.AbsolutePath];
            else if (m_urls.ContainsKey(pathOnly))
                handler = (RequestHandler)m_urls[pathOnly];

            if (handler != null)
                handler.Invoke(e.Request, e.Request.Response);
        }

        public static string PathOnly(string uriPath)
        {
            int dotPos = LastIndexOf(uriPath, '.');
            int questionPos = LastIndexOf(uriPath, '?');
            int slashPos = LastIndexOf(uriPath, '/');

            if (dotPos != -1 && (dotPos < questionPos || questionPos == -1))
            {
                if (slashPos != -1)
                    return uriPath.Substring(0, slashPos + 1);

                // hmm.. A dot but no slash?
                return uriPath;
            }

            if (questionPos != -1)
                return uriPath.Substring(0, questionPos);

            return uriPath;
        }

        static int LastIndexOf(string s, char c)
        {
            return LastIndexOf(s, c, s.Length - 1);
        }

        static int LastIndexOf(string s, char c, int startPos)
        {
            for (int i = startPos; i >= 0; --i)
                if (s[i] == c)
                    return i;

            return -1;
        }

        static string[] GetUriPathNodes(string absolutePath)
        {
            ArrayList nodes = new ArrayList();
            string path = absolutePath.Replace('\\', '/');
            if (path.Length <= 1)
                return new string[] { UrlEncoding.Decode(path) };
            //	return new string[] {path};

            int start = 0;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                {
                    while (i < path.Length && path[i] == '/')
                        i++;
                    nodes.Add(UrlEncoding.Decode(path.Substring(start, i - start)));
                    //nodes.Add(path.Substring(start, i-start));
                    start = i;
                }
            }

            if (start != path.Length)
                nodes.Add(UrlEncoding.Decode(path.Substring(start, path.Length - start)));
            //nodes.Add(path.Substring(start, path.Length-start));

            return nodes.ToArray(typeof(string)) as string[];
        }
    }
}