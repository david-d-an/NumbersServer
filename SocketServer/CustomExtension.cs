using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace SocketServer
{
    public static class Util {

        public static void Monitor(object locker, Action action) {
            try {
                System.Threading.Monitor.Enter(locker);
                action();
            } finally {
                System.Threading.Monitor.PulseAll(locker);
                System.Threading.Monitor.Exit(locker);
            }
        }

        public static int Monitor(object locker, Func<int> func) {
            try {
                System.Threading.Monitor.Enter(locker);
                return func();
            } finally {
                System.Threading.Monitor.PulseAll(locker);
                System.Threading.Monitor.Exit(locker);
            }
        }

        public  static void SendMessage(this NetworkStream stream, string msg) {
            Byte[] replyBytes = System.Text.Encoding.ASCII.GetBytes(msg);
            stream.Write(replyBytes, 0, replyBytes.Length);
        }

        public static void WriteInLog(
            this StreamWriter file, 
            object locker, 
            string threadId,
            string msg) {
            System.Threading.Monitor.Enter(locker);
            file.WriteLine("(Thread {0}) : {1}", threadId, msg);
            System.Threading.Monitor.PulseAll(locker);
            System.Threading.Monitor.Exit(locker);
        }

        public static bool AddToSet(this HashSet<string> set, 
            object locker, string value) {
            System.Threading.Monitor.Enter(locker);
            try {
                // Check and Add must be atomic Tx
                if (set.Where(i => i == value).Count() == 0) {
                    set.Add(value);
                    return true;
                }
                return false;
            }
            finally {
                System.Threading.Monitor.PulseAll(locker);
                System.Threading.Monitor.Exit(locker);
            }
        }


        public static void ConsoleWriteline(string threadId, string msg) {
            Console.WriteLine("(Thread {0}) {1}", threadId, msg);
        }

        public static void ExitMonitor(this object locker)
        {
            if (System.Threading.Monitor.IsEntered(locker))
                System.Threading.Monitor.Exit(locker);
        }
    }

}
