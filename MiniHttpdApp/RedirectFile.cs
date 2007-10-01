using System;
using MiniHttpd;
using MiniHttpd.FileSystem;

namespace MiniHttpdApp
{
    /// <summary>
    /// Summary description for RedirectFile.
    /// </summary>
    [Serializable]
    public class RedirectFile : IFile
    {
        private readonly string _name;
        private readonly IDirectory _parent;
        private readonly string _redirect;

        public RedirectFile(string name, string redirect, IDirectory parent)
        {
            _name = name;
            _redirect = redirect;
            _parent = parent;
        }

        public string Redirect
        {
            get { return _redirect; }
        }

        #region IFile Members

        public void OnFileRequested(HttpRequest request, IDirectory directory)
        {
            request.Response.ResponseCode = "302";
            request.Response.SetHeader("Location", _redirect);
        }

        public string ContentType
        {
            get { return null; }
        }

        public string Name
        {
            get { return _name; }
        }

        public IDirectory Parent
        {
            get { return _parent; }
        }

        public void Dispose()
        {
            // TODO:  Add RedirectFile.Dispose implementation
        }

        #endregion

        public override string ToString()
        {
            return _name;
        }
    }
}