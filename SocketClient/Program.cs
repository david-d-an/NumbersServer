using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace SocketClient
{

    class Program {
        // private static Int32 port = 4000;

        static void Main(string[] args) {
            // new Thread(() => {
            //     Thread.CurrentThread.IsBackground = true; 
            //     Connect("127.0.0.1");
            // }).Start();

            // new Thread(delegate () {
            //     Client myclient = new Client();
            // }).Start();
            // new Thread(() => new Client()).Start();

            new Thread(() => new Client(1, "./Data/a1.txt")).Start();
            new Thread(() => new Client(2, "./Data/a2.txt")).Start();
            new Thread(() => new Client(3, "./Data/a3.txt")).Start();
            new Thread(() => new Client(4, "./Data/a4.txt")).Start();
            new Thread(() => new Client(5, "./Data/a5.txt")).Start();
            new Thread(() => new Client(6, "./Data/a6.txt")).Start();
            new Thread(() => new Client(7, "./Data/a7.txt")).Start();
       }
    }
}
