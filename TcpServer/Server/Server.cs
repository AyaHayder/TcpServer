using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerData;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Server
{
    class Server
    {
        static List<ClientData> _clients;
        static Socket listenerSocket;
        static void Main(string[] args) {
            Console.WriteLine("Starting server on " + Packet.getIp4Address());
            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clients = new List<ClientData>();
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(Packet.getIp4Address()), 4242);
            listenerSocket.Bind(ip);
            Thread listenThread = new Thread(ListenThread);
            listenThread.Start();
        }
        static void ListenThread()
        {
            Socket clientSocket;
            ClientData clientData;

            for (;;)
            {
                listenerSocket.Listen(0);

                clientSocket = listenerSocket.Accept();
                clientData = new ClientData(clientSocket);
                clientData.Start();
                clientData.SendRegistrationPacket();

                _clients.Add(clientData);
            }
        }
        public static void Data_IN(object cSocket)
        {
            Socket clientSocket = (Socket)cSocket;
            byte[] buffer;
            int readBytes;
            Packet packet = null;
            for (;;)
            {
                try
                {
                    buffer = new byte[clientSocket.SendBufferSize];
                    readBytes = clientSocket.Receive(buffer);
                    if (readBytes > 0)
                    {
                        packet = new Packet(buffer);
                        DataManager(packet);
                    }
                }
                catch (SocketException)
                {
                    if (packet != null)
                    {
                        var client = _clients.FirstOrDefault(c => c.id == (string)packet.Gdata[1]);
                        _clients.Remove(client);
                        Console.WriteLine("A client is disconnected");
                    }

                    break;
                }
                catch (Exception) {
                }
                
            }
        }

        public static void DataManager(Packet p) {
            switch (p.packetType)
            {
                case packetType.Registration:
                    Console.WriteLine("A client is connected");
                    break;

                case packetType.Chat:
                    List<ClientData> _disconnectedClients = new List<ClientData>();

                    foreach(ClientData c in _clients)
                    {
                        try
                        {
                            if (c.clientSocket.Connected)
                            {
                                c.clientSocket.Send(p.ToBytes());
                            }
                            else
                            {
                                _disconnectedClients.Add(c);
                            }
                        }
                        catch (SocketException) {
                            _disconnectedClients.Add(c);
                        }
                            
                    }

                    foreach (ClientData c in _disconnectedClients)
                    {
                        _clients.Remove(c);
                    }
                    _disconnectedClients.Clear();
                    break;
            }
        }

    }

    class ClientData
    {
        public Socket clientSocket;
        public Thread clientThread;
        public string id;

        public ClientData() {
            id = Guid.NewGuid().ToString();
            clientThread = new Thread(Server.Data_IN);
            clientThread.Start(clientSocket);
            SendRegistrationPacket();
        }
        public ClientData(Socket clientSocket) {
            this.clientSocket = clientSocket;
            id = Guid.NewGuid().ToString();
            //clientThread = new Thread(Server.Data_IN);
            //clientThread.Start(clientSocket);
            //SendRegistrationPacket();
        }

        public void Start()
        {
            clientThread = new Thread(Server.Data_IN);
            clientThread.Start(clientSocket);
        }

        public void SendRegistrationPacket()
        {
            Packet p = new Packet(packetType.Registration,1);
            p.Gdata.Add(id);
            clientSocket.Send(p.ToBytes());
        }

    }
}
