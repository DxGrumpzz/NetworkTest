namespace Client
{
    using Core;

    using System;
    using System.Net;
    using System.Threading;

    public class Client
    {
        public static void Main()
        {
            Console.WriteLine("Press enter to connec");
            Console.ReadLine();

            IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
            const int port = 5500;

            IPEndPoint ipEndPoint = new IPEndPoint(iPAddress, port);

            TestTcpClient client = new TestTcpClient(ipEndPoint, new Json_Serializer());

            client.AddReceivedEvent("Event1", () =>
            {
                Console.WriteLine("Event1 was called");
            })
            .AddReceivedEvent("Event2", (string message) =>
            {
                Console.WriteLine($"Event2 was called. \nReceived: {message}");
            });


            client.InitializeConnectionSecure("LocalHost");


            while (true)
            {
                string[] command = Console.ReadLine().Split(' ');

                if (command[0].ToUpper() == "CLOSE")
                {
                    client.Close();
                }
                else if (command[0].ToUpper() == "SEND")
                {
                    if (command.Length < 2)
                        continue;

                    string path = command[1];

                    string message = null;
                    if (command.Length > 2)
                    {
                        message = command[2];

                        NetworkMessage s = client.SendSecure(
                            path: path,
                            obj: new TestClass()
                            {
                                Text = message,
                                Number = int.MaxValue,
                                Bool = true,
                                Enumerable = new[] { 1, 2, 4, 8, 16 }
                            });

                        Console.WriteLine($"Received {s.MessageAs<string>()}");
                    }
                    else
                    {

                        NetworkMessage s = client.SendSecure<TestClass>(path: path, null);
                        Console.WriteLine($"Received {s.MessageAs<string>()}");
                    }


                }
            };

        }
    };
};
