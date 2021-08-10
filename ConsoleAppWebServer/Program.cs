using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace ConsoleAppWebServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Webserver webserver = new Webserver();
            IPAddress ip = IPAddress.Parse("192.168.221.1");
            webserver.start(ip, 80, 10, @"C:\Users\uncha\Desktop\HtmlTemp");

            Console.WriteLine("serveren kører...");
            Console.WriteLine("Push a key to stop the server");
            Console.ReadKey();

            webserver.stop();
            Console.WriteLine("serveren er stopedet");
            Console.ReadKey();
        }
    }
}
