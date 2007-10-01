using System;
using System.Web;
using MiniHttpd.FileSystem;

namespace MiniHttpd.Aspx
{
    /// <summary>
    /// Summary description for AspxAppHost.
    /// </summary>
    internal class AspxAppHost : MarshalByRefObject
    {
        public void ProcessRequest(HttpRequest request, DriveFile file, string virtualPath, string physicalDir)
        {
            HttpRuntime.ProcessRequest(new WorkerRequest(request, file, virtualPath, physicalDir));
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Unload()
        {
            HttpRuntime.UnloadAppDomain();
        }
    }
}