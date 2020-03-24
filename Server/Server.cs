namespace Server
{
    using Core;

    using System;
    using System.Net;

    public class Server
    {
        public static void Main()
        {
            //StartInsecure();
            StartSecure();
        }

        private static void StartInsecure()
        {
            Console.Title = "Server";
            Console.WriteLine("Server");


            IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
            const int port = 5500;


            TestTcpServer server = new TestTcpServer(
                new IPEndPoint(iPAddress, port),
                new Json_Serializer());

            server
            .AddController(new Controller())
            .AddController(new Controller2());


            server.ClientConnected += (client) =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Client {client.Client.RemoteEndPoint} connected");
                Console.ResetColor();
            };

            server.ClientDisconnected += (client) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Client {client.Client.RemoteEndPoint} disconnected");
                Console.ResetColor();
            };

            server.InitializeServer();


            while (true)
            {
                string[] command = Console.ReadLine().Split(' ');

                if (command[0].ToUpper() == "CALL")
                {
                    string eventName = command[1];

                    if (command.Length > 2)
                    {
                        string message = command[2];

                        server.SendToAllClients(eventName, message);
                    }
                    else
                    {
                        server.SendToAllClients(eventName);
                    };
                };

            };
        }

        private static void StartSecure()
        {
            Console.Title = "Server";
            Console.WriteLine("Server");


            IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
            const int port = 5500;


            TestTcpServer server = new TestTcpServer(
                new IPEndPoint(iPAddress, port),
                new Json_Serializer());

            server
            .AddController(new Controller())
            .AddController(new Controller2());


            server.ClientConnected += (client) =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Client {client.Client.RemoteEndPoint} connected");
                Console.ResetColor();
            };

            server.ClientDisconnected += (client) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Client {client.Client.RemoteEndPoint} disconnected");
                Console.ResetColor();
            };

            server.InitializeServerSecure();


            while (true)
            {
                string[] command = Console.ReadLine().Split(' ');

                if (command[0].ToUpper() == "CALL")
                {
                    string eventName = command[1];

                    if (command.Length > 2)
                    {
                        string message = command[2];

                        server.SendToAllClientsSecure(eventName, message);
                    }
                    else
                    {
                        server.SendToAllClientsSecure(eventName);
                    };
                };

            };
        }
    };
};