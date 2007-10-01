using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Web.Hosting;
using System.Xml;
using MiniHttpd.FileSystem;

namespace MiniHttpd.Aspx
{
    /// <summary>
    /// Represents a directory containing an ASPX web application.
    /// </summary>
    [Serializable]
    public class AspxAppDirectory : DriveDirectory
    {
        private readonly FileSystemWatcher _configFileWatcher = new FileSystemWatcher();
        private readonly ArrayList _httpHandlers = new ArrayList();
        private readonly string _virtPath;
        private AspxAppHost _appHost;

        private string _binFolder;
        [NonSerialized] private XmlDocument _configFile;

        /// <summary>
        /// Creates a new <see cref="AspxAppDirectory"/> with the specified path and parent.
        /// </summary>
        /// <param name="path">The full path of the web application root.</param>
        /// <param name="parent">The parent directory to which this directory will belong.</param>
        public AspxAppDirectory(string path, IDirectory parent) : base(path, parent)
        {
            _virtPath = HttpWebServer.GetDirectoryPath(this);

            CreateAssemblyInBin(path);

            CreateAppHost();

            _configFileWatcher.Path = path;
            _configFileWatcher.Filter = "Web.config";
            _configFileWatcher.Created += configFileWatcher_Changed;
            _configFileWatcher.Changed += configFileWatcher_Changed;
            _configFileWatcher.Deleted += configFileWatcher_Changed;
            _configFileWatcher.Renamed += configFileWatcher_Renamed;

            _configFileWatcher.EnableRaisingEvents = true;

            LoadWebConfig(System.IO.Path.Combine(path, "Web.config"));
        }

        /// <summary>
        /// Creates a root <see cref="AspxAppDirectory"/> with the specified path.
        /// </summary>
        /// <param name="path">The full path of the directory on disk.</param>
        public AspxAppDirectory(string path) : this(path, null)
        {
        }

        internal string VirtualPath
        {
            get { return _virtPath; }
        }

        /// <summary>
        /// Shut down app domain and delete bin/minihttpd.dll.
        /// </summary>
        public override void Dispose()
        {
            _appHost.Unload();
            if (_binFolder != null)
            {
                if (Directory.Exists(_binFolder))
                {
                    string assemblyPath = System.IO.Path.Combine(
                        _binFolder,
                        System.IO.Path.GetFileName(
                            new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath));
                    if (File.Exists(assemblyPath))
                        File.Delete(assemblyPath);

                    if (Directory.GetFileSystemEntries(_binFolder).Length == 0)
                        Directory.Delete(_binFolder);
                }
            }
            base.Dispose();
        }

        private void CreateAppHost()
        {
            _appHost = ApplicationHost.CreateApplicationHost(typeof (AspxAppHost), _virtPath, Path) as AspxAppHost;
        }

        internal void ProcessRequest(HttpRequest request, IFile file)
        {
            DriveFile dfile = file as DriveFile;
            if (dfile == null)
                throw new ArgumentException("File must be available on disk.");
            try
            {
                _appHost.ProcessRequest(request, dfile, _virtPath, Path);
            }
            catch (AppDomainUnloadedException)
            {
                CreateAppHost();
                ProcessRequest(request, file);
            }
        }

        /// <summary>
        /// Copies the host assembly to the <c>bin</c> folder of the web application if it doesn't exist in the GAC.
        /// The assembly is needed by ASP.NET to access from the web app's domain.
        /// </summary>
        /// <param name="appPath">The full path of the web application directory.</param>
        private void CreateAssemblyInBin(string appPath)
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();

            if (!thisAssembly.GlobalAssemblyCache)
            {
                string copiedAssemblyPath = null;
                try
                {
                    // Create the folder if it doesn't exist, flag it as hidden
                    _binFolder = System.IO.Path.Combine(appPath, "bin");
                    if (!Directory.Exists(_binFolder))
                    {
                        Directory.CreateDirectory(_binFolder);
                        File.SetAttributes(_binFolder, FileAttributes.Hidden);
                    }

                    //TODO: implement httphandlers, lock httpHandlers

                    // Delete the file if it exists, copy to bin
                    string assemblyPath = new Uri(thisAssembly.CodeBase).LocalPath;
                    copiedAssemblyPath = System.IO.Path.Combine(_binFolder, System.IO.Path.GetFileName(assemblyPath));
                    if (File.Exists(copiedAssemblyPath))
                        File.Delete(copiedAssemblyPath);
                    File.Copy(assemblyPath, copiedAssemblyPath);
                }
                catch (IOException)
                {
                    if (!File.Exists(copiedAssemblyPath))
                        throw;

                    if (thisAssembly.FullName != AssemblyName.GetAssemblyName(copiedAssemblyPath).FullName)
                        throw;
                }
            }
        }

        private void LoadWebConfig(string path)
        {
            try
            {
                _httpHandlers.Clear();
                _configFile = new XmlDocument();
                _configFile.Load(path);

                XmlNode handlersNode =
                    _configFile.DocumentElement.SelectSingleNode("/configuration/system.web/httpHandlers");
                if (handlersNode == null)
                    return;

                lock (_httpHandlers)
                {
                    foreach (XmlNode node in handlersNode)
                    {
                        switch (node.Name)
                        {
                            case "add":
                                {
                                    if (node.Attributes["verb"] == null)
                                        break;
                                    if (node.Attributes["path"] == null)
                                        break;
                                    if (node.Attributes["type"] == null)
                                        break;

                                    bool validate = false;

                                    try
                                    {
                                        if (node.Attributes["validate"] != null)
                                            validate = bool.Parse(node.Attributes["validate"].Value);
                                    }
                                    catch (FormatException)
                                    {
                                        validate = false;
                                    }

                                    HttpHandler handler =
                                        new HttpHandler(node.Attributes["verb"].Value, node.Attributes["path"].Value,
                                                        node.Attributes["type"].Value, validate);
                                    _httpHandlers.Remove(handler);
                                    _httpHandlers.Add(handler);

                                    break;
                                }
                            case "remove":
                                {
                                    if (node.Attributes["verb"] == null)
                                        break;
                                    if (node.Attributes["path"] == null)
                                        break;

                                    HttpHandler handler =
                                        new HttpHandler(node.Attributes["verb"].Value, node.Attributes["path"].Value,
                                                        null, false);
                                    _httpHandlers.Remove(handler);

                                    break;
                                }
                            case "clear":
                                {
                                    _httpHandlers.Clear();

                                    break;
                                }
                        }
                    }
                }
            }
            catch (Exception)
            {
                _httpHandlers.Clear();
                _configFile = null;
            }
        }

        private void configFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            LoadWebConfig(e.FullPath);
        }

        private void configFileWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            LoadWebConfig(e.FullPath);
        }
    }
}