using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketClient
{
    class Client{
        private string _ip = "127.0.0.1";
        private int _port = 4000;
        private string _threadId;
        private readonly int MaxItemCountInChunk = 50000;
        private readonly int ItemSize = 10;

        public Client() {
            // Thread ID = last 4 digits of GUID
            _threadId = Thread.CurrentThread.ManagedThreadId.ToString();

            Console.Write("(Client {0}) File Name: ", _threadId);
            string fileName = Console.ReadLine();
            Connect(_ip, fileName);
        }

        public Client(string fileName, int? id = null) {
            if (id == null) {
                _threadId = Thread.CurrentThread.ManagedThreadId.ToString();
            } else {
                _threadId = id.ToString();
            }

            Console.WriteLine("(Client {0}) File Name: {1}", _threadId, fileName);
            Connect(_ip, fileName);
        }

        void Connect(String server, string fileName) {
            TcpClient client = new TcpClient(server, _port);
            NetworkStream netStream = client.GetStream();
            netStream.ReadTimeout = 2500;
            netStream.WriteTimeout = 2500;

            try {
                StreamReader fileStream = new StreamReader(fileName);
                string message;

                long dataCount = 0;
                StringBuilder chunk = new StringBuilder();
                while (true)
                {
                    message = fileStream.ReadLine();

                    // EOF: Final transmission
                    if (message == null) {
                        Transmit(chunk.ToString(), netStream);
                        break;
                    }

                    // Skip balnk line
                    if (message.Length == 0)
                        continue;

                    // Buiild a message packe 
                    chunk.Append(message).Append("\n");
                    dataCount++;

                    // Message packet built complete. Tx immediately 
                    if (dataCount % MaxItemCountInChunk == 0) {
                        Transmit(chunk.ToString(), netStream);
                        chunk = new StringBuilder();
                    }
                }

                // Wait for server termination
                // Client should not terminate as that causes broken pipe server side
                while(true) {
                    Thread.Sleep(20);
                    CheckMessage(netStream);
                }
            }
            catch (IOException) {
                Console.WriteLine("(Client {0}) Connection closed by the server.", _threadId);
            }
            catch (Exception e) {
                Console.WriteLine("(Client {0}) Exception: {1}", _threadId, e);
            }
            finally {
                client.Close();
                netStream.Close();
            }
        }

        private void Transmit(string message, NetworkStream stream) {
            // Translate the Message into ASCII.
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            // Data are always sent in a byte[MaxItemCountInChunk * ItemSize]
            Array.Resize(ref data, MaxItemCountInChunk * ItemSize);

            stream.Write(data, 0, data.Length);
            // Console.WriteLine("(Client {0}) Sent: {1}", threadId, message);         

            // Read the Tcp Server Response Bytes.
            Byte[] responseData = new Byte[256];
            Int32 byteCount = stream.Read(responseData, 0, responseData.Length);
            string response = System.Text.Encoding.ASCII.GetString(responseData, 0, byteCount);

            Console.WriteLine("(Client {0}) Received: {1}", _threadId, response);
        }

        private void CheckMessage(NetworkStream stream) {
            // Read the Tcp Server Response Bytes.
            Byte[] responseData = new Byte[256];
            Int32 byteCount = stream.Read(responseData, 0, responseData.Length);
            if (byteCount > 0) {
                string response = System.Text.Encoding.ASCII.GetString(responseData, 0, byteCount);
                Console.WriteLine("(Client {0}) Server Message: {1}", _threadId, response);
            }
        }
    }
}
