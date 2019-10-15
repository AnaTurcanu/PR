
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    class Program
    {
        private static ClientSocket socket = new ClientSocket();
        static void Main(string[] args)
        {
            Console.Title = "Client";
            socket.Connect("127.0.0.1", 9001);
            Console.WriteLine("/q - exit");

            string command = "";
            while (true)
            {
                command = Console.ReadLine();
                if (command.ToLower() == "/q")
                {
                    return;
                }
                else if (String.IsNullOrWhiteSpace(command))
                {
                    continue;
                }

                var message = Message.CreateMessage(command);

                socket.Send(message);
            }
        }
    }
    class ClientSocket
    {
        private Socket clientSocket;
        private byte[] buffer;
        private const int BUFF_SIZE = 1024;
        private string ip;
        private int port;

        public ClientSocket()
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Connect(string ipAddres, int port)
        {
            ip = ipAddres;
            this.port = port;
            clientSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ipAddres), port), ConnectCallback, null);
        }

        private void Reconnect()
        {
            clientSocket.Close();
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), port), ConnectCallback, null);
        }

        public void Send(byte[] data)
        {
            try
            {
                clientSocket.Send(data);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not Send data, {e.Message}");
            }
        }

        private void ConnectCallback(IAsyncResult result)
        {
            if (clientSocket.Connected)
            {
                try
                {
                    Console.Write("Connection estabilished\n>");
                    buffer = new byte[BUFF_SIZE];
                    clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Can't estabilish a connection, {e.Message}");
                }
            }
            else
            {
                Console.WriteLine("Client Socket could not connect, trying to reconnect in 5 seconds");
                Thread.Sleep(5000);
                this.Reconnect();
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int buffLength = clientSocket.EndReceive(result);
                byte[] packet = new byte[buffLength];
                Array.Copy(buffer, 2, packet, 0, packet.Length);
                string res = Encoding.Default.GetString(packet);
                Console.Write(res + "\n>");

                buffer = new byte[BUFF_SIZE];
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Can't receive data from server {e.Message}");
                Console.WriteLine("Trying to recconect in 5 seconds");
                Thread.Sleep(5000);
                this.Reconnect();
            }
        }

    }
    class Message
    {
        public static byte[] CreateMessage(string msg)
        {
            byte[] packet = new byte[msg.Length + 2];
            byte[] packetLength = BitConverter.GetBytes((ushort)msg.Length);
            Array.Copy(packetLength, packet, 2);
            Array.Copy(Encoding.ASCII.GetBytes(msg), 0, packet, 2, msg.Length);

            return packet;
        }
    }

}