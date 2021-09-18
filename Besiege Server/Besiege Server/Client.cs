using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Besiege_Server
{
    class Client
    {
        public static int dataBufferSize = 4096;

        public int id;
        public TCP tcp;
        public UDP udp;

        /// <summary>
        ///  Creates a new client instance with a given client id
        /// </summary>
        /// <param name="_clientId">The client id</param>
        public Client(int _clientId)
        {
            // Initialize client id
            id = _clientId;

            // Initialize tcp instance
            tcp = new TCP(id);

            // Initialize udp instance
            udp = new UDP(id);
        }

        public class TCP
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            /// <summary>
            /// Creates a new tcp instance with a given id
            /// </summary>
            /// <param name="_id">The id</param>
            public TCP(int _id)
            {
                // Initialize id
                id = _id;
            }

            /// <summary>
            /// Connects a tcp client to the server
            /// </summary>
            /// <param name="_socket">The tcp client</param>
            public void Connect(TcpClient _socket)
            {
                // Initialize tcp client
                socket = _socket;

                // Set receive buffer size to 4096
                socket.ReceiveBufferSize = dataBufferSize;
                
                // Set send buffer size to 4096
                socket.SendBufferSize = dataBufferSize;

                // Get network stream
                stream = socket.GetStream();

                // Initialize received data packet instance
                receivedData = new Packet();

                // Initialize receive buffer to a byte array of size 4096
                receiveBuffer = new byte[dataBufferSize];

                // Begin reading from network stream
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

                // Send welcome packet
                ServerSend.Welcome(id, "Welcome to the server!");

                Console.WriteLine($"{socket.Client.RemoteEndPoint} has successfully connected!");
            }

            public void SendData(Packet _packet)
            {
                try
                {
                    // Check if socket is initialized
                    if (socket != null)
                    {
                        // Begin writing packet byte array to network stream
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    }
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {_ex}");
                    //TODO: disconnect
                }
            }

            /// <summary>
            /// Called when network stream read is complete
            /// </summary>
            /// <param name="_result">The result of the asynchronous operation</param>
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
                            Server.packetHandlers[_packetId](id, _packet);
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
            public IPEndPoint endPoint;

            private int id;

            public UDP(int _id)
            {
                id = _id;
            }

            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
                ServerSend.UDPTest(id);
            }

            public void SendData(Packet _packet)
            {
                Server.SendUDPData(endPoint, _packet);
            }

            public void HandleData(Packet _packetData)
            {
                int _packetLength = _packetData.ReadInt();
                byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });
            }
        }
    }
}
