using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketServer
{
    class Server {
        private TcpListener server = null;
        private readonly string logFileName = "./Logs/result.log";
        private readonly string ip = "127.0.0.1";
        private readonly int port = 4000;

        private readonly string _locker = "THREAD_LOCKER";
        private readonly int _maxThreadCount = 5;
        private int _threadCount;
        private static bool _terminated = false;

        private StreamWriter logFile;
        private int _inputCount = 0;
        private int _inputCountSession = 0;
        private int _uniqueCount = 0;
        private int _uniqueCountSession = 0;
        private int _duplicateCount = 0;
        private int _duplicateCountSession = 0;


        // Use Hash set for average O(1) performance.
        private HashSet<string> hashSet = new HashSet<string>();

        #region ThreadCount
        private int GetThreadCount(object locker) =>
            CustomExtension.ActionWorker(locker, () => _threadCount);
        private void IncrementThreadCount(object locker, int i = 1) =>
            CustomExtension.ActionWorker(locker, () => _threadCount += i);
        private void DecrementThreadCount(object locker, int i = 1) =>
            CustomExtension.ActionWorker(locker, () => _threadCount -= i);
        #endregion

        private void ToggleTerminated(object locker, bool val) =>
            CustomExtension.ActionWorker(locker, () => _terminated = val);

        #region InputCount

        private int GetInputCount(object locker) =>
            CustomExtension.ActionWorker(locker, () => _inputCount);
        private void IncrementInputCount(object locker, int i = 1) =>
            CustomExtension.ActionWorker(locker, () => _inputCount += i);
        private int GetInputCountSession(object locker) =>
            CustomExtension.ActionWorker(locker, () => _inputCountSession);
        private void IncrementInputCountSession(object locker, int i = 1) =>
            CustomExtension.ActionWorker(locker, () => _inputCountSession += i);
        #endregion

        #region UniqueCount
        private int GetUniqueCount(object locker) =>
            CustomExtension.ActionWorker(locker, () => _uniqueCount);
        private void IncrementUniquCount(object locker, int i = 1) =>
            CustomExtension.ActionWorker(locker, () => _uniqueCount += i);
        private int GetUniqueCountSession(object locker) =>
            CustomExtension.ActionWorker(locker, () => _uniqueCountSession);
        private void IncrementUniquCountSession(object locker, int i = 1) =>
            CustomExtension.ActionWorker(locker, () => _uniqueCountSession += i);
        #endregion

        #region DuplicateCount
        private int GetDuplicateCount(object locker) =>
            CustomExtension.ActionWorker(locker, () => _duplicateCount);
        private void IncrementDuplicateCount(object locker, int i = 1) =>
            CustomExtension.ActionWorker(locker, () => _duplicateCount += i);
        private int GetDuplicateCountSession(object locker) =>
            CustomExtension.ActionWorker(locker, () => _duplicateCountSession);
        private void IncrementDuplicateCountSession(object locker, int i = 1) =>
            CustomExtension.ActionWorker(locker, () => _duplicateCountSession += i);
        #endregion

        private void FlushCounters(object locker) {
            Monitor.Enter(locker);
            _duplicateCount += _duplicateCountSession;
            _uniqueCount += _uniqueCountSession;
            _inputCount += _inputCountSession;
            _duplicateCountSession = 0;
            _uniqueCountSession = 0;
            _inputCountSession = 0;
            Monitor.PulseAll(locker);
            Monitor.Exit(locker);
        }

        public Server() {
            _threadCount = 0;
            Directory.CreateDirectory("./Logs");
            logFile = new StreamWriter(logFileName) { AutoFlush = true };
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);
            // server.Server.ReceiveTimeout = 1000;
            // server.Server.SendTimeout = 1000;
            try {
                new Thread(() => Notifiy()).Start();

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
            string threadId = 
                Thread.CurrentThread.ManagedThreadId
                .ToString().PadLeft(2, '0');
            var stream = client.GetStream();
            stream.ReadTimeout = 5000;
            string data = null;
            Byte[] bytes = new Byte[256];
            int i;            

            while (!_terminated) {
                try {
                    i = stream.Read(bytes, 0, bytes.Length);
                    if (i == 0) {
                        // Client disconnected
                        // No need to keep the thread
                        DecrementThreadCount(_locker);
                        client.Close();
                        return;
                    }
                } catch(IOException) {
                    // ReadTimeout causes IOException
                    continue;
                } catch(Exception e) {
                    Console.WriteLine(
                        "(Thread {0}) Exception while reading stream: {1}", 
                        threadId, e.Message);
                    continue;
                }

                try {
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    // Console.WriteLine(
                    //     "(Thread {0}) Received: {1}", 
                    //     threadId, data);

                    if (data == "terminate") {
                        string terminationMsg = 
                            "Termination requested. Server is being shutdown.";
                        Console.WriteLine(
                            "(Thread {0}) Sent: {1}", 
                            threadId, terminationMsg);
                        stream.SendMessage(terminationMsg);

                        ToggleTerminated(_locker, true);
                        DecrementThreadCount(_locker);
                        client.Close();
                        return;
                    }
                    else if (data.Length != 9 || !long.TryParse(data, out long x))
                    {
                        string msg = 
                            "Invalid value entered. Connection is being terminated.";
                        Console.WriteLine(
                            "(Thread {0}) Sent: {1}", 
                            threadId, msg);
                        stream.SendMessage(msg);
                        DecrementThreadCount(_locker);
                        client.Close();
                        return;
                    }


                    IncrementInputCountSession(_locker);
                    if (hashSet.AddToSet(_locker, data)) {
                        IncrementUniquCountSession(_locker);
                        logFile.WriteInLog(_locker, threadId, data);
                        string str = "Accepted: duplicate was NOT found";
                        stream.SendMessage(str);
                        // Console.WriteLine("(Thread {0}) Sent: {1}", threadId, str);
                    } else {
                        IncrementDuplicateCountSession(_locker);
                        string str = "Rejected: duplicate was found (" + data + ")";
                        stream.SendMessage(str);
                        Console.WriteLine("(Thread {0}) Sent: {1}", threadId, str);
                    }

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
            Console.WriteLine("(Thread {0}) Sent: {1}", threadId, denialMsg);
            stream.SendMessage(denialMsg);
            DecrementThreadCount(_locker);
            client.Close();
        }


        public void Notifiy() {
            while(!_terminated) {
                Thread.Sleep(10000);

                String msg = 
                    "Received: " + DateTime.Now.ToString("HH:mm:ss") +
                    Environment.NewLine +
                    Environment.NewLine +
                    GetUniqueCountSession(_locker) + " unique numbers, " +
                    GetDuplicateCountSession(_locker) + " duplicates. " +
                    "Unique total: " + (GetUniqueCount(_locker) + GetUniqueCountSession(_locker));
                Console.WriteLine(msg);
                FlushCounters(_locker);
            }
        }
    }

}
