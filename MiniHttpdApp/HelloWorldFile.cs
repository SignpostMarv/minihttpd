using System.IO;
using MiniHttpd;
using MiniHttpd.FileSystem;

namespace MiniHttpdApp
{
    public class HelloWorldFile : IFile
    {
        private readonly string _name;
        private readonly IDirectory _parent;

        public HelloWorldFile(string name, IDirectory parent)
        {
            _name = name;
            _parent = parent;
        }

        #region IFile Members

        public void OnFileRequested(HttpRequest request, IDirectory directory)
        {
            // Assign a MemoryStream to hold the response content.
            request.Response.ResponseContent = new MemoryStream();

            // Create a StreamWriter to which we can write some text, and write to it.
            using (StreamWriter writer = new StreamWriter(request.Response.ResponseContent))
            {
                writer.WriteLine("Hello, world!");
            }
        }

        public string ContentType
        {
            get { return ContentTypes.GetExtensionType(".txt"); }
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
            // TODO:  Add HelloWorldFile.Dispose implementation
        }

        #endregion
    }
}