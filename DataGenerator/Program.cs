using System;
using System.IO;

namespace DataGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            int dataSize = 2000000;
            string fileName = "a2-2M.txt";

            StreamWriter dataFile = new StreamWriter(fileName);
            Random rnd = new Random((int)DateTime.Now.Ticks);
            for (int i = 0; i < dataSize; i++) {
                // var tmp = Guid.NewGuid().ToString();
                // dataFile.WriteLine(tmp.Substring(tmp.Length - 4));

                int n  = rnd.Next(0, 999999999);
                dataFile.WriteLine(n.ToString().PadLeft(9, '0'));
            }
            dataFile.Flush();
        }
    }
}
