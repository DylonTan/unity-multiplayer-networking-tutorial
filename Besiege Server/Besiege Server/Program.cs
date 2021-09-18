using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Besiege_Server
{
    class Program
    {
        private static bool isRunning = false;

        static void Main(string[] args)
        {
            // Set console title
            Console.Title = "Besiege Server";
            isRunning = true;

            // Create a new thread
            Thread mainThread = new Thread(new ThreadStart(MainThread));

            // Start the thread
            mainThread.Start();

            // Start server on port 26950 with a max player count of 10
            Server.Start(10, 26950);
        }

        // Called when thread gets started
        private static void MainThread()
        {
            Console.Write($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second");
            DateTime _nextLoop = DateTime.Now;

            while (isRunning)
            {
                while (_nextLoop < DateTime.Now)
                {
                    GameLogic.Update();

                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK);

                    if (_nextLoop > DateTime.Now)
                    {
                        // Pause the thread between ticks to reduce cpu usage
                        Thread.Sleep(_nextLoop - DateTime.Now);
                    }
                }
            }
        }
    }
}
