using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Besiege_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // Set console title
            Console.Title = "Besiege Server";

            // Start server on port 26950 with a max player count of 10
            Server.Start(10, 26950);
            Console.ReadKey();
        }
    }
}
