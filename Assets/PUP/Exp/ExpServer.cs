/*  
    This file is part of IFS.

    IFS is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    IFS is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with IFS.  If not, see <http://www.gnu.org/licenses/>.
*/

using IFS.BSP;
using IFS.Logging;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace IFS.Exp
{

    public class ExpWorker : BSPWorkerBase
    {
        public ExpWorker(BSPChannel channel) : base(channel)
        {
            // Register for channel events
            channel.OnDestroy += OnChannelDestroyed;

            _running = true;

            _workerThread = new Thread(new ThreadStart(ExpWorkerThreadInit));
            _workerThread.Start();
        }

        public override void Terminate()
        {
            Logging.Log.Write(LogType.Error, LogComponent.Exp, "Terminate");

            ShutdownWorker();
        }

        private void OnChannelDestroyed(BSPChannel channel)
        {
            Logging.Log.Write(LogType.Error, LogComponent.Exp, "OnChannelDestroyed");

            ShutdownWorker();
        }

        private void ExpWorkerThreadInit()
        {
            //
            // Run the worker thread.
            // If anything goes wrong, log the exception and tear down the BSP connection.
            //
            try
            {
                ExpWorkerThread1();
            }
            catch (Exception e)
            {
                if (!(e is ThreadAbortException))
                {
                    Logging.Log.Write(LogType.Error, LogComponent.Exp, "Exp worker thread terminated with exception '{0}'.", e.Message);
                    Channel.SendAbort("Server encountered an error.");

                    OnExit(this);
                }
            }
        }

        private void ExpWorkerThread()
        {
            // TODO: enforce state (i.e. reject out-of-order block types.)
            byte[] data = new byte[1];
            string result = "\r\n";
            IPHostEntry hostEntry = Dns.GetHostEntry(Configuration.TelnetHost);
            IPAddress address = hostEntry.AddressList[0];

            string request = "";
            Byte[] bytesSent = Encoding.ASCII.GetBytes(request);
            Byte[] bytesReceived = new Byte[1000];
            IPEndPoint ipe = new IPEndPoint(address, 23);
            Socket s = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(ipe);
            if (s == null)
            {
                Log.Write(LogComponent.Exp, "connect failed");
                Channel.Destroy();
                return;
            }
            s.Send(bytesSent, bytesSent.Length, 0);
            int bytes = 0;
            bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
            string datas = Encoding.ASCII.GetString(bytesReceived, 0, bytes);
            Log.Write(LogType.Verbose, LogComponent.Exp, "Received {0}", data);
            Channel.Send(Encoding.ASCII.GetBytes(datas));
            while (_running)
            {
                int length = Channel.Read(ref data, 1);
                Log.Write(LogComponent.Exp, " data ", data[0]);
                if (length < 1)
                {
                    int mark = Channel.LastMark;
                    // See FtpTelnet.bcpl
                    if (mark == 1)
                    {
                        Log.Write(LogComponent.Exp, "Got Sync mark {0}", mark);
                    }
                    else if (mark == 2)
                    {
                        int lineWidth = Channel.ReadByte();
                        Log.Write(LogComponent.Exp, "Got LineWidth {0}", lineWidth);
                        ExpWorkerThread1();
                    }
                    else if (mark == 3)
                    {
                        int pageLength = Channel.ReadByte();
                        Log.Write(LogComponent.Exp, "Got page length mark {0}", pageLength);
                    }
                    else if (mark == 4)
                    {
                        int terminalType = Channel.ReadByte();
                        Log.Write(LogComponent.Exp, "Got terminal type mark {0}", terminalType);
                    }
                    else if (mark == 5)
                    {
                        Log.Write(LogComponent.Exp, "Got timing mark {0}", mark);
                        Channel.SendMark(6, true /* ack */); // Timing reply mark
                    }
                    else if (mark == 6)
                    {
                        Log.Write(LogComponent.Exp, "Got timing reply mark {0}", mark);
                    }
                    else
                    {
                        Log.Write(LogComponent.Exp, "Unexpected mark {0}", mark);

                    }
                }
                else
                {
                    Log.Write(LogComponent.Exp, "Got char {0}", data[0]);
                    result += Convert.ToChar(data[0]);
                    if (data[0] == 13)
                    {
                        Log.Write(LogComponent.Exp, "Got line {0}", result);
                        string reply = "You sent: " + result;
                        Channel.Send(Encoding.ASCII.GetBytes(reply));
                        bytesSent = Encoding.ASCII.GetBytes(reply);
                        s.Send(bytesSent, bytesSent.Length, 0);
                        result = "";
                        /*int bytea = 0;
                        try
                        {
                            bytea = s.Receive(bytesReceived, bytesReceived.Length, 0);
                        }
                        catch { }
                        datas = Encoding.ASCII.GetString(bytesReceived, 0, bytes);*/
                        //Channel.Send(Encoding.ASCII.GetBytes(datas));
                    }
                }

            }
            s.Close();

        }

        private void DoSocket()
        {
            IPHostEntry hostEntry = Dns.GetHostEntry("anjinserver");
            IPAddress address = hostEntry.AddressList[0];

            string request = "";
            Byte[] bytesSent = Encoding.ASCII.GetBytes(request);
            Byte[] bytesReceived = new Byte[1000];
            IPEndPoint ipe = new IPEndPoint(address, 23);
            Socket s = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(ipe);
            if (s == null)
            {
                Log.Write(LogComponent.Exp, "connect failed");
                Channel.Destroy();
                return;
            }
            s.Send(bytesSent, bytesSent.Length, 0);
            int bytes = 0;
            bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
            string data = Encoding.ASCII.GetString(bytesReceived, 0, bytes);

            Log.Write(LogType.Verbose, LogComponent.Exp, "Received {0}", data);
            Channel.Send(Encoding.ASCII.GetBytes(data));
        }


        public void ExpWorkerThread1()
        {
            byte[] optresponse = { 255, 252, 24, 255, 252, 32, 255, 252, 35, 255, 252, 39 };
            byte[] optresponse1 = { 255, 252, 1, 255, 252, 33, 255, 252, 3, 255, 252, 31 };
            byte[] optresponse2 = { 255, 252, 1 };
            //byte[] optresponse1 = { 255, 250, 24, 0, "DEC-VT100", 255, 250, 32, 0, "9600,9600", 255, 250, 35, 0, 42, 255, 250, 39, 0, 42 };
            string request = "";
            string result = "";
            IPHostEntry hostEntry = Dns.GetHostEntry("anjinserver");
            IPAddress address = hostEntry.AddressList[0];
            IPEndPoint ipe = new IPEndPoint(address, 23);
            Socket s = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(ipe);
            if (s == null)
            {
                Console.Write("connect failed");
                return;
            }
            // Read options
            Byte[] bytesReceived = new Byte[1000];
            int bytes = 0;
            if (s.Connected == false) s.Connect(ipe);
            bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
            if (bytes != 0)
            {
                result = Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                //Console.Write("Received {0}", result);
            }

            //write options
            byte[] data = new byte[1];
            //if (s.Connected == false) s.Connect(ipe);

            s.Send(optresponse, optresponse.Length, 0);

            // 2d options phas

            // Read options
            bytesReceived = new Byte[1000];
            bytes = 0;
            //if (s.Connected == false) s.Connect(ipe);
            bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
            if (bytes != 0)
            {
                result = Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                //Console.Write("Received {0}", result);
            }

            //write options
            data = new byte[1];
            if (s.Connected == false) s.Connect(ipe);

            s.Send(optresponse1, optresponse1.Length, 0);

            bytesReceived = new Byte[1000];
            bytes = 0;
            //if (s.Connected == false) s.Connect(ipe);
            bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);

            //write options
            data = new byte[1];
            if (s.Connected == false) s.Connect(ipe);

            s.Send(optresponse2, optresponse2.Length, 0);

            /*bytesReceived = new Byte[1000];
            bytes = 0;
            //if (s.Connected == false) s.Connect(ipe);
            bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);*/


            byte[] bytesSent= new byte[1000];
            while (true)
            {



                bytesReceived = new Byte[1000];
                bytes = 0;
                //if (s.Connected == false) s.Connect(ipe); 
                bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
                if (bytes != 0)
                {
                    result = Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    string datas = Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    Console.Write("OUT->ALTO:{0}", result);
                    Channel.Send(Encoding.ASCII.GetBytes(datas));
                }

                request = "";
                string keystring ="";
                do
                {
                    bytes = Channel.Read(ref bytesSent, 1);
                    keystring = Encoding.ASCII.GetString(bytesSent, 0, bytes);
                    request += Encoding.ASCII.GetString(bytesSent, 0, bytes);
                } while (!keystring.Equals("\r"));
                //result = Encoding.ASCII.GetString(bytesSent, 0, bytes);
                //Console.Write("IN<-ALTO:{0}", result);
                //if (s.Connected == false) s.Connect(ipe);
                //request = Console.ReadLine();
                //request = request + "\r\n";
                bytesSent = Encoding.ASCII.GetBytes(request);

                s.Send(bytesSent, bytesSent.Length, 0);

               /* bytesReceived = new Byte[1000];
                bytes = 0;
                //if (s.Connected == false) s.Connect(ipe); 
                bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
                if (bytes != 0)
                {
                    result = Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    Channel.Send(bytesReceived,bytes,false);
                    Console.Write("{ " +
                        "" +
                        "0}", result);
                }

                //request = Console.ReadLine();
                request = "\n";
                bytesSent = Encoding.ASCII.GetBytes(request);
                //if (s.Connected == false) s.Connect(ipe);

                s.Send(bytesSent, bytesSent.Length, 0);*/

            }
        }

        private void ShutdownWorker()
        {
            // Tell the thread to exit and give it a short period to do so...
            _running = false;

            Log.Write(LogType.Verbose, LogComponent.Exp, "Asking Exp worker thread to exit...");
            _workerThread.Join(1000);

            if (_workerThread.IsAlive)
            {
                Logging.Log.Write(LogType.Verbose, LogComponent.Exp, "Exp worker thread did not exit, terminating.");
                _workerThread.Abort();

                if (OnExit != null)
                {
                    OnExit(this);
                }
            }
        }




        private Thread _workerThread;
        private bool _running;

    }
}
