using System;
using System.Threading;

namespace SocketServer
{
    class Program {
        static void Main(string[] args) {
            new Thread(delegate () {
                Server myserver = new Server();
            }).Start();
        
            Console.WriteLine("Server Started...!");        }
    }
}
