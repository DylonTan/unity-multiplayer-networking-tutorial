﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Besiege_Server
{
    class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string username = _packet.ReadString();

            Console.WriteLine($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}");
            if (_fromClient != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})");
            }
            //TODO: send player into game
        }

        public static void UDPTestReceived(int _fromClient, Packet _packet)
        {
            string _msg = _packet.ReadString();

            Console.WriteLine($"Received packet via UDP. Contains message: {_msg}");
        }
    }
}
