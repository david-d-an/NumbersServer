using System;
using System.Threading;

namespace SocketServer
{
    class Program {
        static void Main(string[] args) {
            // These three thread creating is functinally identical
            // delegate() is anonymous delegate
            // new Thread(delegate() {
            //     Server myserver = new Server();
            // }).Start();

            new Thread(new ThreadStart(() => {
                Server myserver = new Server();
            })).Start();        

            new Thread(() => {
                Server myserver = new Server();
            }).Start();

            Console.WriteLine("Server Started...!");
        }
    }
}
