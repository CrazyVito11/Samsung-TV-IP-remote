using System;
using System.Net.Sockets;
using System.Threading;

namespace Samsung_TV_IP_remote
{
    public delegate void OnConnected(string remoteIp, int port);
    public delegate void OnConnecting(string remoteIp, int port);
    public delegate void OnDataReceived(byte[] data, int bytesRead); 
    public delegate void OnDisconnect();
    public delegate void OnError(TcpClient client);

    class TCPClient
    {
        public event OnConnected    OnConnected;
        public event OnConnecting   OnConnecting;
        public event OnDataReceived OnDataReceived;
        public event OnDisconnect   OnDisconnect;
        public event OnError        OnError;

        private TcpClient     tcpClient;
        private NetworkStream clientStream;
        private int           CurrentWriteByteCount;
        private byte[]        WriteBuffer;
        private byte[]        ReadBuffer;
        private int           port;
        private bool          started = false;

        public TCPClient()
        {
            WriteBuffer           = new byte[1024];
            ReadBuffer            = new byte[1024];
            CurrentWriteByteCount = 0;
        }

        /// <summary>
        /// Initiates a TCP connection to a TCP server with a given address and port
        /// </summary>
        /// <param name="ipAddress">The IP address (IPV4) of the server</param>
        /// <param name="port">The port the server is listening on</param>
        public void Connect(string ipAddress, int port)
        {
            this.port = port;

            OnConnecting(ipAddress, port);
            tcpClient = new TcpClient(ipAddress, port);
            clientStream = tcpClient.GetStream();

            OnConnected(ipAddress, port);

            Thread t = new Thread(new ThreadStart(ListenForPackets));
            started = true;
            t.Start();
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            if (tcpClient == null)
            {
                return;
            }

            if (started)
            {
                OnDisconnect();
            }

            tcpClient.Close();

            started = false;
        }

        /// <summary>
        /// Packet listener that runs on a seperate thread and raises an event when data is received
        /// </summary>
        private void ListenForPackets()
        {
            int bytesRead;

            while (started)
            {
                bytesRead = 0;

                try
                {
                    //Blocks until a message is received from the server
                    bytesRead = clientStream.Read(ReadBuffer, 0, ReadBuffer.Length);
                }
                catch
                {
                    //A socket error has occurred
                    Console.WriteLine("A socket error has occurred with the client socket " + tcpClient.ToString());
                    OnError(tcpClient);
                    break;
                }

                if (bytesRead == 0)
                {
                    break;
                }

                if (OnDataReceived != null)
                {
                    OnDataReceived(ReadBuffer, bytesRead);
                }

                Thread.Sleep(15);
            }

            started = false;
            Disconnect();
        }

        /// <summary>
        /// Adds data to the packet to be sent out, but does not send it across the network
        /// </summary>
        /// <param name="data">The data to be sent</param>
        public void AddToPacket(byte[] data)
        {
            if (CurrentWriteByteCount + data.Length > WriteBuffer.Length)
            {
                FlushData();
            }

            Array.ConstrainedCopy(data, 0, WriteBuffer, CurrentWriteByteCount, data.Length);
            CurrentWriteByteCount += data.Length;
        }

        /// <summary>
        /// Flushes all outgoing data to the server
        /// </summary>
        public void FlushData()
        {
            clientStream.Write(WriteBuffer, 0, CurrentWriteByteCount);
            clientStream.Flush();
            CurrentWriteByteCount = 0;
        }

        /// <summary>
        /// Sends the byte array data immediately to the server
        /// </summary>
        /// <param name="data"></param>
        public void SendImmediate(byte[] data)
        {
            AddToPacket(data);
            FlushData();
        }

        /// <summary>
        /// Tells whether we're connected to the server
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return started && tcpClient.Connected;
        }

        public string GetIp()
        {
            return tcpClient.Client.RemoteEndPoint.ToString();
        }
    }
}
