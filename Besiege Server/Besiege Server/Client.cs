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

        public Client(int _clientId)
        {
            // Initialize client id
            id = _clientId;

            // Initialize tcp instance
            tcp = new TCP(id);
        }

        public class TCP
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private byte[] receiveBuffer;

            public TCP(int _id)
            {
                // Initialize id
                id = _id;
            }

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

                // Initialize receive buffer to a byte array of size 4096
                receiveBuffer = new byte[dataBufferSize];

                // Begin reading from network stream
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

                // TODO: send welcome packet
                Console.WriteLine($"{socket.Client.RemoteEndPoint} has successfully connected!");
            }

            // Called when network stream read is complete
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

                    // Initialize data buffer to a byte array of size 4096
                    byte[] _data = new byte[_byteLength];

                    // Copy data from receive buffer to data buffer
                    Array.Copy(receiveBuffer, _data, _byteLength);

                    // TODO: handle data

                    // Begin reading from network stream again
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine("Error receibing TCP data: " + _ex);
                    // TODO: disconnect
                }
            }
        }
    }
}
