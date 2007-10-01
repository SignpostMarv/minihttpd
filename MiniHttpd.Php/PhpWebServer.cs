using System.Globalization;
using System.IO;
using System.Net;
using MiniHttpd.FileSystem;

namespace MiniHttpd.Php
{
    /// <summary>
    /// Represents a PHP capable web server.
    /// </summary>
    public class PhpWebServer : HttpWebServer
    {
        /// <summary>
        /// Creates a PHP capable web server on the default port, default address and default <see cref="VirtualDirectory"/> root.
        /// </summary>
        public PhpWebServer()
        {
        }

        /// <summary>
        /// Creates a PHP capable web server on the specified port, default address and default <see cref="VirtualDirectory"/> root.
        /// </summary>
        /// <param name="port">An open port of range 1-65535 to listen on. Specify 0 to use any open port.</param>
        public PhpWebServer(int port) : this(port, new VirtualDirectory("/", null))
        {
        }

        /// <summary>
        /// Creates a PHP capable web server on the specified port, specified root and default address.
        /// </summary>
        /// <param name="port">An open port of range 1-65535 to listen on. Specify 0 to use any open port.</param>
        /// <param name="root">An <see cref="IDirectory"/> specifying the root directory. Its <c>Parent</c> must be <c>null</c>.</param>
        public PhpWebServer(int port, IDirectory root) : this(IPAddress.Any, port, root)
        {
        }

        /// <summary>
        /// Creates a PHP capable web server on the specified address, specified port and default <see cref="VirtualDirectory"/> root.
        /// </summary>
        /// <param name="localAddress">The local <see cref="IPAddress"/> to listen on.</param>
        /// <param name="port">An open port of range 1-65535 to listen on. Specify 0 to use any open port.</param>
        public PhpWebServer(IPAddress localAddress, int port)
            : this(localAddress, port, new VirtualDirectory("/", null))
        {
        }

        /// <summary>
        /// Creates a PHP capable web server on the specified address, port and root.
        /// </summary>
        /// <param name="localAddress">The local <see cref="IPAddress"/> to listen on.</param>
        /// <param name="port">An open port of range 1-65535 to listen on. Specify 0 to use any open port.</param>
        /// <param name="root">An <see cref="IDirectory"/> specifying the root directory. Its <c>Parent</c> must be <c>null</c>.</param>
        public PhpWebServer(IPAddress localAddress, int port, IDirectory root) : base(localAddress, port, root)
        {
            DefaultPages.Add("index.php");
            DefaultPages.Add("default.php");
        }

        private static bool IsPhpFile(string path)
        {
            string ext = Path.GetExtension(path);
            if (string.Compare(ext, ".php", true, CultureInfo.InvariantCulture) == 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Returns the file or index page for the given request and resource.
        /// </summary>
        /// <param name="request">The request to which to respond.</param>
        /// <param name="resource">The requested resource.</param>
        protected override void SendFileOrIndex(HttpRequest request, IResource resource)
        {
            for (IResource current = resource; current != null; current = current.Parent)
            {
                if (current is PhpAppDirectory)
                {
                    if (resource is DriveFile)
                    {
                        DriveFile file = resource as DriveFile;
                        if (IsPhpFile(file.Path))
                        {
                            PhpAppDirectory.ProcessRequest(request, file);
                            return;
                        }
                    }
                    else if (resource is IDirectory)
                    {
                        IDirectory targetDir = resource as IDirectory;
                        IFile defaultPage;
                        foreach (string page in DefaultPages)
                        {
                            defaultPage = targetDir.GetFile(page);
                            if (defaultPage != null)
                            {
                                DriveFile file = defaultPage as DriveFile;
                                if (file != null && IsPhpFile(file.Path))
                                    throw new MovedException(request.Uri.AbsolutePath + defaultPage.Name);
                                else
                                {
                                    base.SendFileOrIndex(request, resource);
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            base.SendFileOrIndex(request, resource);
        }
    }
}