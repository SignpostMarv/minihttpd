using System;
using System.Collections.Generic;
using System.Text;

namespace MiniHttpdConsole
{
	class StatusView
	{
		List<StatusItem> items = new List<StatusItem>();
		Dictionary<string, StatusItem> itemsByName = new Dictionary<string, StatusItem>();

		public StatusItem this[string name]
		{
			get
			{
				return itemsByName[name];
			}
		}

		string title;

		public string Title
		{
			get { return title; }
			set { title = value; }
		}

		public void AddItem(StatusItem item)
		{
			if (!string.IsNullOrEmpty(item.Name))
				itemsByName.Add(item.Name, item);
			items.Add(item);
		}

		public void ShowValues()
		{
			Console.WriteLine(title);
			for (int i = 0; i < title.Length; i++)
				Console.Write("-");
			Console.WriteLine();

			foreach (StatusItem item in items)
				item.ShowStatus();
		}

		public void ShowChangedValues()
		{

			foreach (StatusItem item in items)
				item.ShowStatus(true);
		}
	}

	class StatusItem
	{
		public StatusItem(string description, GetValueHandler getValue)
			: this(null, description, getValue)
		{
		}

		public StatusItem(string name, string description, GetValueHandler getValue)
		{
			if (getValue == null)
				throw new ArgumentNullException("getValue");

			this.name = name;
			this.description = description;
			this.GetValue = getValue;

			object value = getValue();
			if (value == null)
				lastValue = null;
			else
				lastValue = value.ToString();
		}

		string name;

		public string Name
		{
			get { return name; }
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

		public void ShowStatus()
		{
			ShowStatus(false);
		}

		public void ShowStatus(bool onlyIfChanged)
		{

			if (GetValue == null)
				return;

			if (Showing != null)
				Showing(this, null);

			if (!enabled)
				return;

			string value = GetValue().ToString();
			if (!onlyIfChanged || (!string.Equals(value, lastValue)))
			{
				if (string.IsNullOrEmpty(description))
					Console.WriteLine(value);
				else
					Console.WriteLine(description + ": " + value);
			}

			lastValue = value;
		}

		public event EventHandler Showing;

		string lastValue;

		public GetValueHandler GetValue;
	}
}
