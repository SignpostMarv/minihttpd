namespace MiniHttpd.FileSystem
{
    /// <summary>
    /// Represents content to be contained in an <see cref="IDirectory"/>.
    /// </summary>
    public interface IFile : IResource
    {
        /// <summary>
        /// Gets the MIME type of the content.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Called when the file is requested by a client.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> requesting the file.</param>
        /// <param name="directory">The <see cref="IDirectory"/> of the parent directory.</param>
        void OnFileRequested(HttpRequest request, IDirectory directory);
    }
}