using System;
using MiniHttpd;

namespace MiniHttpdConsole
{
	/// <summary>
	/// Summary description for RedirectFile.
	/// </summary>
	[Serializable]
	public class RedirectFile : IFile
	{
		public RedirectFile(string name, string redirect, IDirectory parent)
		{
			this.name = name;
			this.redirect = redirect;
			this.parent = parent;
		}

		string name;
		string redirect;
		IDirectory parent;

		public string Redirect
		{
			get
			{
				return redirect;
			}
		}

		#region IFile Members

		public void OnFileRequested(HttpRequest request, IDirectory directory)
		{
			request.Response.ResponseCode = "302";
			request.Response.SetHeader("Location", redirect);
		}

		public string ContentType
		{
			get
			{
				return null;
			}
		}

		#endregion

		#region IResource Members

		public string Name
		{
			get
			{
				return name;
			}
		}

		public IDirectory Parent
		{
			get
			{
				return parent;
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			// TODO:  Add RedirectFile.Dispose implementation
		}

		#endregion

		public override string ToString()
		{
			return name;
		}

	}
}
