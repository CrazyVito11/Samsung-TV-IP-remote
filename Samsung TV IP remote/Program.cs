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
        static void Main(string[] args)
        {
            Console.WriteLine("{0} V{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("-------------------------------------------\n");
            Connect("10.70.0.68", 55000);
            Console.ReadLine();
        }

        static void Connect(String server, Int32 port)
        {
            try
            {
                // Create a TcpClient
                Console.WriteLine("Connecting to {0}...", server);
                TcpClient client = new TcpClient(server, port);

                // Translate the passed message into ASCII and store it as a Byte array.
                //Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                Byte[] data = authenticateHeader();

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);

                Console.WriteLine("Sent authentication data");

                // Receive the TcpServer.response.

                // Buffer to store the response bytes.
                data = new Byte[256];

                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);

                // Close everything.
                stream.Close();
                client.Close();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }

        public static Byte[] authenticateHeader()
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

        private static string byteArrayToPrintedHex(byte[] data)
        {
            string builder = "";
            string bitdata = BitConverter.ToString(data);
            Array bitdataExploded = bitdata.Split('-');

            foreach(String item in bitdataExploded)
            {
                builder += "0x" + item + " ";
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
