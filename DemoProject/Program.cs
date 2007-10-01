using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MiniHttpd;
using DemoProject.WebServer;

namespace DemoProject
{
    class Program
    {
        static void Main(string[] args)
        {
            DemoProject.WebServer.WebServer server = new DemoProject.WebServer.WebServer(81);
            server.AddUrlHandler("/", RootDir);
            server.AddUrlHandler("/shit.xhtml", RootDoc);
            server.Start();
            System.Threading.Thread.Sleep(30000);
        }

        static void RootDir(HttpRequest request, HttpResponse response)
        {
            response.WriteText("Hello World!", true);
        }

        static void RootDoc(HttpRequest request, HttpResponse response)
        {
            response.WriteText("This is the doc", true);
        }
    }
}
