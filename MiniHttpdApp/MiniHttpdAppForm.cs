using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Xml;
using MiniHttpd;
using MiniHttpd.Aspx;
using MiniHttpd.FileSystem;

namespace MiniHttpdApp
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class MiniHttpdAppForm : Form
    {
        private OpenFileDialog addFileDialog;
        private bool autoStart;
        private IContainer components;
        private TreeView fileTreeView;
        private ToolTip fileTreeViewTooltip;
        private FolderBrowserDialog folderAddDialog;
        public bool isFirstUse;
        private TreeNode lastTooltipNode;
        private bool logToFile;
        private StreamWriter logWriter;
        private MainMenu mainMenu;
        private MenuItem menuAbout;
        private MenuItem menuClearLog;
        private MenuItem menuEditUsers;
        private MenuItem menuExit;
        private MenuItem menuItem1;
        private MenuItem menuItem2;
        private MenuItem menuItem3;
        private MenuItem menuItem4;
        private MenuItem menuItem5;
        private MenuItem menuItem6;
        private MenuItem menuItem7;
        private MenuItem menuItem8;
        private MenuItem menuNotifyIconExit;
        private MenuItem menuNotifyIconOpen;
        private MenuItem menuNotifyIconStartServer;
        private MenuItem menuNotifyIconStopServer;
        private MenuItem menuStartServer;
        private MenuItem menuStopServer;
        private MenuItem menuTransferMonitor;
        private bool newLine;
        private NotifyIcon notifyIcon;
        private ContextMenu notifyIconMenu;
        private RichTextBox outputBox;
        private PropertyGrid propertyGrid;
        private string rootPath;
        private DirectoryType rootType;
        private MemoryStream serializedVirtualRoot;
        private HttpWebServer server;
        private Settings settings;
        private Splitter splitter1;
        private Splitter splitter2;
        private TransferMonitorForm transferMonitor;
        private ContextMenu treeContextMenu;

        public MiniHttpdAppForm()
        {
            //
            // Required for Windows Form Designer support
            //

            Application.EnableVisualStyles();

            InitializeComponent();

            InitMenuItems();

            notifyIcon.Icon = Icon;

            server = new AspxWebServer();
            server.Started += server_StateChanged;
            server.Stopped += server_StateChanged;
            ConsoleWriter writer = new ConsoleWriter();
            writer.OnWrite += writer_OnWrite;
            server.Log = writer;

            server_StateChanged(this, null);

            rootPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            settings = new Settings(this);
            propertyGrid.SelectedObject = settings;

            settings.IndexPageStyle = IndexPageStyle.Advanced;

            settings.RootType = DirectoryType.Virtual;

            transferMonitor = new TransferMonitorForm(server);

            LoadSettings();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void OnRootNodeChanged()
        {
            fileTreeView.Nodes.Clear();

            TreeNode root = new TreeNode("Root");
            root.Tag = server.Root;
            root.Nodes.Add(new TreeNode());
            root.Expand();

            fileTreeView.Nodes.Add(root);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.Run(new MiniHttpdAppForm());
        }

        private void MiniHttpdAppForm_Load(object sender, EventArgs e)
        {
            if (settings.AutoStart)
                server.Start();

            MiniHttpdAppForm_Resize(this, null);

            if (fileTreeView.TopNode != null &&
                fileTreeView.TopNode.FirstNode != null &&
                fileTreeView.TopNode.FirstNode.Tag == null &&
                (server.Root == null ||
                 server.Root is VirtualDirectory))
                server.Log.WriteLine("Drag and drop files and folders onto the tree to serve them.");

            if (!server.IsRunning)
                server.Log.WriteLine("Click Server -> Start to start the server.");
        }

        private void LoadSettings()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(LoadSettings));
                return;
            }

            XmlDocument doc;
            XmlElement docElement;
            try
            {
                doc = new XmlDocument();
                doc.Load("MiniHttpdSettings.xml");

                docElement = doc.DocumentElement;
            }
            catch
            {
                isFirstUse = true;
                return;
            }

            settings.RootFolder = docElement["RootFolder"].InnerText;
            settings.RootType = (DirectoryType) Enum.Parse(typeof (DirectoryType), docElement["RootType"].InnerText);
            try
            {
                serializedVirtualRoot = new MemoryStream(Convert.FromBase64String(docElement["VirtualDir"].InnerText));
                if (settings.RootType == DirectoryType.Virtual)
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    serializedVirtualRoot.Seek(0, SeekOrigin.Begin);
                    server.Root = formatter.Deserialize(serializedVirtualRoot) as VirtualDirectory;
                }
            }
            catch (SerializationException err)
            {
                MessageBox.Show(err.Message);
            }

            OnRootNodeChanged();
            settings.HostName = docElement["HostName"].InnerText;

            try
            {
                settings.Port = int.Parse(docElement["Port"].InnerText);
                settings.AutoStart = bool.Parse(docElement["AutoStart"].InnerText);
                settings.LogConnections = bool.Parse(docElement["LogConnections"].InnerText);
                settings.LogRequests = bool.Parse(docElement["LogRequests"].InnerText);
                settings.LogToFile = bool.Parse(docElement["LogToFile"].InnerText);
                settings.AuthenticateClients = bool.Parse(docElement["EnableAuthentication"].InnerText);
            }
            catch (FormatException err)
            {
                MessageBox.Show(err.Message);
            }

            try
            {
                settings.IndexPageStyle =
                    (IndexPageStyle) Enum.Parse(typeof (IndexPageStyle), docElement["IndexPageStyle"].InnerText);
                WindowState =
                    (FormWindowState) Enum.Parse(typeof (FormWindowState), docElement["WindowState"].InnerText);
            }
            catch (ArgumentException err)
            {
                MessageBox.Show(err.Message);
            }


            foreach (XmlElement user in docElement["Users"].ChildNodes)
            {
                ((BasicAuthenticator) server.Authenticator).AddUserByHash(user["Name"].InnerText,
                                                                          user["Password"].InnerText);
            }
        }

        private void SaveSettings()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(SaveSettings));
                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("MiniHttpdSettings"));
            XmlElement element;
            XmlElement docElement = doc.DocumentElement;

            element = doc.CreateElement("VirtualDir");
            docElement.AppendChild(element);

            if (server.Root is VirtualDirectory)
            {
                serializedVirtualRoot = new MemoryStream();
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(serializedVirtualRoot, server.Root);
                element.InnerText = Convert.ToBase64String(serializedVirtualRoot.ToArray());
            }

            else
                element.InnerText = Convert.ToBase64String(serializedVirtualRoot.ToArray());

            element = doc.CreateElement("RootFolder");
            docElement.AppendChild(element);
            element.InnerText = rootPath;

            element = doc.CreateElement("RootType");
            docElement.AppendChild(element);
            element.InnerText = settings.RootType.ToString();

            element = doc.CreateElement("Port");
            docElement.AppendChild(element);
            element.InnerText = settings.Port.ToString();

            element = doc.CreateElement("AutoStart");
            docElement.AppendChild(element);
            element.InnerText = settings.AutoStart.ToString();

            element = doc.CreateElement("LogConnections");
            docElement.AppendChild(element);
            element.InnerText = settings.LogConnections.ToString();

            element = doc.CreateElement("LogRequests");
            docElement.AppendChild(element);
            element.InnerText = settings.LogRequests.ToString();

            element = doc.CreateElement("LogToFile");
            docElement.AppendChild(element);
            element.InnerText = settings.LogToFile.ToString();

            element = doc.CreateElement("HostName");
            docElement.AppendChild(element);
            element.InnerText = settings.HostName;

            element = doc.CreateElement("IndexPageStyle");
            docElement.AppendChild(element);
            element.InnerText = settings.IndexPageStyle.ToString();

            element = doc.CreateElement("WindowState");
            docElement.AppendChild(element);
            element.InnerText = WindowState.ToString();

            element = doc.CreateElement("EnableAuthentication");
            docElement.AppendChild(element);
            element.InnerText = settings.AuthenticateClients.ToString();

            element = doc.CreateElement("Users");
            docElement.AppendChild(element);
            foreach (string username in ((BasicAuthenticator) server.Authenticator).Users)
            {
                XmlElement user = doc.CreateElement("User");
                XmlElement nameElement = doc.CreateElement("Name");
                XmlElement passwordElement = doc.CreateElement("Password");

                nameElement.InnerText = username;
                passwordElement.InnerText = ((BasicAuthenticator) server.Authenticator).GetPasswordHash(username);

                user.AppendChild(nameElement);
                user.AppendChild(passwordElement);
                element.AppendChild(user);
            }

            doc.Save("MiniHttpdSettings.xml");
        }

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hwnd, Int32 msg, Int32 wParam, Int32 lParam);

        private void writer_OnWrite(char[] buffer, int index, int count)
        {
            if (InvokeRequired)
            {
                Invoke(new OnWriteDelegate(writer_OnWrite), new object[] {buffer, index, count});
                return;
            }

            if (outputBox.IsDisposed)
                return;

            if (settings.LogToFile)
            {
                if (logWriter == null)
                {
                    try
                    {
                        FileStream stream =
                            new FileStream("minihttpd.log", FileMode.Append, FileAccess.Write, FileShare.Read, 128);
                        logWriter = new StreamWriter(stream);
                        logWriter.AutoFlush = true;

                        logWriter.WriteLine("-------------------------------------------------------");
                        logWriter.WriteLine("* Beginning log at " + DateTime.Now);
                        logWriter.WriteLine("-------------------------------------------------------");
                    }
                    catch (IOException err)
                    {
                        MessageBox.Show(err.Message);
                    }
                    catch (UnauthorizedAccessException err)
                    {
                        MessageBox.Show(err.Message);
                    }
                }

                logWriter.Write(buffer, index, count);
            }

            string text = new string(buffer, index, count);

            if (text.Length == 0)
                return;

            bool oldNewLine = newLine;

            newLine = false;

            if (text.EndsWith("\r\n"))
            {
                text = text.Substring(0, text.Length - 2);
                newLine = true;
            }

            else if (text.EndsWith("\n"))
            {
                text = text.Substring(0, text.Length - 1);
                newLine = true;
            }

            outputBox.AppendText((oldNewLine ? "\r\n" : "") + text);

            // Scroll to bottom
            SendMessage(outputBox.Handle, 0x0115, 7, 0);
        }

        private void server_StateChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(server_StateChanged), new object[] {sender, e});
                return;
            }

            menuStartServer.Enabled = menuNotifyIconStartServer.Enabled = !server.IsRunning;
            menuStopServer.Enabled = menuNotifyIconStopServer.Enabled = server.IsRunning;

            if (server.IsRunning)
                notifyIcon.Text = "MiniHttpd - running on port " + server.Port;
            else
                notifyIcon.Text = "MiniHttpd";
        }

        private void MiniHttpdAppForm_Closing(object sender, CancelEventArgs e)
        {
            if (server != null)
                server.Stop();

            SaveSettings();
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void menuStartServer_Click(object sender, EventArgs e)
        {
            server.Start();
        }

        private void menuStopServer_Click(object sender, EventArgs e)
        {
            server.Stop();
        }

        private void fileTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            e.Node.Nodes.Clear();

            IDirectory dir = e.Node.Tag as IDirectory;

            if (dir == null)
                return;

            TreeNode[] nodes;

            ICollection subItems;
            try
            {
                subItems = dir.GetDirectories();
            }
            catch (IOException ex)
            {
                server.Log.WriteLine(ex.Message);
                return;
            }
            nodes = new TreeNode[subItems.Count];
            int i = 0;
            foreach (IDirectory subDir in subItems)
            {
                TreeNode node = new TreeNode(subDir.Name);
                node.Nodes.Add(new TreeNode());
                node.Tag = subDir;
                nodes[i++] = node;
            }
            e.Node.Nodes.AddRange(nodes);

            subItems = dir.GetFiles();
            nodes = new TreeNode[subItems.Count];
            i = 0;
            foreach (IFile file in subItems)
            {
                TreeNode node = new TreeNode(file.Name);
                node.Tag = file;
                nodes[i++] = node;
            }
            e.Node.Nodes.AddRange(nodes);

            if (e.Node.Nodes.Count == 0)
            {
                TreeNode empty = new TreeNode("Empty");
                empty.ForeColor = SystemColors.GrayText;
                e.Node.Nodes.Add(empty);
            }
        }

        private void outputBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                Process.Start(e.LinkText);
            }
            catch (Win32Exception)
            {
            }
        }

        private void MiniHttpdAppForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                transferMonitor.Hide();
                Hide();
                notifyIcon.Visible = true;
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Activate();

            notifyIcon.Visible = false;
        }

        private void fileTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            TreeNode node = fileTreeView.GetNodeAt(e.X, e.Y);
            fileTreeView.SelectedNode = node;
        }

        private void fileTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    fileTreeView_DoubleClick(this, null);
                    break;
                case Keys.Delete:
                    treeMenuRemove_Click(this, null);
                    break;
            }
        }

        private void fileTreeView_DoubleClick(object sender, EventArgs e)
        {
            if (GetTreeMenuCopyUrl() != null)
                Process.Start(server.GetUrl(fileTreeView.SelectedNode.Tag as IResource).ToString());
        }

        private void menuTransferMonitor_Click(object sender, EventArgs e)
        {
            transferMonitor.Show();
        }

        private void menuNotifyIconExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void menuAbout_Click(object sender, EventArgs e)
        {
            AboutForm form = new AboutForm();
            form.ShowDialog(this);
        }

        private void fileTreeView_DragDrop(object sender, DragEventArgs e)
        {
            fileTreeView.HideSelection = true;

            TreeNode selectedNode = fileTreeView.SelectedNode;
            TreeNode node = fileTreeView.GetNodeAt(fileTreeView.PointToClient(new Point(e.X, e.Y)));

            if (!e.Data.GetDataPresent("FileDrop"))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            fileTreeView.SelectedNode = node;

            node = GetTreeMenuAddFileNode();

            if (node == null)
            {
                e.Effect = DragDropEffects.None;
                fileTreeView.SelectedNode = selectedNode;
                return;
            }

            string[] files = e.Data.GetData("FileDrop") as string[];
            if (files == null || files.Length == 0)
                return;

            VirtualDirectory dir = node.Tag as VirtualDirectory;
            if (dir == null)
                return;

            string lastNode = null;

            foreach (string path in files)
            {
                try
                {
                    if (Directory.Exists(path))
                        lastNode = dir.AddDirectory(path).Name;
                    else if (File.Exists(path))
                        lastNode = dir.AddFile(path).Name;
                }
                catch (Exception ex)
                {
                    server.Log.WriteLine("Error: " + ex.Message);
                    continue;
                }
            }

            RefreshNode(node, lastNode);
        }

        private void fileTreeView_DragOver(object sender, DragEventArgs e)
        {
            TreeNode selectedNode = fileTreeView.SelectedNode;
            TreeNode node = fileTreeView.GetNodeAt(fileTreeView.PointToClient(new Point(e.X, e.Y)));

            if (!e.Data.GetDataPresent("FileDrop"))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            fileTreeView.HideSelection = false;

            fileTreeView.SelectedNode = node;

            node = GetTreeMenuAddFileNode();

            if (node == null)
            {
                e.Effect = DragDropEffects.None;
                fileTreeView.SelectedNode = selectedNode;
                return;
            }

            e.Effect = DragDropEffects.Copy;
        }

        private void fileTreeView_DragLeave(object sender, EventArgs e)
        {
            fileTreeView.HideSelection = true;
        }

        private void MiniHttpdAppForm_Deactivate(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void menuNotifyIconOpen_Click(object sender, EventArgs e)
        {
            notifyIcon_DoubleClick(this, null);
        }

        private void menuClearLog_Click(object sender, EventArgs e)
        {
            outputBox.Clear();
        }

        private void fileTreeView_MouseMove(object sender, MouseEventArgs e)
        {
            TreeNode node =
                fileTreeView.GetNodeAt(fileTreeView.PointToClient(new Point(MousePosition.X, MousePosition.Y)));

            if (node == null)
                return;

            if (lastTooltipNode == node)
                return;

            string text = "";
            if (node.Tag != null)
            {
                if (node.Tag is IPhysicalResource)
                    text = "Location: " + ((IPhysicalResource) node.Tag).Path;
                else if (node.Tag is RedirectFile)
                    text = "Location: " + ((RedirectFile) node.Tag).Redirect;
            }
            fileTreeViewTooltip.SetToolTip(fileTreeView, text);
            lastTooltipNode = node;
        }

        private void menuEditUsers_Click(object sender, EventArgs e)
        {
            EditUsersForm form = new EditUsersForm(server.Authenticator as BasicAuthenticator);
            form.ShowDialog(this);
            settings.AuthenticateClients = form.EnableAuthentication;
            propertyGrid.Refresh();
        }

        #region Settings

        private class Settings
        {
            private MiniHttpdAppForm form;

            public Settings(MiniHttpdAppForm form)
            {
                this.form = form;
            }

            [Category("Directories")]
            [Description("The root folder to serve when RootType is set to Drive.")]
            [Editor(typeof (FolderSelector), typeof (UITypeEditor))]
            public string RootFolder
            {
                get { return form.rootPath; }
                set
                {
                    form.server.Root = new AspxAppDirectory(value);
                    form.OnRootNodeChanged();

                    RootType = DirectoryType.Drive;

                    if (value == null)
                        return;

                    form.rootPath = value;

                    form.propertyGrid.Refresh();
                }
            }

            [Category("Directories"),
             Description("The type of the root folder, either a root folder or a drive on disk.")]
            public DirectoryType RootType
            {
                get { return form.rootType; }
                set
                {
                    form.rootType = value;

                    if (value == DirectoryType.Drive)
                    {
                        if (form.server.Root is VirtualDirectory)
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            if (form.serializedVirtualRoot != null)
                                form.serializedVirtualRoot.Close();
                            form.serializedVirtualRoot = new MemoryStream();
                            formatter.Serialize(form.serializedVirtualRoot, form.server.Root);
                        }
                        form.server.Root = new AspxAppDirectory(form.rootPath);
                        form.OnRootNodeChanged();
                    }
                    else
                    {
                        if (form.serializedVirtualRoot != null)
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            form.serializedVirtualRoot.Seek(0, SeekOrigin.Begin);
                            form.server.Root = formatter.Deserialize(form.serializedVirtualRoot) as VirtualDirectory;
                        }
                        else
                            form.server.Root = new VirtualDirectory();
                    }
                    form.OnRootNodeChanged();
                }
            }

            [Category("Directories"), Description("The style of the index page.")]
            public IndexPageStyle IndexPageStyle
            {
                get
                {
                    if (form.server.IndexPage is IndexPageEx)
                        return IndexPageStyle.Advanced;

                    return IndexPageStyle.Basic;
                }
                set
                {
                    if (value == IndexPageStyle.Basic)
                        form.server.IndexPage = new IndexPage();
                    else
                        form.server.IndexPage = new IndexPageEx();
                }
            }

            [Category("Connection"), Description("The server's host address to be used when copying URLs.")]
            public string HostName
            {
                get { return form.server.HostName; }
                set { form.server.HostName = value; }
            }

            [Category("Connection"), Description("The port on which the server is to run; default is 80.")]
            public int Port
            {
                get { return form.server.Port; }
                set { form.server.Port = value; }
            }

            [Category("Connection"), Description("Start serving when the application is started.")]
            public bool AutoStart
            {
                get { return form.autoStart; }
                set { form.autoStart = value; }
            }

            [Category("Logging"), Description("Make note in the log when a client connects to the server.")]
            public bool LogConnections
            {
                get { return form.server.LogConnections; }
                set { form.server.LogConnections = value; }
            }

            [Category("Logging"), Description("Make note in the log when a client requests a resource.")]
            public bool LogRequests
            {
                get { return form.server.LogRequests; }
                set { form.server.LogRequests = value; }
            }

            [Category("Logging"), Description("Write the log output to MiniHttpd.log.")]
            public bool LogToFile
            {
                get { return form.logToFile; }
                set { form.logToFile = value; }
            }

            [Category("Security"), Description("Ask for authentication when a client connects to the server.")]
            public bool AuthenticateClients
            {
                get { return form.server.RequireAuthentication; }
                set { form.server.RequireAuthentication = value; }
            }

            #region Nested type: FolderSelector

            private class FolderSelector : UITypeEditor
            {
                public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
                {
                    Settings settings = context.Instance as Settings;

                    if (settings == null)
                        return null;

                    settings.form.folderAddDialog.SelectedPath = value as string;

                    if (settings.form.folderAddDialog.ShowDialog(settings.form) != DialogResult.OK)
                        return value;

                    return settings.form.folderAddDialog.SelectedPath;
                }

                public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
                {
                    return UITypeEditorEditStyle.Modal;
                }
            }

            #endregion
        }

        #endregion

        #region File Tree Menus

        private MenuItem treeMenuAddDirectory;
        private MenuItem treeMenuAddFile;
        private MenuItem treeMenuCopyUrl;
        private MenuItem treeMenuCreateRedirect;
        private MenuItem treeMenuCreateVirtualDirectory;
        private MenuItem treeMenuRemove;

        private void InitMenuItems()
        {
            treeMenuRemove = new MenuItem("&Remove");
            treeMenuAddFile = new MenuItem("Add &File");
            treeMenuAddDirectory = new MenuItem("Add &Directory");
            treeMenuCreateVirtualDirectory = new MenuItem("Create &Virtual Directory");
            treeMenuCreateRedirect = new MenuItem("Create Redirec&t");
            treeMenuCopyUrl = new MenuItem("Copy &URL");

            treeMenuRemove.Click += treeMenuRemove_Click;
            treeMenuAddFile.Click += treeMenuAddFile_Click;
            treeMenuAddDirectory.Click += treeMenuAddDirectory_Click;
            treeMenuCreateVirtualDirectory.Click += treeMenuCreateVirtualDirectory_Click;
            treeMenuCreateRedirect.Click += treeMenuCreateRedirect_Click;
            treeMenuCopyUrl.Click += treeMenuCopyUrl_Click;
        }

        private void treeContextMenu_Popup(object sender, EventArgs e)
        {
            if (fileTreeView.SelectedNode == null)
                return;

            treeContextMenu.MenuItems.Clear();

            int category = 0;

            if (GetTreeMenuCopyUrl() != null)
            {
                treeContextMenu.MenuItems.Add(treeMenuCopyUrl);
                category = -1;
            }
            if (GetTreeMenuAddFileNode() != null)
            {
                if (category != 0)
                {
                    category = 0;
                    treeContextMenu.MenuItems.Add(new MenuItem("-"));
                }
                treeContextMenu.MenuItems.Add(treeMenuAddFile);
            }
            if (GetTreeMenuAddDirectoryNode() != null)
                treeContextMenu.MenuItems.Add(treeMenuAddDirectory);
            if (GetTreeMenuCreateVirtualDirectoryNode() != null)
            {
                if (category != 1)
                {
                    category = 1;
                    treeContextMenu.MenuItems.Add(new MenuItem("-"));
                }
                treeContextMenu.MenuItems.Add(treeMenuCreateVirtualDirectory);
            }
            if (GetTreeMenuCreateRedirect() != null)
            {
                if (category != 1)
                {
                    category = 1;
                    treeContextMenu.MenuItems.Add(new MenuItem("-"));
                }
                treeContextMenu.MenuItems.Add(treeMenuCreateRedirect);
            }
            if (GetTreeMenuRemoveNode() != null)
            {
                if (category != 2)
                {
                    treeContextMenu.MenuItems.Add(new MenuItem("-"));
                }
                treeContextMenu.MenuItems.Add(treeMenuRemove);
            }
        }

        private TreeNode GetTreeMenuCopyUrl()
        {
            if (fileTreeView.SelectedNode != null
                && fileTreeView.SelectedNode.Tag != null
                && fileTreeView.SelectedNode.Tag is IResource)
                return fileTreeView.SelectedNode;
            else
                return null;
        }

        private TreeNode GetTreeMenuRemoveNode()
        {
            TreeNode node = fileTreeView.SelectedNode;
            if (node == null)
                return null;
            if (node.Tag == null)
                return null;

            if (node.Parent == null || !(node.Parent.Tag is VirtualDirectory))
                return null;

            return node;
        }

        private TreeNode GetTreeMenuAddFileNode()
        {
            TreeNode node = fileTreeView.SelectedNode;
            if (node == null)
                return null;
            if (node.Tag != null && node.Tag is DriveDirectory)
                return null;
            if (node.Tag == null || !(node.Tag is VirtualDirectory))
            {
                if (node.Parent != null && node.Parent.Tag != null && node.Parent.Tag is VirtualDirectory)
                    node = node.Parent;
                else
                    return null;
            }
            return node;
        }

        private TreeNode GetTreeMenuAddDirectoryNode()
        {
            return GetTreeMenuAddFileNode();
        }

        private TreeNode GetTreeMenuCreateVirtualDirectoryNode()
        {
            TreeNode node = fileTreeView.SelectedNode;
            if (node == null)
                return null;
            if (node.Tag != null && node.Tag is DriveDirectory)
                return null;

            if (node.Tag is VirtualDirectory)
                return node;

            if (node.Parent != null
                && node.Parent.Tag != null
                && node.Parent.Tag is VirtualDirectory)
                return node.Parent;

            return null;
        }

        private TreeNode GetTreeMenuCreateRedirect()
        {
            return GetTreeMenuCreateVirtualDirectoryNode();
        }

        private void treeMenuRemove_Click(object sender, EventArgs e)
        {
            TreeNode node = GetTreeMenuRemoveNode();
            if (node == null)
                return;

            ((VirtualDirectory) node.Parent.Tag).Remove(((IResource) node.Tag).Name);

            RefreshParentOfNode(node);
        }

        private void AddFile(TreeNode node, ICollection files)
        {
            //todo: Lastfile is not used. Do we need it?
            IFile lastFile;
            try
            {
                foreach (string path in addFileDialog.FileNames)
                {
                    try
                    {
                        lastFile = ((VirtualDirectory) node.Tag).AddFile(path);
                    }
                    catch (DirectoryException ex)
                    {
                        server.Log.WriteLine("Add File: " + ex.Message);
                        return;
                    }
                }
            }
            finally
            {
                RefreshNode(node, null);
            }
        }

        private void treeMenuAddFile_Click(object sender, EventArgs e)
        {
            TreeNode node = GetTreeMenuAddFileNode();
            if (node == null)
                return;

            if (addFileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (addFileDialog.FileNames.Length == 0)
                return;

            AddFile(node, addFileDialog.FileNames);
        }

        private void treeMenuAddDirectory_Click(object sender, EventArgs e)
        {
            TreeNode node = GetTreeMenuAddDirectoryNode();
            if (node == null)
                return;

            if (folderAddDialog.ShowDialog(this) != DialogResult.OK)
                return;

            IDirectory dir;
            try
            {
                dir = ((VirtualDirectory) node.Tag).AddDirectory(folderAddDialog.SelectedPath);
            }
            catch (Exception ex)
            {
                server.Log.WriteLine("Add Directory: " + ex.Message);
                return;
            }

            RefreshNode(node, dir.Name);
        }

        private void treeMenuCreateVirtualDirectory_Click(object sender, EventArgs e)
        {
            TreeNode node = GetTreeMenuCreateVirtualDirectoryNode();
            if (node == null)
                return;

            TextBoxForm form = new TextBoxForm();
            form.Text = "Create Virtual Directory";
            form.Caption = "Enter the name of the new directory to create:";
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                ((VirtualDirectory) node.Tag).AddDirectory(
                    new VirtualDirectory(form.TextBox.Text, node.Tag as VirtualDirectory));
            }
            catch (Exception ex)
            {
                server.Log.WriteLine("Create Virtual Directory: " + ex.Message);
                return;
            }

            RefreshNode(node, form.TextBox.Text);
        }

        private void treeMenuCreateRedirect_Click(object sender, EventArgs e)
        {
            TreeNode node = GetTreeMenuCreateRedirect();
            if (node == null)
                return;

            NewRedirectForm form = new NewRedirectForm();
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                ((VirtualDirectory) node.Tag).AddFile(
                    new RedirectFile(form.RedirectName, form.RedirectTarget, node.Tag as VirtualDirectory));
            }
            catch (Exception ex)
            {
                server.Log.WriteLine("Create Redirect: " + ex.Message);
                return;
            }

            RefreshNode(node, form.RedirectName);
        }

        private void treeMenuCopyUrl_Click(object sender, EventArgs e)
        {
            if (fileTreeView.SelectedNode != null
                && fileTreeView.SelectedNode.Tag != null
                && fileTreeView.SelectedNode.Tag is IResource)
            {
                Clipboard.SetDataObject(
                    server.GetUrl(fileTreeView.SelectedNode.Tag as IResource).ToString().Replace(" ", "%20"));
            }
        }

        private void RefreshNode(TreeNode node, string selectNodeName)
        {
            if (node.Nodes.Count == 0)
            {
                RefreshParentOfNode(fileTreeView.SelectedNode);
                if (selectNodeName != null)
                {
                    fileTreeView.SelectedNode = FindNodeWithText(fileTreeView.SelectedNode, selectNodeName);
                    if (fileTreeView.SelectedNode.Nodes.Count != 0)
                        fileTreeView.SelectedNode.Expand();
                }
            }
            else
            {
                node.Collapse();
                node.Expand();
                if (selectNodeName != null)
                {
                    fileTreeView.SelectedNode = FindNodeWithText(node, selectNodeName);
                    if (fileTreeView.SelectedNode.Nodes.Count != 0)
                        fileTreeView.SelectedNode.Expand();
                }
            }
        }

        private TreeNode FindNodeWithText(TreeNode node, string text)
        {
            if (node == null)
                return null;

            for (int i = 0; i < node.Nodes.Count; i++)
            {
                if (string.Equals(node.Nodes[i].Text, text))
                    return node.Nodes[i];
            }

            return null;
        }

        private void RefreshParentOfNode(TreeNode node)
        {
            int index = node.Index;
            TreeNode parentNode = node.Parent;

            parentNode.Collapse();
            parentNode.Expand();

            if (index < parentNode.Nodes.Count)
                fileTreeView.SelectedNode = parentNode.Nodes[index];
            else
                fileTreeView.SelectedNode = parentNode.Nodes[parentNode.Nodes.Count - 1];
        }

        #endregion

        #region Nested type: DirectoryType

        private enum DirectoryType
        {
            Drive,
            Virtual
        }

        #endregion

        #region Nested type: IndexPageStyle

        private enum IndexPageStyle
        {
            Basic,
            Advanced
        }

        #endregion

        #region Nested type: OnWriteDelegate

        private delegate void OnWriteDelegate(char[] buffer, int index, int count);

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.mainMenu = new System.Windows.Forms.MainMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuExit = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuStartServer = new System.Windows.Forms.MenuItem();
            this.menuStopServer = new System.Windows.Forms.MenuItem();
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.menuTransferMonitor = new System.Windows.Forms.MenuItem();
            this.menuItem6 = new System.Windows.Forms.MenuItem();
            this.menuClearLog = new System.Windows.Forms.MenuItem();
            this.menuItem5 = new System.Windows.Forms.MenuItem();
            this.menuAbout = new System.Windows.Forms.MenuItem();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.outputBox = new System.Windows.Forms.RichTextBox();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.fileTreeView = new System.Windows.Forms.TreeView();
            this.treeContextMenu = new System.Windows.Forms.ContextMenu();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.notifyIconMenu = new System.Windows.Forms.ContextMenu();
            this.menuNotifyIconStartServer = new System.Windows.Forms.MenuItem();
            this.menuNotifyIconStopServer = new System.Windows.Forms.MenuItem();
            this.menuItem7 = new System.Windows.Forms.MenuItem();
            this.menuNotifyIconOpen = new System.Windows.Forms.MenuItem();
            this.menuNotifyIconExit = new System.Windows.Forms.MenuItem();
            this.addFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.folderAddDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.fileTreeViewTooltip = new System.Windows.Forms.ToolTip(this.components);
            this.menuItem8 = new System.Windows.Forms.MenuItem();
            this.menuEditUsers = new System.Windows.Forms.MenuItem();
            this.SuspendLayout();
            // 
            // propertyGrid
            // 
            this.propertyGrid.CommandsVisibleIfAvailable = true;
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Left;
            this.propertyGrid.LargeButtons = false;
            this.propertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.propertyGrid.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(136, 387);
            this.propertyGrid.TabIndex = 0;
            this.propertyGrid.Text = "propertyGrid1";
            this.propertyGrid.ViewBackColor = System.Drawing.SystemColors.Window;
            this.propertyGrid.ViewForeColor = System.Drawing.SystemColors.WindowText;
            // 
            // mainMenu
            // 
            this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[]
                                                 {
                                                     this.menuItem1,
                                                     this.menuItem2,
                                                     this.menuItem4,
                                                     this.menuItem5
                                                 });
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 0;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[]
                                                  {
                                                      this.menuExit
                                                  });
            this.menuItem1.Text = "&File";
            // 
            // menuExit
            // 
            this.menuExit.Index = 0;
            this.menuExit.Text = "E&xit";
            this.menuExit.Click += new System.EventHandler(this.menuExit_Click);
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 1;
            this.menuItem2.MenuItems.AddRange(new System.Windows.Forms.MenuItem[]
                                                  {
                                                      this.menuStartServer,
                                                      this.menuStopServer,
                                                      this.menuItem8,
                                                      this.menuEditUsers
                                                  });
            this.menuItem2.Text = "&Server";
            // 
            // menuStartServer
            // 
            this.menuStartServer.Index = 0;
            this.menuStartServer.Text = "&Start Server";
            this.menuStartServer.Click += new System.EventHandler(this.menuStartServer_Click);
            // 
            // menuStopServer
            // 
            this.menuStopServer.Index = 1;
            this.menuStopServer.Text = "S&top Server";
            this.menuStopServer.Click += new System.EventHandler(this.menuStopServer_Click);
            // 
            // menuItem4
            // 
            this.menuItem4.Index = 2;
            this.menuItem4.MenuItems.AddRange(new System.Windows.Forms.MenuItem[]
                                                  {
                                                      this.menuTransferMonitor,
                                                      this.menuItem6,
                                                      this.menuClearLog
                                                  });
            this.menuItem4.Text = "&View";
            // 
            // menuTransferMonitor
            // 
            this.menuTransferMonitor.Index = 0;
            this.menuTransferMonitor.Text = "Transfer &Monitor";
            this.menuTransferMonitor.Click += new System.EventHandler(this.menuTransferMonitor_Click);
            // 
            // menuItem6
            // 
            this.menuItem6.Index = 1;
            this.menuItem6.Text = "-";
            // 
            // menuClearLog
            // 
            this.menuClearLog.Index = 2;
            this.menuClearLog.Text = "Clear &Log";
            this.menuClearLog.Click += new System.EventHandler(this.menuClearLog_Click);
            // 
            // menuItem5
            // 
            this.menuItem5.Index = 3;
            this.menuItem5.MenuItems.AddRange(new System.Windows.Forms.MenuItem[]
                                                  {
                                                      this.menuAbout
                                                  });
            this.menuItem5.Text = "&Help";
            // 
            // menuAbout
            // 
            this.menuAbout.Index = 0;
            this.menuAbout.Text = "&About";
            this.menuAbout.Click += new System.EventHandler(this.menuAbout_Click);
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(136, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 387);
            this.splitter1.TabIndex = 2;
            this.splitter1.TabStop = false;
            // 
            // outputBox
            // 
            this.outputBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.outputBox.Location = new System.Drawing.Point(139, 291);
            this.outputBox.Name = "outputBox";
            this.outputBox.Size = new System.Drawing.Size(453, 96);
            this.outputBox.TabIndex = 5;
            this.outputBox.Text = "";
            this.outputBox.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.outputBox_LinkClicked);
            // 
            // splitter2
            // 
            this.splitter2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter2.Location = new System.Drawing.Point(139, 288);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(453, 3);
            this.splitter2.TabIndex = 6;
            this.splitter2.TabStop = false;
            // 
            // fileTreeView
            // 
            this.fileTreeView.AllowDrop = true;
            this.fileTreeView.ContextMenu = this.treeContextMenu;
            this.fileTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileTreeView.FullRowSelect = true;
            this.fileTreeView.ImageIndex = -1;
            this.fileTreeView.Location = new System.Drawing.Point(139, 0);
            this.fileTreeView.Name = "fileTreeView";
            this.fileTreeView.PathSeparator = "/";
            this.fileTreeView.SelectedImageIndex = -1;
            this.fileTreeView.Size = new System.Drawing.Size(453, 288);
            this.fileTreeView.Sorted = true;
            this.fileTreeView.TabIndex = 7;
            this.fileTreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.fileTreeView_KeyDown);
            this.fileTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.fileTreeView_MouseDown);
            this.fileTreeView.DragOver += new System.Windows.Forms.DragEventHandler(this.fileTreeView_DragOver);
            this.fileTreeView.DoubleClick += new System.EventHandler(this.fileTreeView_DoubleClick);
            this.fileTreeView.BeforeExpand +=
                new System.Windows.Forms.TreeViewCancelEventHandler(this.fileTreeView_BeforeExpand);
            this.fileTreeView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.fileTreeView_MouseMove);
            this.fileTreeView.DragLeave += new System.EventHandler(this.fileTreeView_DragLeave);
            this.fileTreeView.DragDrop += new System.Windows.Forms.DragEventHandler(this.fileTreeView_DragDrop);
            // 
            // treeContextMenu
            // 
            this.treeContextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[]
                                                        {
                                                            this.menuItem3
                                                        });
            this.treeContextMenu.Popup += new System.EventHandler(this.treeContextMenu_Popup);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 0;
            this.menuItem3.Text = "Test";
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenu = this.notifyIconMenu;
            this.notifyIcon.Text = "MiniHttpd";
            this.notifyIcon.Visible = true;
            this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);
            // 
            // notifyIconMenu
            // 
            this.notifyIconMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[]
                                                       {
                                                           this.menuNotifyIconStartServer,
                                                           this.menuNotifyIconStopServer,
                                                           this.menuItem7,
                                                           this.menuNotifyIconOpen,
                                                           this.menuNotifyIconExit
                                                       });
            // 
            // menuNotifyIconStartServer
            // 
            this.menuNotifyIconStartServer.Index = 0;
            this.menuNotifyIconStartServer.Text = "&Start Server";
            this.menuNotifyIconStartServer.Click += new System.EventHandler(this.menuStartServer_Click);
            // 
            // menuNotifyIconStopServer
            // 
            this.menuNotifyIconStopServer.Index = 1;
            this.menuNotifyIconStopServer.Text = "Sto&p Server";
            // 
            // menuItem7
            // 
            this.menuItem7.Index = 2;
            this.menuItem7.Text = "-";
            // 
            // menuNotifyIconOpen
            // 
            this.menuNotifyIconOpen.DefaultItem = true;
            this.menuNotifyIconOpen.Index = 3;
            this.menuNotifyIconOpen.Text = "&Open MiniHttpd";
            this.menuNotifyIconOpen.Click += new System.EventHandler(this.menuNotifyIconOpen_Click);
            // 
            // menuNotifyIconExit
            // 
            this.menuNotifyIconExit.Index = 4;
            this.menuNotifyIconExit.Text = "E&xit";
            this.menuNotifyIconExit.Click += new System.EventHandler(this.menuNotifyIconExit_Click);
            // 
            // addFileDialog
            // 
            this.addFileDialog.Filter = "All Files|*.*";
            this.addFileDialog.Multiselect = true;
            this.addFileDialog.RestoreDirectory = true;
            // 
            // folderAddDialog
            // 
            this.folderAddDialog.Description = "Add Folder";
            // 
            // fileTreeViewTooltip
            // 
            this.fileTreeViewTooltip.AutomaticDelay = 0;
            this.fileTreeViewTooltip.AutoPopDelay = 5000;
            this.fileTreeViewTooltip.InitialDelay = 0;
            this.fileTreeViewTooltip.ReshowDelay = 0;
            // 
            // menuItem8
            // 
            this.menuItem8.Index = 2;
            this.menuItem8.Text = "-";
            // 
            // menuEditUsers
            // 
            this.menuEditUsers.Index = 3;
            this.menuEditUsers.Text = "Edit &Users";
            this.menuEditUsers.Click += new System.EventHandler(this.menuEditUsers_Click);
            // 
            // MiniHttpdAppForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(592, 387);
            this.Controls.Add(this.fileTreeView);
            this.Controls.Add(this.splitter2);
            this.Controls.Add(this.outputBox);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.propertyGrid);
            this.Menu = this.mainMenu;
            this.Name = "MiniHttpdAppForm";
            this.Text = "MiniHttpd";
            this.Resize += new System.EventHandler(this.MiniHttpdAppForm_Resize);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.MiniHttpdAppForm_Closing);
            this.Load += new System.EventHandler(this.MiniHttpdAppForm_Load);
            this.Deactivate += new System.EventHandler(this.MiniHttpdAppForm_Deactivate);
            this.ResumeLayout(false);
        }

        #endregion

        #endregion
    }
}