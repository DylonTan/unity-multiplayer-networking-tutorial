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
        private static UdpClient udpListener;
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        /// <summary>
        /// Starts the server
        /// </summary>
        /// <param name="_maxPlayers">The max player count</param>
        /// <param name="_port">The port</param>
        public static void Start(int _maxPlayers, int _port)
        {
            // Initialize max player count
            MaxPlayers = _maxPlayers;

            // Initialize port
            Port = _port;

            Console.WriteLine("Starting server...");

            // Initialize additional server data
            InitializeServerData();

            // Initialize tcp listener
            tcpListener = new TcpListener(IPAddress.Any, Port);

            // Start tcp listener
            tcpListener.Start();

            // Begin accepting tcp clients
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            // Initialize udp client
            udpListener = new UdpClient(Port);

            // Begin receiving udp datagrams
            udpListener.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine($"Server listening on port {Port}");
        }

        /// <summary>
        /// Called when a tcp client is trying to connect
        /// </summary>
        /// <param name="_result">The result of the asynchronous operation</param>
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

        private static void UDPReceiveCallback(IAsyncResult _result)
        {
            try
            {
                // Initialize client end point with temporary value
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

                // End receiving udp datagram and overwrites client end point with correct value
                byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);

                // Begin receiving udp datagrams again
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if (_data.Length < 4)
                {
                    // TODO: disconnect;
                    return;
                }

                using (Packet _packet = new Packet(_data))
                {
                    int _clientId = _packet.ReadInt();

                    if (_clientId == 0)
                    {
                        return;
                    }

                    if (clients[_clientId].udp.endPoint == null)
                    {
                        clients[_clientId].udp.Connect(_clientEndPoint);
                        return;
                    }

                    if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                    {
                        clients[_clientId].udp.HandleData(_packet);
                    }
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error receiving UDP data: {_ex}");
            }
        }

        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch (Exception _ex)
            {
                Console.Write($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
            }
        }

        /// <summary>
        /// Initializes server data
        /// </summary>
        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                { (int)ClientPackets.udpTestReceived, ServerHandle.UDPTestReceived }
            };
            Console.WriteLine("Initialized packet handlers");
        }
    }
}
