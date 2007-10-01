using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using MiniHttpd.FileSystem;

namespace MiniHttpd.Php
{
    /// <summary>
    /// Represents a directory containing a PHP web application.
    /// </summary>
    [Serializable]
    public class PhpAppDirectory : DriveDirectory
    {
        private readonly string _virtPath;

        /// <summary>
        /// Creates a new <see cref="PhpAppDirectory"/> with the specified path and parent.
        /// </summary>
        /// <param name="path">The full path of the web application root.</param>
        /// <param name="parent">The parent directory to which this directory will belong.</param>
        public PhpAppDirectory(string path, IDirectory parent) : base(path, parent)
        {
            _virtPath = HttpWebServer.GetDirectoryPath(this);
        }

        /// <summary>
        /// Creates a root <see cref="PhpAppDirectory"/> with the specified path.
        /// </summary>
        /// <param name="path">The full path of the directory on disk.</param>
        public PhpAppDirectory(string path) : this(path, null)
        {
        }

        internal string VirtualPath
        {
            get { return _virtPath; }
        }

        internal static void ProcessRequest(HttpRequest request, IFile file)
        {
            if (!(file is DriveFile))
                throw new ArgumentException("File must be available on disk.");

            if (request.Uri.AbsoluteUri.IndexOf("PHPE9568F") > 0)
            {
                ServePHPImage(request);
                return;
            }

            request.Response.ContentType = "text/html";
            request.Response.BeginChunkedOutput();
            StreamWriter swData = new StreamWriter(request.Response.ResponseContent);

            int RequestID = InitRequest();

            RegisterVariable(RequestID, "QueryString", request.Uri.AbsoluteUri);

            StreamReader srPostData = new StreamReader(request.PostData);
            string strPostData = srPostData.ReadToEnd();
            RegisterVariable(RequestID, "PostData", strPostData);

            ExecutePHP(RequestID, (file as DriveFile).Path);

            StringBuilder builder = new StringBuilder();
            builder.Capacity = GetResultText(RequestID, builder, 0);
            GetResultText(RequestID, builder, builder.Capacity + 1);
            swData.Write(builder.ToString());
            DoneRequest(RequestID);

            swData.Flush();
            swData.Close();
        }

        private static void ServePHPImage(HttpRequest request)
        {
            request.Response.ContentType = ContentTypes.GetExtensionType(".gif");

            request.Response.ResponseContent =
                Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    request.Uri.AbsoluteUri.IndexOf("PHPE9568F34") > 0
                        ? "MiniHttpd.Php.php.gif"
                        : "MiniHttpd.Php.zend1.gif");
        }


        /// <summary>
        /// Initializes the PHP request
        /// </summary>
        /// <returns>RequestID for further interaction</returns>
        [DllImport("PHP4App.dll")]
        public static extern int InitRequest();

        /// <summary>
        /// Calls the PHP engine for a file
        /// </summary>
        /// <param name="RequestID">Request (from initialization)</param>
        /// <param name="FileName">Full path to the file</param>
        /// <returns>0 for successful execution</returns>
        [DllImport("PHP4App.dll")]
        public static extern int ExecutePHP(int RequestID, string FileName);

        /// <summary>
        /// Calls the PHP engine with code (not a file)
        /// </summary>
        /// <param name="RequestID">Request (from initialization)</param>
        /// <param name="ACode">Code to run on the PHP engine</param>
        /// <returns>0 for successful execution</returns>
        [DllImport("PHP4App.dll")]
        public static extern int ExecuteCode(int RequestID, string ACode);

        /// <summary>
        /// Closes request and finalizes memory usage
        /// </summary>
        /// <param name="RequestID">Request (from initialization)</param>
        [DllImport("PHP4App.dll", SetLastError=true)]
        public static extern void DoneRequest(int RequestID);

        /// <summary>
        /// Retrieves the results from the PHP engine
        /// </summary>
        /// <param name="RequestID">Request (from initialization)</param>
        /// <param name="Buf">Buffer to contain the results</param>
        /// <param name="BufLen">Capacity of the buffer</param>
        /// <returns></returns>
        [DllImport("PHP4App.dll", SetLastError=true)]
        public static extern int GetResultText(int RequestID, StringBuilder Buf, int BufLen);

        /// <summary>
        /// Writes a variable to the PHP engine for use in the request
        /// </summary>
        /// <param name="RequestID">Request (from initialization)</param>
        /// <param name="AName">Name of the variable to register</param>
        /// <param name="AValue">Value of the variable</param>
        [DllImport("PHP4App.dll", SetLastError=true)]
        public static extern void RegisterVariable(int RequestID, string AName, string AValue);

        /// <summary>
        /// Retrieves the value of a registered variable
        /// </summary>
        /// <param name="RequestID">Request (from initialization)</param>
        /// <param name="AName">Name of the variable to retrieve</param>
        /// <param name="Buf">Buffer to contain the results</param>
        /// <param name="BufLen">Capacity of the buffer</param>
        /// <returns></returns>
        [DllImport("PHP4App.dll", SetLastError=true)]
        public static extern int GetVariable(int RequestID, string AName, StringBuilder Buf, int BufLen);
    }
}