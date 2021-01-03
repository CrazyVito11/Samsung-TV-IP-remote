using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Samsung_TV_IP_remote
{
    class Program
    {
        private static TCPClient client;
        static void Main(string[] args)
        {
            Console.WriteLine("{0} V{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("-------------------------------------------\n");

            //Connect("10.70.0.68", 55000);

            client = new TCPClient();

            client.OnConnecting   += OnConnecting;
            client.OnConnect      += OnConnect;
            client.OnDataReceived += DataReceived;

            client.Connect("10.70.0.68", 55000);
           
            Console.ReadLine();
        }

        static void OnConnecting(string remoteIp, int port)
        {
            Console.Write("Connecting to {0}...", remoteIp);
        }

        static void OnConnect(string remoteIp, int port)
        {
            Console.WriteLine(" OK");
            Console.Write("Authenticating...");

            client.SendImmediate(authenticateHeader());
        }

        static void DataReceived(byte[] data, int bytesRead)
        {
            Console.WriteLine(Encoding.ASCII.GetString(data, 0, bytesRead));
            Console.WriteLine(byteArrayToPrintedHex(data, bytesRead));
        }

        public static byte[] authenticateHeader()
        {
            byte[] headerBeginning = { 0x00, 0x13, 0x00 };
            byte[] appName         = Encoding.ASCII.GetBytes("iphone.iapp.samsung");

            byte[] payloadHeader = { 0x64, 0x00 };
            byte[] machineIp     = Base64EncodeWithLength(getMachineIP());
            byte[] uniqueID      = Base64EncodeWithLength("00:00:00:00:00");
            byte[] name          = Base64EncodeWithLength(Environment.MachineName);
            byte[] payload       = CombineByteArray(payloadHeader, CombineByteArray(CombineByteArray(machineIp, uniqueID), name));
            byte[] payloadSize   = getFirstTwoBytes(BitConverter.GetBytes(payload.Length));

            byte[] authenticationPackage = CombineByteArray(headerBeginning, CombineByteArray(appName, CombineByteArray(payloadSize, payload)));
            return authenticationPackage;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.ASCII.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
        public static byte[] Base64EncodeWithLength(string plainText)
        {
            string base64Data = Base64Encode(plainText);
            byte[] dataSize   = getFirstTwoBytes(BitConverter.GetBytes(base64Data.Length));

            return CombineByteArray(dataSize, Encoding.ASCII.GetBytes(base64Data));
        }

        public static byte[] getFirstTwoBytes(byte[] originalByteArray)
        {
            byte[] newByteArray = { originalByteArray[0], originalByteArray[1] };

            return newByteArray;
        }

        private static string byteArrayToPrintedHex(byte[] data, int length = 0)
        {
            string builder        = "";
            string bitdata        = BitConverter.ToString(data);
            int counter           = 0;
            Array bitdataExploded = bitdata.Split('-');

            foreach (String item in bitdataExploded)
            {
                if (length != 0 && counter > length) break;

                builder += "0x" + item + " ";
                counter++;
            }

            return builder;
        }

        private static byte[] CombineByteArray(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        private static string getMachineIP()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address.ToString();
        }
    }
}
