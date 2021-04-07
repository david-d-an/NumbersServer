using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketClient
{
    class Client{
        private string ip = "127.0.0.1";
        private int port = 4000;
        private string threadId;
        private readonly int MaxItemCountInChunk = 3266;
        // private readonly int ItemSize = 10;


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

                long dataCount = 0;
                StringBuilder chunk = new StringBuilder();
                while (true)
                {
                    message = file.ReadLine();
                    // EOF
                    if (message == null) {
                        // Console.WriteLine("Last Trnsmit called: {0}", dataCount);
                        // Console.WriteLine("{0}", chunk);
                        Transmit(chunk.ToString(), stream);
                        break;
                    }

                    // var message = Console.ReadLine();
                    if (message.Length == 0)
                        continue;

                    chunk.Append(message).Append("\n");
                    dataCount++;

                    if (dataCount % MaxItemCountInChunk == 0) {
                        // Console.WriteLine("Trnsmit called: {0}", dataCount);
                        // Console.WriteLine("{0}", chunk);
                        Transmit(chunk.ToString(), stream);
                        chunk = new StringBuilder();
                    }
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

        private void Transmit(string message, NetworkStream stream)
        {
            // Translate the Message into ASCII.
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            // Send the message to the connected TcpServer. 
            stream.Write(data, 0, data.Length);
            // Console.WriteLine("(Client {0}) Sent: {1}", threadId, message);         

            // Bytes Array to receive Server Response.
            // Read the Tcp Server Response Bytes.
            Byte[] responseData = new Byte[256];
            Int32 byteCount = stream.Read(responseData, 0, responseData.Length);
            string response = System.Text.Encoding.ASCII.GetString(responseData, 0, byteCount);

            Console.WriteLine("(Client {0}) Received: {1}", threadId, response);
            // Intentional delay for testing
            // Thread.Sleep(2000);
        }
    }
}
