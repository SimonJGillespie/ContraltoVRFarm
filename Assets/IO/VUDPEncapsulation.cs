﻿/*  
    This file is part of ContrAlto.

    ContrAlto is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    ContrAlto is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with ContrAlto.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Net;
using System.Net.Sockets;

using Contralto.Logging;
using System.Threading;
using System.Net.NetworkInformation;
using VNetwork;

namespace Contralto.IO
{    
    /// <summary>
    /// Implements the logic for encapsulating a 3mbit ethernet packet into/out of UDP datagrams.
    /// Sent packets are broadcast to the subnet.        
    /// </summary>
    public class VUDPEncapsulation : IPacketEncapsulation
    {
        public VUDPEncapsulation(string interfaceName, byte address)
        {         
            // Try to set up UDP client.
            Random rand = new Random();
            byte[] IPAddress = new byte[4];
            IPAddress[3] = address;
            IPAddress[2] = 1;
            IPAddress[1] = 168;
            IPAddress[0] = 192;
            _thisIPAddress = new IPAddress(IPAddress);
            _udpClient = new VUdpClient(_thisIPAddress);
            _broadcastEndpoint = new IPEndPoint(_thisIPAddress, _udpPort);


        }        

        public void RegisterReceiveCallback(ReceivePacketDelegate callback)
        {   

            // Set up input
            _callback = callback;
            BeginReceive();
        }

        public void Shutdown()
        {
            // Shut down the reciever thread.

            if (_receiveThread != null)
            {
                _receiveThread.Abort();
            }

        }


        /// <summary>
        /// Sends an array of bytes over the ethernet as a 3mbit packet encapsulated in a 10mbit packet.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="hostId"></param>
        public void Send(ushort[] packet, int length)
        {


            // Sanity check.
            if (length < 1)
            {
                throw new InvalidOperationException("Raw packet data must contain at least two bytes for addressing.");
            }

            //
            // Outgoing packet contains 1 extra word (2 bytes) containing
            // the prepended packet length (one word)            
            byte[] packetBytes = new byte[length * 2 + 2];

            //
            // First two bytes include the length of the 3mbit packet; since 10mbit packets have a minimum length of 46 
            // bytes, and 3mbit packets have no minimum length this is necessary so the receiver can pull out the 
            // correct amount of data.
            //            
            packetBytes[0] = (byte)((length) >> 8);
            packetBytes[1] = (byte)(length);

            //
            // Do this annoying dance to stuff the ushorts into bytes because this is C#.
            //
            for (int i = 0; i < length; i++)
            {                
                packetBytes[i * 2 + 2] = (byte)(packet[i] >> 8);
                packetBytes[i * 2 + 3] = (byte)(packet[i]);
            }

            Log.Write(LogType.Verbose, LogComponent.HostNetworkInterface, "Sending packet via UDP; source {0} destination {1}, length {2} words.",
                Conversion.ToOctal(packetBytes[3]),
                Conversion.ToOctal(packetBytes[2]),
                length);

            _udpClient.Send(packetBytes, packetBytes.Length, _broadcastEndpoint);            
        }

        /// <summary>
        /// Begin receiving packets, forever.
        /// </summary>

        void BeginReceive()
        {
            // Kick off receive thread.   
            _receiveThread = new Thread(ReceiveThread);
            _receiveThread.Start();
        }

        private void ReceiveThread()
        {
            Log.Write(LogComponent.HostNetworkInterface, "UDP Receiver thread started.");
            
            IPEndPoint groupEndPoint = new IPEndPoint(IPAddress.Any, _udpPort);            

            while (true)
            {                
                byte[] data = _udpClient.Receive(ref groupEndPoint);
                
                //
                // Sanitize the data (at least make sure the length is valid):
                //
                if (data.Length < 4)
                {
                    Log.Write(LogType.Verbose, LogComponent.HostNetworkInterface, "Invalid packet: Packet is fewer than 2 words long, dropping.");
                    continue;
                }                

                // Drop our own UDP packets.
                if (!groupEndPoint.Address.Equals(_thisIPAddress))
                {
                    Log.Write(LogComponent.HostNetworkInterface, "Received UDP-encapsulated 3mbit packet.");
                    _callback(new System.IO.MemoryStream(data));
                }
            }
        }

        
        private IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }

            return new IPAddress(broadcastAddress);
        } 


        // Callback delegate for received data.
        private ReceivePacketDelegate _callback;

        // Thread used for receive
        private Thread _receiveThread;

        public VUdpClient _udpClient;

        // UDP port (TODO: make configurable?)
        private const int _udpPort = 42424;
        //private VRUDP _udpClient;
        private IPEndPoint _broadcastEndpoint;

        // The IP address (unicast address) of the interface we're using to send UDP datagrams.
        private IPAddress _thisIPAddress;
    }   
}
