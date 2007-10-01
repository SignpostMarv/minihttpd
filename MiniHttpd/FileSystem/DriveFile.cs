using System;
using System.IO;

namespace MiniHttpd.FileSystem
{
    /// <summary>
    /// Represents a physical file on disk.
    /// </summary>
    [Serializable]
    public class DriveFile : IFile, IPhysicalResource
    {
        private readonly IDirectory _parent;
        private readonly string _path;
        private string _name;

        /// <summary>
        /// Creates a new <see cref="DriveFile"/> representing a specified file.
        /// </summary>
        /// <param name="path">The full path of the file.</param>
        /// <param name="parent">The parent directory of the file.</param>
        public DriveFile(string path, IDirectory parent) : this(path, parent, true)
        {
        }

        internal DriveFile(string path, IDirectory parent, bool checkPath)
        {
            if (checkPath)
            {
                path = System.IO.Path.GetFullPath(path);
                if (!File.Exists(path))
                    throw new FileNotFoundException(path + " not found");
                if (path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                    throw new ArgumentException("Path cantains invalid characters.", "path");
            }
            _path = path;
            _parent = parent;
        }

        #region IFile Members

        /// <summary>
        /// Called when the file is requested by a client.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> requesting the file.</param>
        /// <param name="directory">The <see cref="IDirectory"/> of the parent directory.</param>
        public void OnFileRequested(HttpRequest request, IDirectory directory)
        {
            if (request.IfModifiedSince != DateTime.MinValue)
            {
                if (File.GetLastWriteTimeUtc(_path) < request.IfModifiedSince)
                    request.Response.ResponseCode = "304";
                return;
            }
            if (request.IfUnmodifiedSince != DateTime.MinValue)
            {
                if (File.GetLastWriteTimeUtc(_path) > request.IfUnmodifiedSince)
                    request.Response.ResponseCode = "304";
                return;
            }

            if (System.IO.Path.GetFileName(_path).StartsWith("."))
            {
                request.Response.ResponseCode = "403";
                return;
            }

            try
            {
                request.Response.ResponseContent = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (FileNotFoundException)
            {
                request.Response.ResponseCode = "404";
            }
            catch (IOException e)
            {
                request.Response.ResponseCode = "500";
                request.Server.Log.WriteLine(e);
            }
        }

        /// <summary>
        /// The MIME type of the content.
        /// </summary>
        public string ContentType
        {
            get { return ContentTypes.GetExtensionType(System.IO.Path.GetExtension(_path)); }
        }

        /// <summary>
        /// The name of the entry.
        /// </summary>
        public string Name
        {
            get
            {
                if (_parent == null)
                    return null;
                if (_name == null)
                    _name = System.IO.Path.GetFileName(_path);
                return _name;
            }
        }

        /// <summary>
        /// The parent directory of the object.
        /// </summary>
        public IDirectory Parent
        {
            get { return _parent; }
        }

        /// <summary>
        /// DriveFile requires no disposal logic.
        /// </summary>
        public virtual void Dispose()
        {
        }

        #endregion

        #region IPhysicalResource Members

        /// <summary>
        /// Gets the full path of the file on disk.
        /// </summary>
        public string Path
        {
            get { return _path; }
        }

        #endregion

        /// <summary>
        /// Returns the name of the file.
        /// </summary>
        /// <returns>The name of the file.</returns>
        public override string ToString()
        {
            return _name;
        }
    }
}