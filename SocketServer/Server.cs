using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketServer {
    class Server {
        private static readonly string _locker = "THREAD_LOCKER";
        private static readonly int _maxThreadCount = 5;
        private static int _threadCount;
        private static bool _terminated = false;

        private static void AddThreadCount(int i) {
            _threadCount += i;
        }

        TcpListener server = null;
        public Server(string ip, int port) {
            _threadCount = 0;
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);
            // server.Server.ReceiveTimeout = 1000;
            // server.Server.SendTimeout = 1000;
            server.Start();
            StartListener();
        }

        public void StartListener() {
            try {
                while (true) {

                    Console.WriteLine("Waiting for a connection...");
                    while(!server.Pending()) {
                        if (_terminated) {
                            Console.WriteLine("Server shutdown by termination request.");
                            return;
                        }
                        Thread.Sleep(10);
                    }
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connection Request received!");

                    Monitor.Enter(_locker);            
                    try {
                        Console.WriteLine("{0} out of {1} connections available.", 
                            _maxThreadCount - _threadCount,
                            _maxThreadCount);

                        if (_threadCount < 5) {
                            Thread t = new Thread(
                                new ParameterizedThreadStart(HandleDeivce));
                            t.Start(client);
                            Monitor.Enter(_locker);
                            AddThreadCount(1);
                            Monitor.Exit(_locker);
                            Console.WriteLine("Connection created.");
                        }
                    } catch (Exception ex){
                        Console.WriteLine(ex.Message);
                    } finally {
                        if (Monitor.IsEntered(_locker))
                            Monitor.Exit(_locker);
                    }
                }
            }
            catch (SocketException e) {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }
        }

        public void HandleDeivce(Object obj) {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            stream.ReadTimeout = 5000;
            string data = null;
            Byte[] bytes = new Byte[256];
            int i;

            while (!_terminated) {
                try {
                    i = stream.Read(bytes, 0, bytes.Length);
                    if (i == 0) {
                        // Connection closed by client
                        // No need to keep the thread
                        client.Close();
                        return;
                    }
                } catch(IOException) {
                    // ReadTimeout causes IOException
                    continue;
                }
                 catch(Exception e) {
                    Console.WriteLine("Exception while reading stream: {0}", e.Message);
                    continue;
                }

                try {
                    // string hex = BitConverter.ToString(bytes);
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("{1}: Received: {0}", 
                        data, 
                        Thread.CurrentThread.ManagedThreadId);

                    if (data == "terminate") {
                        string terminationMsg = "Termination requested. Server is being shutdown.";
                        Console.WriteLine("{1}: Sent: {0}", 
                            terminationMsg, 
                            Thread.CurrentThread.ManagedThreadId);
                        sendMessage(stream, terminationMsg);

                        Monitor.Enter(_locker);
                        _terminated = true;
                        AddThreadCount(-1);
                        Monitor.PulseAll(_locker);
                        Monitor.Exit(_locker);
                        client.Close();
                        return;
                    }
                    else if (data.Length != 9 || !long.TryParse(data, out long x))
                    {
                        string msg = "Invalud value entered. Connection is being terminated.";
                        Console.WriteLine("{1}: Sent: {0}", 
                            msg, 
                            Thread.CurrentThread.ManagedThreadId);
                        sendMessage(stream, msg);
                        Monitor.Enter(_locker);
                        AddThreadCount(-1);
                        Monitor.PulseAll(_locker);
                        Monitor.Exit(_locker);
                        client.Close();
                        return;
                    }

                    string str = "Transmission Acknowledged";
                    sendMessage(stream, str);
                    Console.WriteLine("{1}: Sent: {0}", 
                        str, 
                        Thread.CurrentThread.ManagedThreadId);
                } catch(Exception ex) {
                    throw ex;
                }
                finally {
                    if (Monitor.IsEntered(_locker))
                        Monitor.Exit(_locker);
                }
            }

            string msg2 = "Request deined. Server is being terminated.";
            Console.WriteLine("{1}: Sent: {0}", 
                msg2, 
                Thread.CurrentThread.ManagedThreadId);
            sendMessage(stream, msg2);
            Monitor.Enter(_locker);
            AddThreadCount(-1);
            Monitor.PulseAll(_locker);
            Monitor.Exit(_locker);
            client.Close();
        }

        private static void sendMessage(NetworkStream stream, string msg) {
            Byte[] replyBytes = System.Text.Encoding.ASCII.GetBytes(msg);
            stream.Write(replyBytes, 0, replyBytes.Length);
        }
    }
}
