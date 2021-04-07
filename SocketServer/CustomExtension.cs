using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace SocketServer
{
    public static class CustomExtension {

        public static void ActionWorker(object locker, Action action) {
            try {
                Monitor.Enter(locker);
                action();
            } finally {
                Monitor.PulseAll(locker);
                Monitor.Exit(locker);
            }
        }

        public static int ActionWorker(object locker, Func<int> func) {
            try {
                Monitor.Enter(locker);
                return func();
            } finally {
                Monitor.PulseAll(locker);
                Monitor.Exit(locker);
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
            Monitor.Enter(locker);
            file.WriteLine("(Thread {0}) : {1}", threadId, msg);
            Monitor.PulseAll(locker);
            Monitor.Exit(locker);
        }

        public static bool AddToSet(this HashSet<string> set, 
            object locker, string value) {
            Monitor.Enter(locker);
            try {
                // Check and Add must be atomic Tx
                if (set.Where(i => i == value).Count() == 0) {
                    set.Add(value);
                    return true;
                }
                return false;
            }
            finally {
                Monitor.PulseAll(locker);
                Monitor.Exit(locker);
            }
        }


        public static void ConsoleWriteline(string threadId, string msg) {
            Console.WriteLine("(Thread {0}) {1}", threadId, msg);
        }

        public static void ExitMonitor(this object locker)
        {
            if (Monitor.IsEntered(locker))
                Monitor.Exit(locker);
        }
    }

}
