using System;
using System.Threading;

namespace SocketServer
{
    class Program {
        // private static Int32 port = 4000;

        static void Main(string[] args) {
            Thread t = new Thread(delegate () {
                // replace the IP with your system IP Address...
                // Server myserver = new Server("192.168.***.***", 13000);
                Server myserver = new Server();
            });
            t.Start();
        
            Console.WriteLine("Server Started...!");        }
    }
}
