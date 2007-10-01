using System;
using System.IO;

namespace MiniHttpdConsole
{
	/// <summary>
	/// Summary description for ConsoleWriter.
	/// </summary>
	public class ConsoleWriter : TextWriter
	{

		public override System.Text.Encoding Encoding
		{
			get
			{
				return System.Text.Encoding.Default;
			}
		}

		public override void Write(char[] buffer, int index, int count)
		{
			if (OnWrite != null)
				OnWrite(buffer, index, count);
		}

		public override void Write(char value)
		{
			Write(new char[] { value }, 0, 1);
		}

		public delegate void WriteEventHandler(char[] buffer, int index, int count);
		public event WriteEventHandler OnWrite;
	}
}