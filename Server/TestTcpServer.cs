namespace Server
{
    using Core;

    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;


    /// <summary>
    /// A simple TCP server
    /// </summary>
    public class TestTcpServer
    {
        /// <summary>
        /// This server's IP address
        /// </summary>
        private readonly IPEndPoint _ipEndPoint;

        /// <summary>
        /// A serializer that is used to serializer/deserialize the network data
        /// </summary>
        private readonly ISerializer _serializer;

        /// <summary>
        /// The underlying server connection
        /// </summary>
        private TcpListener _server;

        /// <summary>
        /// A list of connected clients
        /// </summary>
        private readonly List<TcpClient> _connectedClients = new List<TcpClient>();

        /// <summary>
        /// A list of registered controller
        /// </summary>
        private readonly Dictionary<string, ControllerBase> _controllers = new Dictionary<string, ControllerBase>();

        /// <summary>
        /// An event that will be fired when a client is connected
        /// </summary>
        public event Action<TcpClient> ClientConnected;

        /// <summary>
        /// An event that will be fired when a client disconnected
        /// </summary>
        public event Action<TcpClient> ClientDisconnected;


        public TestTcpServer(IPEndPoint ipEndPoint, ISerializer serializer)
        {
            _ipEndPoint = ipEndPoint;
            _serializer = serializer;
        }


        /// <summary>
        /// Initialize server and start accepting clients
        /// </summary>
        public void InitializeServer()
        {
            // Instantiate the server
            _server = new TcpListener(_ipEndPoint);

            // Run the run
            _server.Start();

            // Start handling the clients
            RunServer();
        }

        /// <summary>
        /// Registers a controller
        /// </summary>
        /// <param name="controller"> The controller register </param>
        /// <returns></returns>
        public TestTcpServer AddController(ControllerBase controller)
        {
            // Add the controller to the list
            _controllers.Add(controller.GetType().Name, controller);

            return this;
        }


        /// <summary>
        /// Send a message to every connected client
        /// </summary>
        /// <typeparam name="T"> The type of the message </typeparam>
        /// <param name="eventName"> The name of the client-side event </param>
        /// <param name="message"> The message to send </param>
        public void SendToAllClients<T>(string eventName, T message)
        {
            SendToClients(eventName, message);
        }

        /// <summary>
        /// Send a message to every connected client
        /// </summary>
        /// <param name="eventName"> The name of the client-side event </param>
        public void SendToAllClients(string eventName)
        {
            SendToClients(eventName, null);
        }

        /// <summary>
        /// Sends a message to every connected client
        /// </summary>
        /// <param name="eventName"> The name of the event to call </param>
        /// <param name="message"> the message that the client will receive </param>
        private void SendToClients(string eventName, object message)
        {
            // The ServerEvent that will be sent to the client
            var serverEvent = new ServerEvent()
            {
                EventName = eventName,
            };

            // The theres a message to send
            if (message != null)
            {
                // Serialize the message
                serverEvent.Data = _serializer.Serialize(message);
                
                // Set the message type
                serverEvent.DataTypename = message.GetType().AssemblyQualifiedName;
            };

            // Serialize the ServerEvent
            var serializedMessage = _serializer.Serialize(serverEvent);

            // Send the serializedMessage to every client
            _connectedClients.ForEach(client =>
            client.Client.Send(serializedMessage));
        }

        /// <summary>
        /// Start hanlding client connections, and requests
        /// </summary>
        private void RunServer()
        {
            Task.Run(() =>
            {
                // COntiniously accept an incoming client
                while (true)
                {
                    TcpClient client = _server.AcceptTcpClient();

                    // Add the client to the list
                    _connectedClients.Add(client);

                    // Invoke the ClientConnected event
                    ClientConnected?.Invoke(client);

                    Task.Run(() =>
                    {
                        // Allocate some buffers for the client's request
                        byte[] buffer = new byte[1024];
                        List<byte> completeRequest = new List<byte>();

                        while (true)
                        {
                            // Wait until a request is received
                            int bytes = client.Client.Receive(buffer);

                            // If no bytes were received
                            if (bytes == 0)
                            {
                                // Disconnect the client 
                                HandleClientDisconnection(client);
                                return;
                            };

                            // Add the received data to the list buffer
                            for (int a = 0; a < bytes; a++)
                            {
                                completeRequest.Add(buffer[a]);
                            };

                            // Get the client's stream
                            NetworkStream networkStream = client.GetStream();

                            // Read the data as long's as theres data present
                            while (networkStream.DataAvailable == true)
                            {
                                int readBytes = networkStream.Read(buffer, 0, buffer.Length);

                                for (int a = 0; a < readBytes; a++)
                                {
                                    completeRequest.Add(buffer[a]);
                                };
                            };

                            // Deserialize the request 
                            NetworkMessage request = _serializer.Deserialize<NetworkMessage>(completeRequest.ToArray());

                            // Handle it
                            HandleRequest(request, client);

                            // Clear the request buffer
                            completeRequest.Clear();
                        };
                    });
                };
            });
        }

        /// <summary>
        /// Hanldes a client's request
        /// </summary>
        /// <param name="request"> The client's requets </param>
        /// <param name="client"> The client that sent the request </param>
        private void HandleRequest(NetworkMessage request, TcpClient client)
        {
            // Get the request's controller and action names from path
            string controllerName = request.PathSegments[0];
            string actionName = request.PathSegments[1];

            // Try to find the controller
            _controllers.TryGetValue(controllerName, out ControllerBase controller);

            // If no controller was found
            if (controller is null)
            {
                SendToClient(client, $"Request failed. \nNo such controller: {controllerName}");
                return;
            };

            // Try to find the action
            var action = controller.GetAction(actionName);

            // If no action was found
            if (action is null)
            {
                SendToClient(client, $"No such action: {actionName}");
                return;
            };

            // Request-argument validation
            if (request.RequestHasArguments == true &&
               action.ActionHasParameters == false)
            {
                SendToClient(client, $"Action {actionName} doesn't take an argument(s)");
                return;
            };

            if (request.RequestHasArguments == false &&
              action.ActionHasParameters == true)
            {
                SendToClient(client, $"Action {actionName} missing argument(s)");
                return;
            };


            // if the request doesn't contain arguments
            if (request.RequestHasArguments == false)
            {
                // Call the action
                ActionResult actionResult = action.Invoke();

                // Send the action result to the client
                SendToClient(client, actionResult.Data);
            }
            // if the request contains arguments
            else
            {
                // Get the action's paramters
                var actionParams = action.Parameters;

                // The action's parameter type
                var actionParamType = actionParams[0].ParameterType;

                // The type of the request 
                var objType = Type.GetType(request.MessageTypeName);

                // If the argument types match
                if (objType == actionParamType)
                {
                    // Serialize the request into the correct type
                    var obj = _serializer.Deserialize(request.Message, objType);

                    // Invoke the action and pass it the argument
                    ActionResult actionResult = action.Invoke<object>(obj);

                    // The the action result to the client
                    SendToClient(client, actionResult.Data);
                }
                else
                {
                    SendToClient(client, "Error Invalid argument type supplied");
                };
            }
        }

        /// <summary>
        /// Handle client disconnection
        /// </summary>
        /// <param name="client"> </param>
        private void HandleClientDisconnection(TcpClient client)
        {
            // Invoke the ClientDisconnected event
            ClientDisconnected?.Invoke(client);

            var clientStream = client.GetStream();

            // Close the connection and stream
            client.Close();
            clientStream.Close();

            // Remove the client from the list
            _connectedClients.Remove(client);
        }

        /// <summary>
        /// Send a message to a single client
        /// </summary>
        /// <typeparam name="T"> The type of the message </typeparam>
        /// <param name="client"> The client to send the message to </param>
        /// <param name="data"> The message to send to the client </param>
        private void SendToClient<T>(TcpClient client, T data)
        {
            // The NetworkMessage which will be sent to the client
            var networkMessage = new NetworkMessage()
            {
                Message = _serializer.Serialize(data),
            };

            // Send the message to the client
            client.Client.Send(_serializer.Serialize(networkMessage));
        }


    };
};