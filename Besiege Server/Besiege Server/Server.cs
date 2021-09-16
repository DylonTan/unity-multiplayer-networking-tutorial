using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Besiege_Server
{
    class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        private static TcpListener tcpListener;

        public static void Start(int _maxPlayers, int _port)
        {
            // Initialize max player count
            MaxPlayers = _maxPlayers;

            // Initialize port
            Port = _port;

            Console.WriteLine("Starting server...");

            // Initialize additional server data (dictionary of clients)
            InitializeServerData();

            // Initialize tcp listener
            tcpListener = new TcpListener(IPAddress.Any, Port);

            // Start tcp listener
            tcpListener.Start();

            // Begin accepting tcp clients
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            Console.WriteLine($"Server listening on port {Port}");
        }

        // Called when tcp client is trying to connect
        private static void TCPConnectCallback(IAsyncResult _result)
        {
            // End accepting tcp clients and get tcp client instance
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);

            // Begin accepting tcp clients again
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            Console.WriteLine($"Incoming connection from {_client.Client.RemoteEndPoint}...");

            // Loop through dictionary of clients
            for (int i = 1; i <= MaxPlayers; i++)
            {
                // Check if current client instance is empty
                if (clients[i].tcp.socket == null)
                {
                    // Connect to server with client instance
                    clients[i].tcp.Connect(_client);
                    return;
                }
            }

            Console.WriteLine($"{_client.Client.RemoteEndPoint} failed to connect: Server full!");
        }

        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }
        }
    }
}
