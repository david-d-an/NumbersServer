using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace SocketClient
{
    class Client{
        private string ip = "127.0.0.1";
        private int port = 4000;
        private string threadId;

        public Client() {
            // Thread ID = last 4 digits of GUID
            threadId = Guid.NewGuid().ToString();
            threadId = threadId.Substring(threadId.Length - 4);

            Console.Write("(Client {0}) File Name: ", threadId);
            string fileName = Console.ReadLine();
            Connect(ip, fileName);
        }

        public Client(int id, string fileName) {
            threadId = id.ToString();

            Console.WriteLine("(Client {0}) File Name: {1}", threadId, fileName);
            Connect(ip, fileName);
        }

        void Connect(String server, string fileName) {
            try {
                StreamReader file = new StreamReader(fileName);
                string message;
                TcpClient client = new TcpClient(server, port);
                NetworkStream stream = client.GetStream();
                stream.ReadTimeout = 5000;

                while ((message = file.ReadLine()) != null) {
                    // var message = Console.ReadLine();
                    if (message.Length == 0)
                        continue;

                    // Translate the Message into ASCII.
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);   
                    // Send the message to the connected TcpServer. 
                    stream.Write(data, 0, data.Length);
                    // Console.WriteLine("(Client {0}) Sent: {1}", threadId, message);         

                    // Bytes Array to receive Server Response.
                    data = new Byte[256];
                    String response = String.Empty;

                    // Read the Tcp Server Response Bytes.
                    Int32 bytes = stream.Read(data, 0, data.Length);
                    response = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                    // Console.WriteLine("(Client {0}) Received: {1}", threadId, response);
                    // Intentional delay for testing
                    Thread.Sleep(2000);
                }
                client.Close();
                stream.Close();
            }
            catch (IOException) {
                Console.WriteLine("(Client {0}) Connection closed by the server.", threadId);
            }
            catch (Exception e) {
                Console.WriteLine("(Client {0}) Exception: {1}", threadId, e);
            }
            finally {
            }
        }

    }
}
