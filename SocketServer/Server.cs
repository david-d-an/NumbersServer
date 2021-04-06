using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketServer {
    class Server {
        private string logFileName = "./Logs/result.log";
        private string ip = "127.0.0.1";
        private int port = 4000;

        private readonly string _locker = "THREAD_LOCKER";
        private readonly int _maxThreadCount = 5;
        private int _threadCount;
        private static bool _terminated = false;

        private StreamWriter logFile;
        private int inputCount = 0;
        private int uniqueCount = 0;
        private int duplicateCount = 0;
        private HashSet<long> data = new HashSet<long>();

        private void IncrementThreadCount(object locker, int i = 1) {
            Monitor.Enter(locker);
            _threadCount += i;
            Monitor.PulseAll(locker);
            Monitor.Exit(locker);
        }
        private void DecrementThreadCount(object locker, int i = 1) {
            Monitor.Enter(locker);
            _threadCount -= i;
            Monitor.PulseAll(locker);
            Monitor.Exit(locker);
        }

        private void ToggleTerminated(object locker, bool val) {
            Monitor.Enter(locker);
            _terminated = val;
            Monitor.PulseAll(locker);
            Monitor.Exit(locker);
        }

        public int GetThreadCount(object locker) { 
            try {
                Monitor.Enter(locker);
                return _threadCount;
            } finally {
                Monitor.PulseAll(locker);
                Monitor.Exit(locker);
            }
        }

        TcpListener server = null;
        public Server() {
            _threadCount = 0;
            Directory.CreateDirectory("./Logs");
            logFile = new StreamWriter(logFileName) { AutoFlush = true };
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);
            // server.Server.ReceiveTimeout = 1000;
            // server.Server.SendTimeout = 1000;
            try {
                server.Start();
                StartListener();
            } catch (SocketException e) {
                Console.WriteLine("SocketException: {0}", e);
            } catch (Exception e) {
                Console.WriteLine("Exception: {0}", e);
            } finally {
                server.Stop();
            }
        }

        public void StartListener() {
            Console.WriteLine("Waiting for a connection...");

            while (true) {
                while(!server.Pending()) {
                    if (_terminated) {
                        Console.WriteLine("Server shutdown by termination request.");
                        return;
                    }
                    Thread.Sleep(10);
                }
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connection Request received!");

                try {
                    Console.WriteLine("{0} out of {1} connections available.", 
                        _maxThreadCount - GetThreadCount(_locker),
                        _maxThreadCount);

                    if (GetThreadCount(_locker) < 5) {
                        Thread t = new Thread(
                            new ParameterizedThreadStart(HandleDeivce));
                        t.Start(client);
                        IncrementThreadCount(_locker);
                        Console.WriteLine("Connection created.");
                    }
                } catch (Exception ex){
                    // Exception is isolated one thread instance
                    // Keep the server going
                    Console.WriteLine(ex.Message);
                } finally {
                    // ExitMonitor(_locker);
                    _locker.ExitMonitor();
                }
            }
        }

        public void HandleDeivce(Object obj) {
            TcpClient client = (TcpClient)obj;
            int threadId = Thread.CurrentThread.ManagedThreadId;
            var stream = client.GetStream();
            stream.ReadTimeout = 5000;
            string data = null;
            Byte[] bytes = new Byte[256];
            int i;            

            while (!_terminated) {
                try {
                    i = stream.Read(bytes, 0, bytes.Length);
                    if (i == 0) {
                        // Termination requested by client
                        // No need to keep the thread
                        client.Close();
                        return;
                    }
                } catch(IOException) {
                    // ReadTimeout causes IOException
                    continue;
                } catch(Exception e) {
                    Console.WriteLine("Exception while reading stream: {0}", e.Message);
                    continue;
                }

                try {
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("{1}: Received: {0}", data, threadId);

                    if (data == "terminate") {
                        string terminationMsg = "Termination requested. Server is being shutdown.";
                        Console.WriteLine("{1}: Sent: {0}", terminationMsg, threadId);
                        stream.SendMessage(terminationMsg);

                        ToggleTerminated(_locker, true);
                        DecrementThreadCount(_locker);
                        client.Close();
                        return;
                    }
                    else if (data.Length != 9 || !long.TryParse(data, out long x))
                    {
                        string msg = "Invalud value entered. Connection is being terminated.";
                        Console.WriteLine("{1}: Sent: {0}", msg, threadId);
                        stream.SendMessage(msg);
                        DecrementThreadCount(_locker);
                        client.Close();
                        return;
                    }

                    string str = "Transmission Acknowledged";
                    logFile.WriteInLog(_locker, data + " : " + threadId);
                    stream.SendMessage(str);
                    Console.WriteLine("{1}: Sent: {0}", str, threadId);
                } catch(Exception ex) {
                    Console.WriteLine("Exception: {0}", ex.Message);
                    logFile.Close();
                    // throw ex;
                    return;
                } finally {
                    _locker.ExitMonitor();
                }
            }

            // This part reached when request arrived while termination in progress
            string denialMsg = "Request deined. Server is being terminated.";
            Console.WriteLine("{1}: Sent: {0}", denialMsg, threadId);
            stream.SendMessage(denialMsg);
            DecrementThreadCount(_locker);
            client.Close();
        }
    }

    public static class CustomClassExtension {
        public  static void SendMessage(this NetworkStream stream, string msg) {
            Byte[] replyBytes = System.Text.Encoding.ASCII.GetBytes(msg);
            stream.Write(replyBytes, 0, replyBytes.Length);
        }

        public static void WriteInLog(this StreamWriter file, object locker, string msg) {
            Monitor.Enter(locker);
            file.WriteLine(msg);
            Monitor.PulseAll(locker);
            Monitor.Exit(locker);
        }

        public static void ExitMonitor(this object locker)
        {
            if (Monitor.IsEntered(locker))
                Monitor.Exit(locker);
        }
    }

}
