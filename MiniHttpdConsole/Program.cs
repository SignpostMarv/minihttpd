using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;
using MiniHttpd;

namespace MiniHttpdConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			new Program(args);
		}

		HttpWebServer server = new HttpWebServer();

		MenuView menu = new MenuView();
		StatusView status = new StatusView();

		bool autoStart = true;

		bool logToScreen = true;
		StreamWriter logWriter;

		bool silent = false;

		string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		MemoryStream serializedVirtualRoot;

		Program(string[] args)
		{

			ConsoleWriter writer = new ConsoleWriter();
			server.Log = writer;
			writer.OnWrite += new ConsoleWriter.WriteEventHandler(writer_OnWrite);

			ToggleLogWriter(true);

			LoadSettings();

			ParseArguments(args);

			if (silent)
			{
				Console.SetOut(System.IO.TextWriter.Null);
				Console.SetError(System.IO.TextWriter.Null);
				try
				{
					server.Start();
					while(server.IsRunning)
						System.Threading.Thread.Sleep(1000);
					return;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e);
					return;
				}
				finally
				{
				}
			}

			if (autoStart)
			{
				try
				{
					server.Start();
					Console.WriteLine();
				}
				catch(Exception e)
				{
					Console.Error.WriteLine(e);
				}
			}

			InitStatusView();
			InitMainMenu();

			menu.StatusView = status;

			menu.DoMenuModal();

			server.Dispose();
		}

		void writer_OnWrite(char[] buffer, int index, int count)
		{
			string text = new string(buffer, index, count);

			if (logToScreen)
				Console.Write(text);

			if (logWriter != null)
				logWriter.Write(text);
		}

		private void InitStatusView()
		{
			status.Title = "MiniHttpd";

			status.AddItem(new StatusItem(null, delegate { return server.IsRunning ? "Running" : "Stopped"; }));
			status.AddItem(new StatusItem("Server URL", delegate { return server.ServerUri; }));
			status.AddItem(new StatusItem("Root type", delegate { return server.Root.GetType().Name; }));
			status.AddItem(new StatusItem("RootDir", "Root directory", delegate { return server.Root is DriveDirectory ? (server.Root as DriveDirectory).Path : null; }));
			status["RootDir"].Showing += delegate { status["RootDir"].Enabled = server.Root is DriveDirectory; };
			status.AddItem(new StatusItem("Auto start", delegate { return autoStart; }));
			status.AddItem(new StatusItem("Index page", delegate { return server.IndexPage.GetType().Name; }));
			status.AddItem(new StatusItem("Log to screen", delegate { return logToScreen; }));
			status.AddItem(new StatusItem("Log to file", delegate { return logWriter != null; }));
			status.AddItem(new StatusItem("Log connections", delegate { return server.LogConnections; }));
			status.AddItem(new StatusItem("Log requests", delegate { return server.LogRequests; }));
		}

		private void InitMainMenu()
		{
			menu.Title = "Main Menu";

			menu.AddItem(new MenuItem(
				"u", "Show server status",
				delegate { status.ShowValues(); }));

			menu.AddItem(new MenuItem(
				"a", "Toggle auto start server on load",
				delegate { autoStart = !autoStart; },
				delegate { return autoStart; }));

			menu.AddItem(new MenuItem(
				"s", "Start server",
				delegate
				{
					if (server.IsRunning)
						server.Stop();
					else
						server.Start();
				}));
			menu["s"].Showing += delegate
			{
				if (server.IsRunning)
					menu["s"].Description = "Stop server";
				else
					menu["s"].Description = "Start server";
			};

			menu.AddItem(new MenuItem(
				"b", "Browse file tree",
				delegate { BrowseTree(); }));

			menu.AddItem(new MenuItem(
				"r", "Toggle root folder type",
				delegate { ToogleRootFolderType(); },
				delegate { return server.Root.GetType().Name; }));

			menu.AddItem(new MenuItem(
				"t", "Set root path",
				new EventHandler<MenuItemSelectedEventArgs>(SetRootPath),
				delegate { return server.Root is IPhysicalResource ? (server.Root as IPhysicalResource).Path : "None"; }));

			menu.AddItem(new MenuItem(
				"i", "Toggle index page style",
				delegate
				{
					if (server.IndexPage is IndexPageEx)
						server.IndexPage = new IndexPage();
					else
						server.IndexPage = new IndexPageEx();
				},
				delegate { return server.IndexPage.GetType().Name; }));

			menu.AddItem(new MenuItem(
				"n", "Set host name",
				new EventHandler<MenuItemSelectedEventArgs>(SetHostName),
				delegate { return server.HostName; }));

			menu.AddItem(new MenuItem(
				"p", "Set port",
				new EventHandler<MenuItemSelectedEventArgs>(SelectPort),
				delegate { return server.Port; }));

			menu.AddItem(new MenuItem(
				"h", "Help",
				delegate { ShowHelp(); }));

			menu.AddItem(new MenuItem(
				"ls", "Log to screen",
				delegate { logToScreen = !logToScreen; },
				delegate { return logToScreen; }));

			menu.AddItem(new MenuItem(
				"lf", "Log to file",
				delegate { ToggleLogWriter(); },
				delegate { return logWriter != null; }));

			menu.AddItem(new MenuItem(
				"lc", "Log connections",
				delegate { server.LogConnections = !server.LogConnections; },
				delegate { return server.LogConnections; }));

			menu.AddItem(new MenuItem(
				"lr", "Log requests",
				delegate { server.LogRequests = !server.LogRequests; },
				delegate { return server.LogRequests; }));

			menu.AddItem(new MenuItem(
				"w", "Save settings",
				delegate { SaveSettings(); }));

			menu.AddItem(new MenuItem(
				"wq", "Save settings and quit",
				delegate { SaveSettings(); menu.Stop(); }));

			menu.AddItem(new MenuItem(
				"q!", "Discard changes and quit",
				delegate { menu.Stop(); }));
		}

		private void ToggleLogWriter()
		{
			if (logWriter != null)
				ToggleLogWriter(false);
			else
				ToggleLogWriter(true);
		}

		void ToggleLogWriter(bool logToFile)
		{
			if (logToFile)
			{
				if (logWriter == null)
				{
					try
					{
						FileStream stream = new FileStream("MiniHttpd.log", FileMode.Append, FileAccess.Write, FileShare.Read, 128);
						logWriter = new StreamWriter(stream);
						logWriter.AutoFlush = true;

						logWriter.WriteLine("-------------------------------------------------------");
						logWriter.WriteLine("* Beginning log at " + DateTime.Now);
						logWriter.WriteLine("-------------------------------------------------------");
					}
					catch { }
				}
			}
			else
			{
				if (logWriter != null)
				{
					logWriter.Close();
					logWriter = null;
				}
			}
		}

		private void SetRootPath(object sender, MenuItemSelectedEventArgs e)
		{
			string path = e.Args;
			if (e.Args == null)
			{
				Console.Write("Enter path: ");
				path = Console.ReadLine();
			}
			rootPath = path;
			if(server.Root is DriveDirectory)
				server.Root = new DriveDirectory(path);
		}

		private void ToogleRootFolderType()
		{
			if (server.Root is VirtualDirectory)
			{
				BinaryFormatter formatter = new BinaryFormatter();
				serializedVirtualRoot = new MemoryStream();
				formatter.Serialize(serializedVirtualRoot, server.Root);
				server.Root = new DriveDirectory(rootPath);
			}
			else
			{
				if (serializedVirtualRoot == null)
					server.Root = new VirtualDirectory();
				else
				{
					BinaryFormatter formatter = new BinaryFormatter();
					serializedVirtualRoot.Seek(0, SeekOrigin.Begin);
					server.Root = formatter.Deserialize(serializedVirtualRoot) as VirtualDirectory;
				}
			}
		}

		private void BrowseTree()
		{
			TreeBrowser browser = new TreeBrowser(server, server.Root);
			browser.Show();
			menu.ShowItems();
		}

		private void ShowHelp()
		{
			Console.WriteLine("MiniHttpd version: " + server.GetType().Assembly.GetName().Version);
			Console.WriteLine("Rei Miyasaka (rei@thefraser.com)");
			Console.WriteLine("http://www.codeproject.com/csharp/minihttpd.asp");
			Console.WriteLine();
			Console.WriteLine("Usage:");
			Console.WriteLine(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
				" [-s] [-l] [-p port] [-r path] [-n name]");
			Console.WriteLine("-s\tStart server in silent mode.");
			Console.WriteLine("-l\tLog to file.");
			Console.WriteLine("-p\tSelect a port.");
			Console.WriteLine("-r\tSet a root path.");
			Console.WriteLine("-n\tSet a host name.");
		}

		private void ParseArguments(string[] args)
		{
			for (int i = 0; i < args.Length; i++)
			{
				try
				{
					switch (args[i])
					{
						case "-s":
							silent = true;
							break;
						case "-l":
							ToggleLogWriter(true);
							server.LogConnections = true;
							server.LogRequests = true;
							break;
						case "-p":
							if (i + 1== args.Length)
								throw new Exception("Parameter required for switch -p");
							server.Port = int.Parse(args[++i]);
							break;
						case "-r":
							if (i + 1 == args.Length)
								throw new Exception("Parameter required for switch -r");
							server.Root = new DriveDirectory(args[++i]);
							break;
						case "-n":
							if (i + 1 == args.Length)
								throw new Exception("Parameter required for switch -n");
							server.HostName = args[++i];
							break;
						default:
							throw new Exception("Invalid argument: " + args[i]);
					}
				}
				catch(Exception e)
				{
					Console.Error.WriteLine(e.Message);
				}
			}
		}

		private void LoadSettings()
		{
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
				return;
			}

			try
			{
				rootPath = docElement["RootFolder"].InnerText;
			}
			catch { }

			try
			{
				DirectoryType type = (DirectoryType)Enum.Parse(typeof(DirectoryType), docElement["RootType"].InnerText);
				if ((server.Root is VirtualDirectory && type == DirectoryType.Drive) ||
					server.Root is DriveDirectory && type == DirectoryType.Virtual)
					ToogleRootFolderType();
			}
			catch { }

			try
			{
				serializedVirtualRoot = new MemoryStream(Convert.FromBase64String(docElement["VirtualDir"].InnerText));
				if (server.Root is VirtualDirectory)
				{
					BinaryFormatter formatter = new BinaryFormatter();
					serializedVirtualRoot.Seek(0, SeekOrigin.Begin);
					server.Root = formatter.Deserialize(serializedVirtualRoot) as VirtualDirectory;
				}
			}
			catch { }

			try
			{
				server.Port = int.Parse(docElement["Port"].InnerText);
			}
			catch { }

			try
			{
				autoStart = bool.Parse(docElement["AutoStart"].InnerText);
			}
			catch { }

			try
			{
				server.LogConnections = bool.Parse(docElement["LogConnections"].InnerText);
			}
			catch { }

			try
			{
				server.LogRequests = bool.Parse(docElement["LogRequests"].InnerText);
			}
			catch { }

			try
			{
				ToggleLogWriter(bool.Parse(docElement["LogToFile"].InnerText));
			}
			catch { }

			try
			{
				logToScreen = bool.Parse(docElement["LogToScreen"].InnerText);
			}
			catch { }

			try
			{
				server.HostName = docElement["HostName"].InnerText;
			}
			catch { }

			try
			{
				IndexPageStyle style = (IndexPageStyle)Enum.Parse(typeof(IndexPageStyle), docElement["IndexPageStyle"].InnerText);
				if (style == IndexPageStyle.Basic)
					server.IndexPage = new IndexPage();
				else
					server.IndexPage = new IndexPageEx();
			}
			catch { }
		}

		private void SaveSettings()
		{
			try
			{
				XmlDocument doc = new XmlDocument();
				doc.AppendChild(doc.CreateElement("MiniHttpdSettings"));
				XmlElement element;
				XmlElement docElement = doc.DocumentElement;

				element = doc.CreateElement("VirtualDir");
				docElement.AppendChild(element);

				if (server.Root is VirtualDirectory)
				{
					serializedVirtualRoot = new System.IO.MemoryStream();
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
				element.InnerText = server.Root is VirtualDirectory ? DirectoryType.Virtual.ToString() : DirectoryType.Drive.ToString();

				element = doc.CreateElement("Port");
				docElement.AppendChild(element);
				element.InnerText = server.Port.ToString();

				element = doc.CreateElement("AutoStart");
				docElement.AppendChild(element);
				element.InnerText = autoStart.ToString();

				element = doc.CreateElement("LogConnections");
				docElement.AppendChild(element);
				element.InnerText = server.LogConnections.ToString();

				element = doc.CreateElement("LogRequests");
				docElement.AppendChild(element);
				element.InnerText = server.LogRequests.ToString();

				element = doc.CreateElement("LogToFile");
				docElement.AppendChild(element);
				element.InnerText = (logWriter != null).ToString();

				element = doc.CreateElement("LogToScreen");
				docElement.AppendChild(element);
				element.InnerText = logToScreen.ToString();

				element = doc.CreateElement("HostName");
				docElement.AppendChild(element);
				element.InnerText = server.HostName;

				element = doc.CreateElement("IndexPageStyle");
				docElement.AppendChild(element);
				element.InnerText = server.IndexPage is IndexPageEx ? IndexPageStyle.Advanced.ToString() : IndexPageStyle.Basic.ToString();

				doc.Save("MiniHttpdSettings.xml");
				Console.WriteLine("Settings saved");
			}
#if !DEBUG
			catch (Exception e)
			{
				Console.Error.WriteLine("Error saving settings: ");
				Console.Error.WriteLine(e.ToString());
			}
#endif
			finally { }
		}

		private void SetHostName(object sender, MenuItemSelectedEventArgs e)
		{
			string input = e.Args;
			if (e.Args == null)
			{
				Console.Write("Enter host name: ");
				input = Console.ReadLine();
			}
			if (input.Length == 0)
				return;

			server.HostName = input;
		}

		private void SelectPort(object sender, MenuItemSelectedEventArgs e)
		{
			if (server.IsRunning)
			{
				Console.Error.WriteLine("Port cannot be changed while the server is running.");
				return;
			}
			string input = e.Args;
			if (e.Args == null)
			{
				Console.Write("Enter port: ");
				input = Console.ReadLine();
			}
			if (input.Length == 0)
				return;

			server.Port = int.Parse(input);
		}

		public enum DirectoryType
		{
			Drive,
			Virtual
		}

		public enum IndexPageStyle
		{
			Basic,
			Advanced
		}
	}
}
