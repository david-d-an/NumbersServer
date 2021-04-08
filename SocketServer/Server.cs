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
    private const int NumberOfSifters = 1;
    private readonly string logFileName = "./Logs/result.log";
    private readonly string ip = "127.0.0.1";
    private readonly int port = 4000;

    private readonly int MaxItemCountInChunk = 50000;
    private readonly int ItemSize = 10;

    private readonly string _threadLock = "THREAD_LOCKER";
    private readonly string _counterLock = "COUNTER_LOCKER";
    private readonly string _queueLock = "QUEUE_LOCKER";

    // MaxThreadCount is the same as MaxConnectionCount
    private readonly int MaxThreadCount = 5;

    private TcpListener server = null;
    private int _threadCount = 0;
    private static bool _terminated = false;

    private StreamWriter logFile;
    private int _inputCount = 0;
    private int _inputCountSession = 0;
    private int _uniqueCount = 0;
    private int _uniqueCountSession = 0;
    private int _duplicateCount = 0;
    private int _duplicateCountSession = 0;


    private Queue<string> dataQueue; 

    // Use Hash set for average O(1) performance.
    private HashSet<string> uniqueValues = new HashSet<string>();

    #region ThreadCount
    private int GetThreadCount(object locker) =>
        Util.Monitor(locker, () => _threadCount);
    private void IncrementThreadCount(object locker, int i = 1) =>
        Util.Monitor(locker, () => _threadCount += i);
    private void DecrementThreadCount(object locker, int i = 1) =>
        Util.Monitor(locker, () => _threadCount -= i);
    #endregion

    private void ToggleTerminated(object locker, bool val) =>
        Util.Monitor(locker, () => _terminated = val);

    #region InputCount
    private int GetInputCount(object locker) =>
        Util.Monitor(locker, () => _inputCount);
    private void IncrementInputCount(object locker, int i = 1) =>
        Util.Monitor(locker, () => _inputCount += i);
    private int GetInputCountSession(object locker) =>
        Util.Monitor(locker, () => _inputCountSession);
    private void IncrementInputCountSession(object locker, int i = 1) =>
        Util.Monitor(locker, () => _inputCountSession += i);
    #endregion

    #region UniqueCount
    private int GetUniqueCount(object locker) =>
        Util.Monitor(locker, () => _uniqueCount);
    private void IncrementUniquCount(object locker, int i = 1) =>
        Util.Monitor(locker, () => _uniqueCount += i);
    private int GetUniqueCountSession(object locker) =>
        Util.Monitor(locker, () => _uniqueCountSession);
    private void IncrementUniquCountSession(object locker, int i = 1) =>
        Util.Monitor(locker, () => _uniqueCountSession += i);
    #endregion

    #region DuplicateCount
    private int GetDuplicateCount(object locker) =>
        Util.Monitor(locker, () => _duplicateCount);
    private void IncrementDuplicateCount(object locker, int i = 1) =>
        Util.Monitor(locker, () => _duplicateCount += i);
    private int GetDuplicateCountSession(object locker) =>
        Util.Monitor(locker, () => _duplicateCountSession);
    private void IncrementDuplicateCountSession(object locker, int i = 1) =>
        Util.Monitor(locker, () => _duplicateCountSession += i);
    #endregion

    private KeyValuePair<string, int>[] FlushCounters(object locker) {
      Monitor.Enter(locker);

      _duplicateCount += _duplicateCountSession;
      _uniqueCount += _uniqueCountSession;
      _inputCount += _inputCountSession;

      var counters = new KeyValuePair<string, int>[] {
          new KeyValuePair<string, int>(
              "Duplicate", _duplicateCountSession),
          new KeyValuePair<string, int>(
              "Unique", _uniqueCountSession),
          new KeyValuePair<string, int>(
              "TotalUnique", _uniqueCount),
          new KeyValuePair<string, int>(
              "TotalSubmission", _inputCount)
      };

      _duplicateCountSession = 0;
      _uniqueCountSession = 0;
      _inputCountSession = 0;

      Monitor.PulseAll(locker);
      Monitor.Exit(locker);
      return counters;
    }

    public Server() {
      dataQueue = new Queue<string>();
      Directory.CreateDirectory("./Logs");
      logFile = new StreamWriter(logFileName) { AutoFlush = false };
      server = new TcpListener(IPAddress.Parse(ip), port);

      try {
        // Notifier pushes stats to Console every 10 seconds
        new Thread(() => Notify()) {
          IsBackground = true
        }.Start();

        // Sift is worker to process data
        for(int i = 0; i < NumberOfSifters; i++) {
          new Thread(() => Sift()) {
            IsBackground = true
          }.Start();
        }

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
        // Main thread standing by to accpet client by AcceptTcpClient()
        while(!server.Pending()) {
          // Ternmination initiated by a client
          if (_terminated) {
            Console.WriteLine("Server shutdown by termination request.");
            return;
          }
          Thread.Sleep(10);
        }
        TcpClient client = server.AcceptTcpClient();

        // Checking connection pool for availability
        StringBuilder connMsg = new StringBuilder("Connection Request received!\n");
        try {
          connMsg.Append(MaxThreadCount - GetThreadCount(_threadLock));
          connMsg.Append("/");
          connMsg.Append(MaxThreadCount.ToString());
          connMsg.Append(" Connections available\n");

          if (GetThreadCount(_threadLock) < MaxThreadCount) {
            Thread t = new Thread(
              new ParameterizedThreadStart(HandleClient));
            t.IsBackground = true;
            t.Start(client);
            IncrementThreadCount(_threadLock);
            connMsg.Append("Connection created.\n");
          } else {
            connMsg.Append("Connection refused.\n");
          }
          // Console.WriteLine(connMsg.ToString());
        } catch (Exception ex){
          // Exception is isolated one thread instance
          // Keep the server going
          Console.WriteLine(ex.Message);
        } finally {
          _threadLock.ExitMonitor();
        }
      }
    }

    /***************************************************************/
    /* The main creates mulitple thread of this function to handle */ 
    /* Client data submission                                      */
    /* This competes with main thread and other HadleClient thread */
    /***************************************************************/
    public void HandleClient(Object obj) {
      TcpClient client = (TcpClient)obj;
      string threadId = Thread.CurrentThread.ManagedThreadId
                        .ToString().PadLeft(2, '0');
      var stream = client.GetStream();
      stream.ReadTimeout = 2500;
      stream.WriteTimeout = 2500;

      // string data = null;
      Byte[] bytes = new Byte[MaxItemCountInChunk*ItemSize];
      int readCount;
      string ext = "";

      while (!_terminated) {
        ext = "";
        try {
          readCount = stream.Read(bytes, 0, bytes.Length);
          if (readCount == 0) {
            // Client disconnected
            // No need to keep the thread
            DecrementThreadCount(_threadLock);
            client.Close();
            return;
          }

          // Additional read if initially read part of client packet.
          // while (readCount < bytes.Length && bytes[readCount-1] != 10) {
          while (readCount < bytes.Length) {
            // Initial read was premature
            // Subsequent reads to fill up the entire array
            var tmpCount = stream.Read(bytes, readCount, bytes.Length-readCount);
            readCount += tmpCount;
          }
        } catch(IOException) {
          // ReadTimeout causes IOException
          // Client still connected. Keep thread.
          continue;
        } catch(Exception e) {
          Console.WriteLine(
            "(Thread {0}) Exception while reading stream: {1}", 
            threadId, e);
          continue;
        }

        string stringData = Encoding.ASCII.GetString(bytes, 0, readCount);
        var dataArray = stringData.Split("\n");
        try {
          foreach(var data in dataArray) {
            // Skip blank line
            if (data.Length == 0)
              continue;
            // terminate request detected
            else if (data == "terminate") {
              string terminationMsg =
                "Termination requested. Server is being shutdown.";
              Console.WriteLine(
                "(Thread {0}) Sent: {1}",
                threadId, terminationMsg);
              stream.SendMessage(terminationMsg);

              ToggleTerminated(_threadLock, true);
              DecrementThreadCount(_threadLock);
              client.Close();
              // Console.WriteLine(ConnectClosedMsg());
              return;
            }
            // invalid data detected
            else if (data.Length != 9 || !long.TryParse(data, out long x)) {
              string msg = 
                "Invalid value entered. Connection is being terminated.";
              Console.WriteLine(
                "(Thread {0}) Sent: {1}", 
                threadId, msg);
              Console.WriteLine(
                "DataLength: {0}, Data: {1}", data.Length, data);
              stream.SendMessage(msg);
              DecrementThreadCount(_threadLock);
              client.Close();
              // Console.WriteLine(ConnectClosedMsg());
              return;
            }

            // Normal data. 
            // Add to queue and Sift will handle the rest
            Monitor.Enter(_queueLock);
            dataQueue.Enqueue(data);
            Monitor.PulseAll(_queueLock);
            Monitor.Exit(_queueLock);
          }

          // Send Tx receipt to client
          // The client won't send next packet until this receipt is recieved
          bytes = new Byte[MaxItemCountInChunk * ItemSize];
          stream.SendMessage("Received " + stringData.Length + " bytes" + ext);
        } catch(IOException) {
          Console.WriteLine("Client connection is already terminated");
        } catch(Exception ex) {
          stream.SendMessage("Server error occurred");
          Console.WriteLine("Exception: {0}: {1}", ex.Message, ex.StackTrace);
          logFile.Close();
          // throw ex;
          return;
        } finally {
          _threadLock.ExitMonitor();
        }
      }

      // This part reached when request arrived while termination in progress
      string denialMsg = "Request deined. Server is being terminated.";
      Console.WriteLine("(Thread {0}) Sent: {1}", threadId, denialMsg);
      stream.SendMessage(denialMsg);
      DecrementThreadCount(_threadLock);
      client.Close();
      // Console.WriteLine(ConnectClosedMsg());
    }


    /****************************************************/
    /* Separated worker logics from HandleClient thread */
    /* This competes with Sift thread                   */
    /****************************************************/
    public void Notify() {
      while(!_terminated) {
        Thread.Sleep(10000);

        var counts = FlushCounters(_counterLock);
        int uc = counts.Where(k => k.Key == "Unique").FirstOrDefault().Value;
        int dc = counts.Where(k => k.Key == "Duplicate").FirstOrDefault().Value;
        int tu = counts.Where(k => k.Key == "TotalUnique").FirstOrDefault().Value;
        int ts = counts.Where(k => k.Key == "TotalSubmission").FirstOrDefault().Value;

        String msg = Environment.NewLine +
                    "Received at " + DateTime.Now.ToString("HH:mm:ss") +
                    Environment.NewLine +
                    "# of Unique since last report: " + uc + Environment.NewLine +
                    "# of Duplicates since last report: " + dc + Environment.NewLine +
                    "# Unique all time: " + tu + Environment.NewLine +
                    "# of Total submission: " + ts + Environment.NewLine;
        Console.WriteLine(msg);
      }
    }

    /****************************************************/
    /* Separated worker logics from HandleClient thread */
    /* This competes with Notify thread                 */
    /****************************************************/
    public void Sift() {
      while(!_terminated) {
        if (dataQueue.Count == 0)
          continue;

        var tmpQ = new Queue<string>();
        // Bulk unload from Queue to reduce overhead
        Monitor.Enter(_queueLock);
        while(dataQueue.Count > 0)
          tmpQ.Enqueue(dataQueue.Dequeue());
        Monitor.PulseAll(_queueLock);
        Monitor.Exit(_queueLock);

        foreach(var data in tmpQ) {
          IncrementInputCountSession(_counterLock);
          if (uniqueValues.AddToSet(_counterLock, data)) {
            IncrementUniquCountSession(_counterLock);
            logFile.WriteInLog(_counterLock, data);
          } else {
            IncrementDuplicateCountSession(_counterLock);
          }
        }
        logFile.Flush();
      }
    }
  }
}
