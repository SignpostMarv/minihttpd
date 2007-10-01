using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using MiniHttpd;

namespace MiniHttpdConsole
{
	class TreeBrowser
	{
		public TreeBrowser(HttpWebServer server, IDirectory directory)
		{
			this.server = server;
			current = directory;
			SetPrompt();

			menu.AddItem(new MenuItem(
				"ls", "List objects in directory",
				new EventHandler<MenuItemSelectedEventArgs>(List)));

			menu.AddItem(new MenuItem(
				"cd", "Change directory",
				new EventHandler<MenuItemSelectedEventArgs>(Cd)));

			menu.AddItem(new MenuItem(
				"mkdir", "Create a new directory",
				new EventHandler<MenuItemSelectedEventArgs>(Mkdir)));

			menu.AddItem(new MenuItem(
				"add", "Add a file or directory",
				new EventHandler<MenuItemSelectedEventArgs>(Add)));

			menu.AddItem(new MenuItem(
				"link", "Create a redirect",
				new EventHandler<MenuItemSelectedEventArgs>(Redirect)));

			menu.AddItem(new MenuItem(
				"rm", "Remove a file or a directory and all its contents",
				new EventHandler<MenuItemSelectedEventArgs>(Rm)));

			menu.AddItem(new MenuItem(
				"loc", "Show the location of a resource",
				new EventHandler<MenuItemSelectedEventArgs>(Location)));

			menu.AddItem(new MenuItem(
				"q", "Return to main menu", delegate { menu.Stop(); }));

			menu["mkdir"].Showing += delegate { menu["mkdir"].Enabled = current is VirtualDirectory; };
			menu["add"].Showing += delegate { menu["add"].Enabled = current is VirtualDirectory; };
			menu["link"].Showing += delegate { menu["link"].Enabled = current is VirtualDirectory; };
		}

		void Mkdir(object sender, MenuItemSelectedEventArgs e)
		{

			if (!(current is VirtualDirectory))
				throw new MenuItemUsageException("Cannot create directory in " + current.GetType().Name);

			string name;
			if (e.Args != null)
				name = e.Args;
			else
			{
				Console.Write("Enter directory name: ");
				name = Console.ReadLine();
			}

			(current as VirtualDirectory).AddDirectory(new VirtualDirectory(name, current));
		}

		void Cd(object sender, MenuItemSelectedEventArgs e)
		{
			string path;
			if (e.Args != null)
				path = e.Args;
			else
			{
				Console.WriteLine("Enter path: ");
				path = Console.ReadLine();
			}

			IResource resource = NavigateTo(current, path);
			if (resource == null)
				throw new MenuItemUsageException("Directory not found.");

			if (!(resource is IDirectory))
				throw new MenuItemUsageException("Path specified is not a directory.");

			current = resource as IDirectory;
			SetPrompt();
		}

		void List(object sender, MenuItemSelectedEventArgs e)
		{
			foreach (IDirectory directory in current.GetDirectories())
				Console.WriteLine("[" + directory.Name + "]");
			foreach (IFile file in current.GetFiles())
				Console.WriteLine(file.Name);
		}

		void Location(object sender, MenuItemSelectedEventArgs e)
		{
			string path;
			if (e.Args != null)
				path = e.Args;
			else
			{
				Console.Write("Enter path: ");
				path = Console.ReadLine();
			}

			IResource resource = NavigateTo(current, path);
			if (resource == null)
				throw new MenuItemUsageException("Path not found.");

			if (resource is VirtualDirectory)
				Console.WriteLine("Resource is a virtual directory.");
			else if (resource is IPhysicalResource)
				Console.WriteLine("Location: " + (resource as IPhysicalResource).Path);
			else if (resource is RedirectFile)
				Console.WriteLine("URL: " + (resource as RedirectFile).Redirect);
		}

		void Add(object sender, MenuItemSelectedEventArgs e)
		{
			if (!(current is VirtualDirectory))
				throw new MenuItemUsageException("Cannot add files to " + current.GetType().Name);
			string path;
			if (e.Args != null)
				path = e.Args;
			else
			{
				Console.Write("Enter path: ");
				path = Console.ReadLine();
			}

			path = Path.GetFullPath(path);

			if (Directory.Exists(path))
				(current as VirtualDirectory).AddDirectory(path);
			else if (File.Exists(path))
				(current as VirtualDirectory).AddFile(path);
			else
				throw new MenuItemUsageException("File not found.");
		}

		void Redirect(object sender, MenuItemSelectedEventArgs e)
		{
			if (!(current is VirtualDirectory))
				throw new MenuItemUsageException("Cannot create directory in " + current.GetType().Name);

			string name;
			string url;
			Console.Write("Enter name of redirect: ");
			if (string.IsNullOrEmpty(name = Console.ReadLine()))
				throw new MenuItemUsageException("You must enter a name.");
			Console.Write("Enter redirect URL: ");
			url = Console.ReadLine();

			(current as VirtualDirectory).AddFile(new RedirectFile(name, url, current));
		}

		void Rm(object sender, MenuItemSelectedEventArgs e)
		{
			string path;
			if (e.Args != null)
				path = e.Args;
			else
			{
				Console.Write("Enter path: ");
				path = Console.ReadLine();
			}

			IResource resource = NavigateTo(current, path);
			if (resource == null)
				throw new MenuItemUsageException("Resource not found.");

			if (resource.Parent == null)
				throw new MenuItemUsageException("Root cannot be removed.");

			if (!(resource.Parent is VirtualDirectory))
				throw new MenuItemUsageException("Parent of resource must be a virtual directory.");

			(resource.Parent as VirtualDirectory).Remove(resource.Name);
		}

		IResource NavigateTo(IDirectory directory, string path)
		{
			//TODO: enhance this for multiple nodes
			if (path == "..")
				return directory.Parent;
			else if (path == "/")
			{
				if (directory.Parent == null)
					return directory;
				else
					return NavigateTo(directory.Parent, "/");
			}
			else
			{
				IResource resource = directory.GetDirectory(path);
				if (resource != null)
					return resource;
				else
					return directory.GetFile(path);
			}
		}

		MenuView menu = new MenuView();

		IDirectory current;
		HttpWebServer server;

		void SetPrompt()
		{
			menu.Prompt = server.GetUrl(current).ToString().Replace(" ", "+") + " ";
		}

		public void Show()
		{
			menu.DoMenuModal();
		}
	}
}
