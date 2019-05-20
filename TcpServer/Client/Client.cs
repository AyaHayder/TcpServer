using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerData;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Client
{
    class Client
    {
        public static string name;
        public static string password;
        public static Socket master;
        public static Int32 id =0;
        static void Main(string[] args)
        {
            Console.Write("Enter host IP address: ");
            String ip = Console.ReadLine();
            Console.Clear();
            Console.Write("Enter your name: ");
            name = Console.ReadLine();
            Console.Write("Enter Password: ");
            password = Console.ReadLine();
            master = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ip), 4242);
            Console.ForegroundColor = ConsoleColor.Cyan;
            try
            {
                master.Connect(ipep);
                SendRegistrationPacket();
                LoginData();
                if(id > 0)
                {
                    Thread t = new Thread(DataIn);
                    t.Start();
                    for (;;)
                    {
                        string input = Console.ReadLine();
                        Packet p = new Packet(packetType.Chat, id);
                        p.Gdata.Add(name);
                        p.Gdata.Add(input);
                        master.Send(p.ToBytes());
                    }
                }
                else
                {
                    Console.WriteLine("Wrong username or password!");
                    Console.WriteLine("Press any key to continue ..");
                    Console.ReadLine();
                }

                
            }

            catch
            {
                Console.WriteLine("Could not connect to host");
            }
        }
        public static void LoginData()
        {
            Byte[] buffer;
            int readBytes;
                try
                {
                    buffer = new Byte[master.SendBufferSize];
                    readBytes = master.Receive(buffer);
                    if (readBytes > 0)
                        {
                            DataManager(new Packet(buffer));
                        }
                }
                catch (SocketException)
                {
                    Console.WriteLine("The server has disconnected!");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
        }

        public static void DataIn()
        {
            Byte[] buffer;
            int readBytes;
            for (;;)
            {
                try
                {
                    buffer = new Byte[master.SendBufferSize];
                    readBytes = master.Receive(buffer);
                    if (readBytes > 0)
                    {
                        DataManager(new Packet(buffer));
                    }
                }
                catch(SocketException)
                {
                    Console.WriteLine("The server has disconnected!");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            }
        }

        static void DataManager(Packet p)
        {
            switch (p.packetType)
            {
                case packetType.Registration:
                    id = p.senderId;
                    foreach (String friendName in p.Gdata)
                        Console.WriteLine(friendName);
                    break;
                case packetType.Chat:
                    Console.WriteLine(p.Gdata[0] + ": " + p.Gdata[1]);
                    break;
            }
        }
        public static void SendRegistrationPacket()
        {
            Packet p = new Packet(packetType.Registration, id);
            p.Gdata.Add(id);
            p.Gdata.Add(name);
            p.Gdata.Add(password);
            master.Send(p.ToBytes());
        }

    }
}
