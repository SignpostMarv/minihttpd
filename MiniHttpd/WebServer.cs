using System;
using System.Collections;
using System.IO;
using MiniHttpd.FileSystem;

namespace MiniHttpd
{
    /// <summary>
    /// Represents a web server with a tree-file structure.
    /// </summary>
    public class VirtualHttpServer
    {
        #region Delegates

        ///<summary>
        /// Used to take care of an incoming query.
        /// Use the method AddUrlHandler to handle urls.
        ///</summary>
        ///<param name="request"></param>
        ///<param name="response"></param>
        public delegate void RequestHandler(HttpRequest request, HttpResponse response);

        #endregion

        /// <summary>
        /// Provides a list of characters deemed invalid in Windows NT file strings.
        /// </summary>
        public static readonly char[] InvalidFileChars = new char[] {'?', '"', '\\', '<', '>', '*', '|', ':'};

        private readonly HttpServer _m_server;
        private readonly Hashtable _m_urls;

        /// <summary>
        /// Creates a web server on the default port, default address and default <see cref="VirtualDirectory"/> root.
        /// </summary>
        public VirtualHttpServer(int port)
        {
            _m_server = new HttpServer(port);
            _m_server.RequestReceived += server_RequestReceived;
            _m_urls = new Hashtable();
        }


        /// <summary>
        /// Used to handle an url.
        /// Note that the directory handlers are getting called for unknown documents.
        /// </summary>
        /// <param name="url">Url (examples: "/", "/index.php"</param>
        /// <param name="handler"></param>
        public void AddUrlHandler(string url, RequestHandler handler)
        {
            if (_m_urls.ContainsKey(url))
                throw new ArgumentException("Url '" + url + "' is already handled.");

            _m_urls.Add(url, handler);
        }

        /// <summary>
        /// Start the webserver
        /// </summary>
        public void Start()
        {
            _m_server.Start();
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop()
        {
            _m_server.Stop();
        }

        /// <summary>
        /// Checks whether a file is a valid filename on the current OS.
        /// </summary>
        /// <param name="filename">The filename to be examined.</param>
        /// <returns>True if the filename is safe, otherwise false.</returns>
        public static bool IsValidFilename(string filename)
        {
            if (filename.IndexOfAny(InvalidFileChars) >= 0 || filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                filename.IndexOf("..") >= 0)
                return false;
            return true;
        }

        private void server_RequestReceived(object sender, RequestEventArgs e)
        {
            if (e.Request.Uri.AbsolutePath.IndexOfAny(new char[] {'*', '?'}) >= 0)
                throw new HttpRequestException("401");

            string pathOnly = PathOnly(e.Request.Uri.AbsolutePath);
            RequestHandler handler = null;
            if (_m_urls.ContainsKey(e.Request.Uri.AbsolutePath))
                handler = (RequestHandler) _m_urls[e.Request.Uri.AbsolutePath];
            else if (_m_urls.ContainsKey(pathOnly))
                handler = (RequestHandler) _m_urls[pathOnly];

            if (handler != null)
                handler.Invoke(e.Request, e.Request.Response);
        }

        /// <summary>
        /// Returns the path part of a URI, /this/is/a/path.xml will return /this/is/a/
        /// </summary>
        /// <param name="uriPath"></param>
        /// <returns></returns>
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

        private static int LastIndexOf(string s, char c)
        {
            return LastIndexOf(s, c, s.Length - 1);
        }

        private static int LastIndexOf(string s, char c, int startPos)
        {
            for (int i = startPos; i >= 0; --i)
            {
                if (s[i] == c)
                    return i;
            }

            return -1;
        }

        ///<summary>
        ///</summary>
        ///<param name="absolutePath"></param>
        ///<returns></returns>
        public static string[] GetUriPathNodes(string absolutePath)
        {
            ArrayList nodes = new ArrayList();
            string path = absolutePath.Replace('\\', '/');
            if (path.Length <= 1)
                return new string[] {UrlEncoding.Decode(path)};
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

            return nodes.ToArray(typeof (string)) as string[];
        }
    }
}