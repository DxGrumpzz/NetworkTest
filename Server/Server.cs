namespace Server
{
    using Core;

    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public class Server
    {
        public static void Main()
        {
            #region MyRegion

            /*

            Console.Title = "Server";
            Console.WriteLine("Server");

            IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
            const int port = 5500;

            ISerializer serializer = new Json_Serializer();

            List<TcpClient> clients = new List<TcpClient>();

            IPEndPoint ipEndPoint = new IPEndPoint(iPAddress, port);

            TcpListener server = new TcpListener(ipEndPoint);

            Console.WriteLine("Initializing server");

            server.Start();

            Console.WriteLine("Waiting for connections");

            Dictionary<string, object> controllers = new Dictionary<string, object>()
            {
                { nameof(Controller), new Controller() }
            };

            Task.Run(() =>
            {
                while (true)
                {

                    string[] command = Console.ReadLine().Split(' ');

                    if (command[0].ToLower() == "send")
                    {
                        string message = command[1];

                        clients.ForEach(client =>
                        client.Client.Send(serializer.Serialize(message)));

                        Console.WriteLine($"Send {message} to every client");
                    }
                    else if (command[0].ToUpper() == "CALL")
                    {
                        string message = command[1];

                        clients.ForEach(client =>
                        client.Client.Send(serializer.Serialize(message)));
                    };

                };
            });


            Task.Run(() =>
            {
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();

                    clients.Add(client);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Client connected {client.Client.RemoteEndPoint}");
                    Console.ResetColor();

                    Console.Title = $"Clients: {clients.Count}";

                    Task.Run(() =>
                    {
                        byte[] buffer = new byte[1024];

                        while (true)
                        {
                            List<byte> completeRequest = new List<byte>();

                            int bytes = client.Client.Receive(buffer);

                            if (bytes == 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Client disconnected");
                                Console.ResetColor();

                                clients.Remove(client);

                                Console.Title = $"Clients: {clients.Count}";
                                return;
                            };

                            for (int a = 0; a < bytes; a++)
                            {
                                completeRequest.Add(buffer[a]);
                            };


                            NetworkStream networkStream = client.GetStream();

                            while (networkStream.DataAvailable == true)
                            {
                                int readBytes = networkStream.Read(buffer, 0, buffer.Length);

                                for (int a = 0; a < readBytes; a++)
                                {
                                    completeRequest.Add(buffer[a]);
                                };
                            };


                            NetworkMessage request = serializer.Deserialize<NetworkMessage>(completeRequest.ToArray());


                            string controllerName = request.PathSegments[0];
                            string actionName = request.PathSegments[1];

                            controllers.TryGetValue(controllerName, out object controller);

                            if (controllerName is null)
                            {
                                client.Client.Send(serializer.Serialize($"Request failed. \nNo such controller: {controllerName}"));
                                continue;
                            };

                            bool requestHasArguments = request.Message != null ? true : false;

                            Type controllerType = controller.GetType();

                            var controllerActions = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod)
                            .Where(action => action.ReturnType == typeof(ActionResult) ||
                                             action.ReturnType.BaseType == typeof(ActionResult));

                            if (requestHasArguments == true)
                            {
                                var objType = Type.GetType(request.MessageTypeName);

                                var obj = serializer.Deserialize(request.Message, objType);


                                foreach (var action in controllerActions)
                                {
                                    var actionParams = action.GetParameters();
                                    bool actionHasParams = actionParams.Length > 0 ? true : false;

                                    if (actionHasParams == false)
                                        continue;

                                    if (action.Name == actionName)
                                    {
                                        var s1 = actionParams[0].ParameterType;

                                        if (objType == s1)
                                        {
                                            ActionResult actionResult = (ActionResult)action.Invoke(controller, new[] { obj });

                                            client.Client.Send(serializer.Serialize(new NetworkMessage()
                                            {
                                                Message = serializer.Serialize(actionResult.Result),
                                            }));
                                        }
                                    };
                                };
                            }
                            else
                            {
                                var objType = Type.GetType(request.MessageTypeName);

                                foreach (var action in controllerActions)
                                {
                                    var actionParams = action.GetParameters();
                                    bool actionHasParams = actionParams.Length > 0 ? true : false;

                                    if (actionHasParams == false)
                                        continue;

                                    if (action.Name == actionName)
                                    {
                                        var s1 = actionParams[0].ParameterType;

                                        if (objType == s1)
                                        {
                                            ActionResult actionResult = (ActionResult)action.Invoke(controller, null);
                                        }
                                    };
                                };
                            } 
                        };
                    });
                };
            });
            */

            #endregion


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
    };
};