<center> 
<h1>
.NET Numbers Server Code Challenge Instructions
</h1>
</center>

<br>

# Project Structure
The solution is made of three projects.
```
 {project_root}
        │
        ├── SocketApp.sln
        ├── Challenge Instructions.md
        ├── README.md
        │
        ├── DataGenerator
        │       │
        │       DataGenerator.csproj
        │
        ├── SocketClient
        │       │
        │       SocketClient.csproj
        │
        └── SocketServer
                │
                SocketServer.csproj
```

1. **SocketApp.sln**
    Solution file enclosing SocketClient and SocketServer projects
1. **Challenge Instructions.md**
    This file itself.
1. **README.md**
    The file has New Relic's requirements for this code challenge.
1. **DataGenerator**
    The project is to create a text-based data file that contains 9 digit random numbers to test the NumberServer system. 
    The name and size of data can be adjusted inside *program.cs* file.
1. **SocketClient**
    Client side application of NumberServer system
1. **SocketClient**
    Server side application of NumberServer system

<br><br>

# Usage Instructions

## 1. Socket Server
The application receives the stream of data from a NumberServer client application(s) with the following specifications:

1. Data transmission is through a TCP socket over port 4000.
1. Data is split by newline characters ('\')
1. Split data is stored if found unique. All duplicate data is ignored.
1. The number of unique and duplicate values are counted as part of statistics
1. Invalid data causes the connection to be terminated and the rest of the data is discarded
    * data is not a positive integer (leading 0's are permissible) 
    * data must be exactly 9 digits including leading 0's. (12345678 is invalid 012345678 is valid)
    * Server and other clients are NOT affected
1. Value "terminate" causes server shutdown. 
    * The Server starts shutting down,
    * All other client connections are severed by the server

<br>

### **These two values must match between Server.cs file and Client.cs file** <br>
```
private readonly int MaxItemCountInChunk = 50000;   // Number of items in a packeet (i.e. Number of lines)
private readonly int ItemSize = 10;                 // Data is expected to be 9 digits plus \n.
```

Execute below script to run the application

<pre>
> cd <i>{project_root}</i>/SocketServer
> dotnet build
> dotnet run
</pre>

Every 10 seconds, the application pushes a console message to the screen.
In the background, the application extracts series of 9 digit numbers from the data stream and writes them into a logfile located at:

<pre>
{project_root}/SocketServer/Log/result.log
</pre>

The server pushes a message to report the current statistics in the following format:

```
Received at 13:41:09                        // Time of report
# Unique since last report: 339306          // Number of unique values last 10 seconds
# Unique all time: 339306                   // Number of unique values since app start
# Duplicates since last report: 468972      // Number of duplicate values last 10 seconds
# Duplicates all time: 468972               // Number of duplicate values since app start
# Total submission: 808279                  // Number of all values since app start
```

<br>

## 2. Socket Client

The application reads data files from the drive. The location of files needs to be added in the program.cs file.
The data is saved in plain text format with the following specification:

1. Data pieces are separated by newline character (\n)
2. Clean data piece must be 9 digit integer.
    * Leading zeros are required to occupy the entire 9 spaces
    * blank lines with white spaces are ignored and have no effect
    * Data files must not have null characters (\0)
3. The last line doesn't have to end with newline as newline will be added during transmission.

> Note: The **Socket Client** app must be running to prevent application crashes.

<pre>
    static void Main(string[] args) {
        new Thread(() => new Client("./Data/a2-2M-A.txt", 0)).Start();      // Client ID: 0
        new Thread(() => new Client("./Data/a2-2M-B.txt", 1)).Start();      // Client ID: 1
        new Thread(() => new Client("./Data/a2-2M-A.txt", 2)).Start();      // Client ID: 2
        new Thread(() => new Client("./Data/a2-2M-B.txt", 3)).Start();      // Client ID: 3
    }
</pre>

The above example shows that the project is configured to read two files (a2-2M-A.txt and a2-2M-B.txt) twice each 
and transmit their datat o the server. <br>

### **These two values must match between Server.cs file and Client.cs file** <br>
```
private readonly int MaxItemCountInChunk = 50000;   // Number of items in a packeet (i.e. Number of lines)
private readonly int ItemSize = 10;                 // Data is expected to be 9 digits plus \n.
```


Once the configuration is done, execute the below script to run the application

<pre>
> cd {project_root}/SocketClient
> dotnet build
> dotnet run
</pre>

An example of a normal client console output is:

```
> dotnet run

(Client 0) File Name: ./Data/a1.txt
(Client 1) File Name: ./Data/a2.txt
(Client 2) File Name: ./Data/a3.txt
(Client 3) File Name: ./Data/a4.txt
(Client 4) File Name: ./Data/a5.txt
(Client 5) File Name: ./Data/a6.txt
(Client 6) File Name: ./Data/a7.txt
(Client 0) Received: Received 500000 bytes
(Client 5) Connection closed by the server.     // 5 connections max, Client 5 dropped
(Client 6) Connection closed by the server.     // 5 connections max, Client 6 dropped
(Client 1) Received: Received 500000 bytes
(Client 0) Received: Received 500000 bytes
(Client 1) Received: Received 500000 bytes
(Client 3) Received: Received 500000 bytes
(Client 4) Received: Received 500000 bytes
(Client 1) Received: Received 500000 bytes
(Client 2) Received: Received 500000 bytes
(Client 2) Received: Received 500000 bytes
(Client 3) Received: Received 500000 bytes
(Client 1) Connection closed by the server.     // Finished upload. Connection timed out
(Client 0) Received: Received 500000 bytes
(Client 3) Connection closed by the server.     // Finished upload. Connection timed out
(Client 2) Received: Received 500000 bytes
(Client 2) Connection closed by the server.     // Finished upload. Connection timed out
(Client 0) Connection closed by the server.     // Finished upload. Connection timed out
(Client 4) Received: Received 500000 bytes
(Client 4) Connection closed by the server.     // Finished upload. Connection timed out
```

* The max number of simultaneous connections is 5. Therefore, Client 5 and Client 6 were disconnected 
  before their first packets were transmitted. <br>

* The size of each packet is 50K bytes here. The client/server configuration is set to 5K items per packet 
  with 10 bytes per item.  This means if the packet has less than 5K bytes, it is still sent in a 50K byte packet 
  with null bytes filling the void.

<br><br>
