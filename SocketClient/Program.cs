using System.Threading;

namespace SocketClient
{

    class Program {
        static void Main(string[] args) {

            // new Thread(() => new Client("./Data/a100K-Invalid.txt", 0)).Start();
            // for(int i = 1; i < 5; i++) {
            //     new Thread(() => new Client("./Data/a400K.txt", i)).Start();
            // }

            new Thread(() => new Client("./Data/a2M.txt", 0)).Start();
       }
    }
}
