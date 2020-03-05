using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;

namespace VNetwork
{

    public class VUdpClient
    {
        static int clients = 150;
        static bool locking;
        static int[][] queueindexes;
        static byte[][] buffer;
        public static byte[][] packetb;
        static int buffercount = 20;
        static int buffersize = 1000;
        static int[] bufferlength;
        static IPEndPoint[] bufferendpoint;
        //public static IPAddress ouraddress;
        static int bufferindex;
        static int[] stackptr;
        // Use this for initialization
        public static byte[] nullpacket;
        public IPAddress ouraddress;

        static VUdpClient()
        {
            bufferindex = 0;
            locking = false;
            nullpacket = new byte[0];
            queueindexes = new int[clients][];
            for (int i = 0; i < clients; i++)
            {
                queueindexes[i] = new int[buffercount];
            }

            buffer = new byte[buffercount][];
            for (int b = 0; b < buffercount; b++)
            {
                buffer[b] = new byte[buffersize];
            }
            bufferendpoint = new IPEndPoint[buffercount];
            bufferlength = new int[buffercount];
            packetb = new byte[clients][];
            stackptr = new int[clients];

        }

        public VUdpClient(IPAddress clientaddress)
        {
            ouraddress = clientaddress;
        }



        void Backoff()
        {
            System.Random rand = new System.Random();
            int next = (int)rand.NextDouble() * 1000;
            Thread.Sleep(next);
            //for (int i = 0; i < next; i++) ;
        }

        public new int Send(byte[] packet, int Length, IPEndPoint _broadcastEndpoint)
        {
            if (locking)
                while (locking)
                    Backoff();

            locking = true;
            Debug.Log("Packet sent");
            if (bufferindex == buffercount) bufferindex = 0;
            for (int c = 0; c < clients; c++)
            {
                //if (stackptr[c] == buffercount) dropone(c);
                if (stackptr[c] == buffercount) stackptr[c] = 0;
                // Add a new buffer in the ring and add 
                queueindexes[c][stackptr[c]] = bufferindex;
                stackptr[c]++;
            }
            bufferendpoint[bufferindex] = new IPEndPoint(_broadcastEndpoint.Address, _broadcastEndpoint.Port);
            //buffer[bufferindex] = new byte[Length];
            Array.Copy(packet, buffer[bufferindex], packet.Length);
            /*for (int i = 0; i < packet.Length; i++)
            {
                buffer[bufferindex][i] = packet[i];
            }*/
            bufferlength[bufferindex] = packet.Length;
            bufferindex++;


            locking = false;
            return (0);
        }

        void dropone(int c)
        {

            for (int i = 1; i < stackptr[c]; i++)
            {
                queueindexes[i - 1] = queueindexes[i];
            }
            stackptr[c]--;

        }



        public new byte[] Receive(ref IPEndPoint groupEndPoint)
        {
            int c = (int)ouraddress.GetAddressBytes()[3];
            //if (stackptr[c] == 0 || bufferendpoint[queueindexes[0][c]].Address.Equals(ouraddress)) return new byte[0];
            // Every client has their own queue so we can remove our own packets
            if (stackptr[c] == 0) return nullpacket;
            if (locking)
                while (locking)
                    Backoff();
            locking = true;
            //Debug.Log("Packet received");
            packetb[c] = new byte[bufferlength[queueindexes[c][0]]];
            groupEndPoint.Address = bufferendpoint[queueindexes[c][0]].Address;
            groupEndPoint.Port = bufferendpoint[queueindexes[c][0]].Port;
            Array.Copy(buffer[queueindexes[c][0]], packetb[c], bufferlength[queueindexes[c][0]]);
            /*for (int i = 0; i < bufferlength[queueindexes[0][c]]; i++)
            {
                packetb[c][i] = buffer[queueindexes[0][c]][i];
            }*/
            dropone(c);
            locking = false;
            return packetb[c];
        }

    }
}