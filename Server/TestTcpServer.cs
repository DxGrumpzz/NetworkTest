﻿namespace Server
{
    using Core;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Threading.Tasks;

    public class TestTcpServer
    {
        private readonly IPEndPoint _ipEndPoint;
        private readonly ISerializer _serializer;

        private TcpListener _server;

        private List<TcpClient> _connectedClients = new List<TcpClient>();

        private Dictionary<string, object> _controllers = new Dictionary<string, object>();


        public TestTcpServer(IPEndPoint ipEndPoint, ISerializer serializer)
        {
            _ipEndPoint = ipEndPoint;
            _serializer = serializer;
        }


        public void InitializeServer()
        {
            _server = new TcpListener(_ipEndPoint);

            _server.Start();

            RunServer();
        }

        public TestTcpServer AddController(object controller)
        {
            _controllers.Add(controller.GetType().Name, controller);

            return this;
        }


        public void SendToAllClients(string message)
        {
            var serializedMessage = _serializer.Serialize(message);

            _connectedClients.ForEach(client =>
            client.Client.Send(serializedMessage));
        }


        private void RunServer()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    TcpClient client = _server.AcceptTcpClient();

                    _connectedClients.Add(client);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Client connected {client.Client.RemoteEndPoint}");
                    Console.ResetColor();

                    Console.Title = $"_connectedClients: {_connectedClients.Count}";

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

                                _connectedClients.Remove(client);

                                Console.Title = $"_connectedClients: {_connectedClients.Count}";
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


                            NetworkMessage request = _serializer.Deserialize<NetworkMessage>(completeRequest.ToArray());


                            string controllerName = request.PathSegments[0];
                            string actionName = request.PathSegments[1];

                            _controllers.TryGetValue(controllerName, out object controller);

                            if (controllerName is null)
                            {
                                client.Client.Send(_serializer.Serialize($"Request failed. \nNo such controller: {controllerName}"));
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

                                var obj = _serializer.Deserialize(request.Message, objType);


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

                                            client.Client.Send(_serializer.Serialize(new NetworkMessage()
                                            {
                                                Message = _serializer.Serialize(actionResult.Result),
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
        }

    }

};