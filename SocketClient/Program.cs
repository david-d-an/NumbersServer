using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace SocketClient {
    class Program {
        private static Int32 port = 4000;

        static void Main(string[] args) {
            // new Thread(() => {
            //     Thread.CurrentThread.IsBackground = true; 
            //     Connect("127.0.0.1");
            // }).Start();
            Connect("127.0.0.1");
        }

        static void Connect(String server) {
            try {
                using(TcpClient client = new TcpClient(server, port))
                using(NetworkStream stream = client.GetStream())
                while (true) {
                    stream.ReadTimeout = 5000;
                    var message = Console.ReadLine();
                    if (message.Length == 0)
                        continue;

                    // Translate the Message into ASCII.
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);   
                    // Send the message to the connected TcpServer. 
                    stream.Write(data, 0, data.Length);
                    Console.WriteLine("Sent: {0}", message);         
                    // Bytes Array to receive Server Response.
                    data = new Byte[256];
                    String response = String.Empty;
                    // Read the Tcp Server Response Bytes.
                    Int32 bytes = stream.Read(data, 0, data.Length);
                    response = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    Console.WriteLine("Received: {0}", response);      
                    Thread.Sleep(2000);   
                }
            }
            catch (IOException) {
                Console.WriteLine("Connection closed by the server.");
            }
            catch (Exception e) {
                Console.WriteLine("Exception: {0}", e);
            }
        }
    }
}
