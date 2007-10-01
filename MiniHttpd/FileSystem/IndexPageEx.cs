using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MiniHttpd.FileSystem
{
    /// <summary>
    /// A slightly fancy index page used by <see cref="HttpWebServer"/>.
    /// </summary>
    public class IndexPageEx : IndexPage
    {
        #region ResourceColumn enum

        /// <summary>
        /// Specifies resource columns to display or sort by.
        /// </summary>
        public enum ResourceColumn
        {
            /// <summary>
            /// No sort.
            /// </summary>
            None,
            /// <summary>
            /// The date and time the resource was created.
            /// </summary>
            Created,
            /// <summary>
            /// The date and time the resource was modified.
            /// </summary>
            Modified,
            /// <summary>
            /// The size of the resource.
            /// </summary>
            Size,
            /// <summary>
            /// The name of the resource.
            /// </summary>
            Name
        }

        #endregion

        private ResourceColumn[] columns = {ResourceColumn.Modified, ResourceColumn.Size, ResourceColumn.Name};

        /// <summary>
        /// Gets or sets the list of columns to display in the index page.
        /// </summary>
        public ResourceColumn[] Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        internal override void PrintBody(StreamWriter writer,
                                         HttpRequest request,
                                         IDirectory directory,
                                         ICollection dirs,
                                         ICollection files
            )
        {
            bool reverse = request.Query["desc"] != null;

            ResourceColumn sort = ResourceColumn.None;

            try
            {
                string sortString = request.Query["sort"];
                if (sortString != null && sortString != string.Empty)
                    sort = (ResourceColumn) Enum.Parse(typeof (ResourceColumn), sortString);
            }
            catch (ArgumentException)
            {
            }

            writer.WriteLine("<h2>Index of " + MakeLinkPath(directory, request) + "</h2>");

            writer.WriteLine("<table cellspacing=\"0\">");

            writer.WriteLine("<tr>");
            foreach (ResourceColumn column in columns)
            {
                writer.Write(GetColumnTd(column) + "<b><a href=\"" + "." + "?sort=" + column);
                if (sort == column && !reverse)
                    writer.Write("&desc");
                writer.Write("\"/>");
                writer.WriteLine(column + "</a></b></td>");
            }
            writer.WriteLine("</tr>");

            ArrayList entries = new ArrayList(dirs.Count + files.Count);

            foreach (IDirectory dir in dirs)
                entries.Add(new ResourceEntry(dir));
            foreach (IFile file in files)
                entries.Add(new ResourceEntry(file));

            if (sort != ResourceColumn.None)
                entries.Sort(new ResourceComparer(reverse, sort));

            foreach (ResourceEntry entry in entries)
                entry.WriteHtml(writer, columns);

            writer.WriteLine("</table>");
        }

        private static string MakeLinkPath(IDirectory directory, HttpRequest request)
        {
            StringBuilder sb = new StringBuilder();
            ArrayList pathList = new ArrayList();

            for (IDirectory dir = directory; dir != null; dir = dir.Parent)
                pathList.Add(dir.Name);

            pathList.RemoveAt(pathList.Count - 1);

            pathList.Reverse();

            sb.Append("<a href=\"" + request.Uri.Scheme + "://" + request.Uri.Host);
            if (request.Uri.Port != 80)
                sb.Append(":" + request.Uri.Port);
            sb.Append("/\">");
            sb.Append(request.Uri.Host + "</a>");

            if (pathList.Count > 0)
                sb.Append(" - ");

            StringBuilder reassembledPath = new StringBuilder();

            for (int i = 0; i < pathList.Count; i++)
            {
                string path = pathList[i] as string;

                sb.Append("<a href=\"/");

                reassembledPath.Append(UrlEncoding.Encode(path));
                reassembledPath.Append("/");

                sb.Append(reassembledPath.ToString());

                sb.Append("\">");

                sb.Append(path);

                if (i < pathList.Count - 1)
                    sb.Append("</a>/");
                else
                    sb.Append("</a>");
            }

            return sb.ToString();
        }

        private static string GetColumnTd(ResourceColumn column)
        {
            switch (column)
            {
                case ResourceColumn.Created:
                case ResourceColumn.Modified:
                    return "<td nowrap=\"true\">&nbsp;&nbsp;";
                case ResourceColumn.Size:
                    return "<td align=\"right\" nowrap=\"true\">&nbsp;&nbsp;";
                case ResourceColumn.Name:
                    return "<td>&nbsp;&nbsp;";
                default:
                    return "<td>&nbsp;&nbsp;";
            }
        }

        #region Nested type: ResourceComparer

        private class ResourceComparer : IComparer
        {
            private readonly ResourceColumn column;
            private readonly bool reverse;

            public ResourceComparer(bool reverse, ResourceColumn column)
            {
                this.reverse = reverse;
                this.column = column;
            }

            #region IComparer Members

            public int Compare(object x, object y)
            {
                ResourceEntry entryX = x as ResourceEntry;
                ResourceEntry entryY = y as ResourceEntry;

                if (entryX == null || entryY == null)
                    return -1;
                if (entryX.IsDirectory != entryY.IsDirectory)
                {
                    if (entryX.IsDirectory && !entryY.IsDirectory)
                        return -1;
                    else
                        return 1;
                }

                switch (column)
                {
                    case ResourceColumn.Created:
                        return entryX.Created.CompareTo(entryY.Created) * (reverse ? -1 : 1);
                    case ResourceColumn.Modified:
                        return entryX.Modified.CompareTo(entryY.Modified) * (reverse ? -1 : 1);
                    case ResourceColumn.Size:
                        return entryX.Size.CompareTo(entryY.Size) * (reverse ? -1 : 1);
                    case ResourceColumn.Name:
                        return entryX.Name.CompareTo(entryY.Name) * (reverse ? -1 : 1);
                    default:
                        return 0;
                }
            }

            #endregion
        }

        #endregion

        #region Nested type: ResourceEntry

        private class ResourceEntry
        {
            private DateTime created = DateTime.MinValue;
            private readonly bool isDir;
            private DateTime modified = DateTime.MinValue;
            private readonly string path;
            private readonly IResource resource;
            private long size = -1;

            public ResourceEntry(IResource resource)
            {
                this.resource = resource;
                if (resource is IPhysicalResource)
                    path = (resource as IPhysicalResource).Path;
                if (resource is IDirectory)
                    isDir = true;
            }

            public DateTime Created
            {
                get
                {
                    if (path == null)
                        return DateTime.MinValue;

                    try
                    {
                        if (created == DateTime.MinValue)
                            created = File.GetCreationTime(path);
                    }
                    catch (IOException)
                    {
                    }
                    return created;
                }
            }

            public DateTime Modified
            {
                get
                {
                    if (path == null)
                        return DateTime.MinValue;

                    try
                    {
                        if (modified == DateTime.MinValue)
                            modified = File.GetCreationTime(path);
                    }
                    catch (IOException)
                    {
                    }
                    return modified;
                }
            }

            public long Size
            {
                get
                {
                    if (path == null)
                        return -1;

                    if (isDir)
                        return -1;

                    try
                    {
                        if (size == -1)
                            size = new FileInfo(path).Length;
                    }
                    catch (IOException)
                    {
                    }
                    return size;
                }
            }

            public string Name
            {
                get { return resource.Name; }
            }

            public bool IsDirectory
            {
                get { return isDir; }
            }

            public void WriteHtml(TextWriter writer, IEnumerable<ResourceColumn> columns)
            {
                try
                {
                    if (path != null)
                    {
                        if ((File.GetAttributes(path) & FileAttributes.Hidden) != 0)
                            return;
                    }
                }
                catch (IOException)
                {
                }

                writer.WriteLine("<tr>");

                foreach (ResourceColumn column in columns)
                {
                    writer.Write(GetColumnTd(column));
                    switch (column)
                    {
                        case ResourceColumn.Created:
                            if (path != null)
                            {
                                if (Created != DateTime.MinValue)
                                    writer.Write(Created.ToString());
                            }
                            break;
                        case ResourceColumn.Modified:
                            if (path != null)
                            {
                                if (Modified != DateTime.MinValue)
                                    writer.Write(Modified.ToString());
                            }
                            break;
                        case ResourceColumn.Size:
                            if (!isDir)
                            {
                                if (path != null)
                                {
                                    if (Size != -1)
                                        writer.Write(ReadableDataLength.Calculate(Size));
                                }
                            }
                            break;
                        case ResourceColumn.Name:
                            writer.WriteLine("<a href=\"" + UrlEncoding.Encode(resource.Name) +
                                             (isDir ? "/\">[" : "\">") + resource.Name + (isDir ? "]" : "") + "</a>");
                            break;
                    }
                    writer.WriteLine("</td>");
                }

                writer.WriteLine("</tr>");
            }
        }

        #endregion
    }
}