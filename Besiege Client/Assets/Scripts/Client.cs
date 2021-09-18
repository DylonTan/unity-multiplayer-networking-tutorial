using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class Client : MonoBehaviour
{
    public static Client Instance;
    public static int dataBufferSize = 4096;

    public string ip = "127.0.0.1";
    public int port = 26950;
    public int myId = 0;
    public TCP tcp;
    public UDP udp;

    private delegate void PacketHandler(Packet _packet);
    private static Dictionary<int, PacketHandler> packetHandlers;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    private void Start()
    {
        tcp = new TCP();
        udp = new UDP();
    }

    public void ConnectToServer()
    {
        InitializeClientData();
        tcp.Connect();
    }

    public class TCP
    {
        public TcpClient socket;

        public NetworkStream stream;
        private Packet receivedData;
        public byte[] receiveBuffer;

        public void Connect()
        {
            // Initialize new tcp client instance and set receive/send buffer sizes;=
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            // Initialize receive buffer
            receiveBuffer = new byte[dataBufferSize];

            // Begin connecting to server
            socket.BeginConnect(Instance.ip, Instance.port, ConnectCallback, socket);
        }

        private void ConnectCallback(IAsyncResult _result)
        {
            // End connecting to server
            socket.EndConnect(_result);

            // Check if socket is not connected to server
            if (!socket.Connected)
            {
                return;
            }

            // Get network stream
            stream = socket.GetStream();

            // Initialize received data packet instance
            receivedData = new Packet();

            // Begin reading from network stream
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via TCP: {_ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                // End reading from network stream and get data byte length
                int _byteLength = stream.EndRead(_result);

                // Check if byte length is less than or equal to 0
                if (_byteLength <= 0)
                {
                    // TODO: disconnect
                    return;
                }

                // Initialize data buffer to a byte array
                byte[] _data = new byte[_byteLength];

                // Copy data from receive buffer to data buffer
                Array.Copy(receiveBuffer, _data, _byteLength);

                // Reset received data packet instance depending on result of handling data
                receivedData.Reset(HandleData(_data));

                // Begin reading from network stream again
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception _ex)
            {
                Console.WriteLine("Error receiving TCP data: " + _ex);
                // TODO: disconnect
            }
        }

        private bool HandleData(byte[] _data)
        {
            // Initialize packet length
            int _packetLength = 0;

            // Append bytes of data provided to received data packet instance
            receivedData.SetBytes(_data);

            // Check if unread length of received data packet instance is more than or equal to 4 (size of int)
            if (receivedData.UnreadLength() >= 4)
            {
                // Get packet content's length by reading int of received data packet instance
                _packetLength = receivedData.ReadInt();

                // Check if packet content's length is less than or equal to 0
                if (_packetLength <= 0)
                {
                    return true;
                }
            }

            // Loop as long as packet length is more than 0 and less than or equal to unread length of received data packet instance
            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                // Get packet content by reading bytes of size packet content's length
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);

                // Execute on main thread
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        // Get packet id by reading int of packet instance
                        int _packetId = _packet.ReadInt();

                        // Call appropriate packet handler
                        packetHandlers[_packetId](_packet);
                    }
                });

                // Reset packet length
                _packetLength = 0;

                // Check if unread length of received data packet instance is more than or equal to 4 (size of int)
                if (receivedData.UnreadLength() >= 4)
                {
                    // Get packet content's length by reading int of received data packet instance
                    _packetLength = receivedData.ReadInt();

                    // Check if packet content's length is less than or equal to 0
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (_packetLength <= 1)
            {
                return true;
            }

            return false;
        }
    }

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(Instance.ip), Instance.port);
        }

        public void Connect(int _localPort)
        {
            socket = new UdpClient(_localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using (Packet _packet = new Packet())
            {
                SendData(_packet);
            }
        }

        public void SendData(Packet _packet)
        {
            try
            {
                _packet.InsertInt(Instance.myId);
                if (socket != null)
                {
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            }
            catch(Exception _ex)
            {
                Debug.Log($"Error sending data to server via UDP: {_ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                byte[] _data = socket.EndReceive(_result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if (_data.Length < 4)
                {
                    // TODO: disconnect
                    return;
                }

                HandleData(_data);
            }
            catch
            {
                // TODO: disconnect
            }
        }

        private void HandleData(byte[] _data)
        {
            using (Packet _packet = new Packet(_data))
            {
                int _packetLength = _packet.ReadInt();
                _data = _packet.ReadBytes(_packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_data))
                    {
                        int _packetId = _packet.ReadInt();
                        packetHandlers[_packetId](_packet);
                    }
                });
            };
        }
    }

    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.udpTest, ClientHandle.UDPTest }
        };
        Debug.Log("Initialized packet handlers");
    }
}
