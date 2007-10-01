using System;
using System.Collections.Generic;
using System.Text;

namespace MiniHttpdConsole
{
	class MenuView
	{

		public MenuView()
		{
			AddItem(new MenuItem("?", "Show menu items", delegate { ShowItems(); }));
		}

		Dictionary<string, MenuItem> itemsByCommand = new Dictionary<string, MenuItem>();
		List<MenuItem> items = new List<MenuItem>();

		public MenuItem this[string command]
		{
			get
			{
				return itemsByCommand[command];
			}
		}

		string title = "Menu";

		public string Title
		{
			get { return title; }
			set { title = value; }
		}

		string prompt = "Selection: ";

		public string Prompt
		{
			get { return prompt; }
			set { prompt = value; }
		}

		public void AddItem(MenuItem item)
		{
			itemsByCommand.Add(item.Command, item);
			items.Add(item);
		}

		public void DoMenu()
		{
			string input;
			for(;;)
			{
				Console.Write(prompt);
				input = Console.ReadLine();
				Console.WriteLine();

				string command = null;
				string args = null;
				int spaceIndex = input.IndexOf(' ');
				if (spaceIndex >= 0)
				{
					command = input.Substring(0, spaceIndex).Trim();
					args = input.Substring(spaceIndex).Trim();
				}
				else
					command = input;

				if (!itemsByCommand.ContainsKey(command))
				{
					Console.Error.WriteLine("Invalid input. Type '?' for menu.");
					return;
				}

				MenuItem item = itemsByCommand[command];

				item.OnSelected(args);

				break;
			}
		}

		bool isRunning;

		public bool IsRunning
		{
			get { return isRunning; }
			set { isRunning = value; }
		}

		StatusView statusView;

		internal StatusView StatusView
		{
			get { return statusView; }
			set { statusView = value; }
		}

		bool showStatus = true;

		public bool ShowStatus
		{
			get { return showStatus; }
			set { showStatus = value; }
		}

		public void DoMenuModal()
		{
			isRunning = true;
			ShowItems();
			while (isRunning)
			{
				DoMenu();

				if (!isRunning)
					break;

				if (statusView != null && showStatus)
					statusView.ShowChangedValues();
				Console.WriteLine();
			}
		}

		public void Stop()
		{
			isRunning = false;
		}

		public void ShowItems()
		{
			Console.WriteLine(title);
			for (int i = 0; i < title.Length; i++)
				Console.Write("-");
			Console.WriteLine();

			foreach (MenuItem item in items)
			{
				item.Show();
			}
		}
	}

	public class MenuItemSelectedEventArgs : EventArgs
	{
		public MenuItemSelectedEventArgs(string args)
		{
			this.args = args;
		}

		string args;

		public string Args
		{
			get { return args; }
		}
	}

	public class MenuItemUsageException : Exception
	{
		public MenuItemUsageException(string message)
			: base(message)
		{
		}
	}

	public delegate object GetValueHandler();

	class MenuItem
	{
		public MenuItem(string command, string description, EventHandler<MenuItemSelectedEventArgs> selected)
			: this(command, description, selected, null)
		{
		}

		public MenuItem(string command, string description, EventHandler<MenuItemSelectedEventArgs> selected, GetValueHandler value)
		{
			this.command = command;
			this.description = description;
			this.Selected = selected;
			this.GetValue = value;
		}

		string command;

		public string Command
		{
			get { return command; }
		}

		string description;

		public string Description
		{
			get { return description; }
			set { description = value; }
		}

		bool enabled = true;

		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		public void Show()
		{
			if (Showing != null)
				Showing(this, null);
			if (!Enabled)
				return;
			Console.Write(Command + "\t" + Description);
			if (GetValue != null)
				Console.Write(" (" + GetValue() + ")");
			Console.WriteLine();
		}

		public void OnSelected(string args)
		{
			if (Showing != null)
				Showing(this, null);

			if (!Enabled)
			{
				Console.Error.WriteLine("Menu item is disabled. Type '?' for menu.");
				return;
			}

			if (Selected != null)
			{

				if (string.IsNullOrEmpty(args))
					args = null;

				foreach(EventHandler<MenuItemSelectedEventArgs> handler in Selected.GetInvocationList())
					try
					{
						handler(this, new MenuItemSelectedEventArgs(args));
					}
					catch(Exception e)
					{
						Console.Error.WriteLine(e.Message);
					}
			}
		}

		public event EventHandler<MenuItemSelectedEventArgs> Selected;
		public event EventHandler Showing;
		public GetValueHandler GetValue;

		//TODO: parameters
	}
}
