using System;
using System.Collections;

namespace MiniHttpd.FileSystem
{
    /// <summary>
    /// Represents a directory which can be populated with subdirectories and files programmatically.
    /// </summary>
    [Serializable]
    public class VirtualDirectory : IDirectory
    {
        private readonly Hashtable _directories;
        private readonly Hashtable _files;
        private readonly string _name;
        private readonly IDirectory _parent;

        /// <summary>
        /// Creates a new virtual directory.
        /// </summary>
        /// <param name="name">The name of the directory as it will be seen in the directory.</param>
        /// <param name="parent">An <see cref="IDirectory" /> specifying the parent directory. This value should be <c>null</c> if this directory is to be the root directory.</param>
        public VirtualDirectory(string name, IDirectory parent)
        {
            _name = name;
            _parent = parent;
            _directories = new Hashtable();
            _files = new Hashtable();
        }

        /// <summary>
        /// Creates a new virtual directory to use as the root directory.
        /// </summary>
        public VirtualDirectory() : this("/", null)
        {
        }

        #region IDirectory Members

        /// <summary>
        /// Returns the specified subdirectory.
        /// </summary>
        /// <param name="dir">The name of the subdirectory to retrieve.</param>
        /// <returns>An <see cref="IDirectory"/> representing the specified directory, or <c>null</c> if one doesn't exist.</returns>
        public IDirectory GetDirectory(string dir)
        {
            if (dir == "/")
                return this;
            return _directories[dir] as IDirectory;
        }

        /// <summary>
        /// Returns the specified file.
        /// </summary>
        /// <param name="filename">The name of the file to retrieve.</param>
        /// <returns>An <see cref="IFile"/> representing the specified file, or <c>null</c> if one doesn't exist.</returns>
        public IFile GetFile(string filename)
        {
            return _files[filename] as IFile;
        }

        /// <summary>
        /// Returns a collection of subdirectories available in the directory.
        /// </summary>
        /// <returns>An <see cref="ICollection"/> containing <see cref="IDirectory"/> objects available in the directory.</returns>
        public ICollection GetDirectories()
        {
            return _directories.Values;
        }

        /// <summary>
        /// Returns a collection of files available in the directory.
        /// </summary>
        /// <returns>An <see cref="ICollection"/> containing <see cref="IFile"/> objects available in the directory.</returns>
        public ICollection GetFiles()
        {
            return _files.Values;
        }

        /// <summary>
        /// Gets the name of the directory.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the parent directory of the directory.
        /// </summary>
        public IDirectory Parent
        {
            get { return _parent; }
        }

        /// <summary>
        /// Returns the resource with the given name.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        /// <returns>A resource with the given name or <c>null</c> if one isn't available.</returns>
        public IResource GetResource(string name)
        {
            IFile file = GetFile(name);
            if (file != null)
                return file;
            return GetDirectory(name);
        }

        /// <summary>
        /// Dispose all containing files and directories.
        /// </summary>
        public virtual void Dispose()
        {
            foreach (IFile file in _files.Values)
                file.Dispose();

            foreach (IDirectory dir in _directories.Values)
                dir.Dispose();

            _files.Clear();
            _directories.Clear();
        }

        #endregion

        private void CheckExistence(string name)
        {
            if (_directories.ContainsKey(name))
                throw new DirectoryException("A directory of the name \"" + name + "\" already exists");
            if (_files.ContainsKey(name))
                throw new DirectoryException("A file of the name \"" + name + "\" already exists");
        }

        /// <summary>
        /// Adds a subdirectory to the directory.
        /// </summary>
        /// <param name="directory">An <see cref="IDirectory" /> specifying the directory to add. The directory's parent must be the directory to which the directory is added.</param>
        public void AddDirectory(IDirectory directory)
        {
            CheckExistence(directory.Name);

            if (directory.Parent != this)
                throw new DirectoryException("The directory's parent must be the directory to which it is added");

            _directories.Add(directory.Name, directory);
        }

        /// <summary>
        /// Adds a physical subdirectory to the directory.
        /// </summary>
        /// <param name="path">The full path of the directory to add.</param>
        /// <returns>The newly created <see cref="DriveDirectory"/>.</returns>
        public DriveDirectory AddDirectory(string path)
        {
            DriveDirectory directory = new DriveDirectory(path, this);
            CheckExistence(directory.Name);

            _directories.Add(directory.Name, directory);

            return directory;
        }

        /// <summary>
        /// Adds a physical subdirectory to the directory with a specific name.
        /// </summary>
        /// <param name="alias">The name of the subdirectory to add as it will be seen in the directory.</param>
        /// <param name="path">The full path of the directory to add.</param>
        /// <returns>The newly created <see cref="DriveDirectory"/>.</returns>
        public DriveDirectory AddDirectory(string alias, string path)
        {
            DriveDirectory directory = new DriveDirectory(alias, path, this);
            CheckExistence(directory.Name);

            _directories.Add(directory.Name, directory);

            return directory;
        }

        /// <summary>
        /// Adds a physical file to the directory.
        /// </summary>
        /// <param name="path">The full path of the file to add.</param>
        /// <returns>The newly created <see cref="DriveFile"/>.</returns>
        public DriveFile AddFile(string path)
        {
            DriveFile file = new DriveFile(path, this);
            AddFile(file);
            return file;
        }

        /// <summary>
        /// Adds a file to the directory.
        /// </summary>
        /// <param name="file">An <see cref="IFile"/> representing the file to add. The file's parent must be the directory to which the file is added.</param>
        public void AddFile(IFile file)
        {
            CheckExistence(file.Name);

            if (file.Parent != this)
                throw new DirectoryException("The file's must be the directory to which it is added.");

            _files.Add(file.Name, file);
        }

        /// <summary>
        /// Removes a file or subdirectory from the directory.
        /// </summary>
        /// <param name="name">The name of the file or subdirectory to remove.</param>
        public void Remove(string name)
        {
            if (_files.ContainsKey(name))
            {
                ((IFile) _files[name]).Dispose();
                _files.Remove(name);
            }
            else if (_directories.Contains(name))
            {
                ((IDirectory) _directories[name]).Dispose();
                _directories.Remove(name);
            }
        }

        /// <summary>
        /// Returns the name of the directory.
        /// </summary>
        /// <returns>The name of the directory.</returns>
        public override string ToString()
        {
            return _name;
        }
    }
}