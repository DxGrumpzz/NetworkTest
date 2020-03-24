namespace Client
{
    using Core;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A simple TCP client. Provides some extra functionality for network communication
    /// </summary>
    public class TestTcpClient
    {

        #region Private fields

        /// <summary>
        /// A boolean flag that indicates if the client should wait for server-received events
        /// </summary>
        private bool _handleReceivedEvents = true;

        /// <summary>
        /// The underlying client 
        /// </summary>
        private TcpClient _client;

        /// <summary>
        /// The address endpoint of the server
        /// </summary>
        private IPEndPoint _endPoint;

        /// <summary>
        /// The type of _serializer
        /// </summary>
        private ISerializer _serializer;

        private SerializerType _serializerType;

        /// <summary>
        /// A stream that provides a secure way to transfer data between hosts
        /// </summary>
        //private SslStream _secureStream;

        /// <summary>
        /// A dictionary that contains registered client side events
        /// </summary>
        private Dictionary<string, Action> _receivedEvents = new Dictionary<string, Action>();

        /// <summary>
        /// A dictionary that contains registered client side events that can take an argument
        /// </summary>
        private Dictionary<string, Action<object>> _receivedEventsArgs = new Dictionary<string, Action<object>>();


        #endregion


        /// <summary>
        /// Constructor that can take a custom made _serializer
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="_serializer"></param>
        /// <param name="addressFamily"></param>
        public TestTcpClient(IPEndPoint endPoint, ISerializer serializer, AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            _client = new TcpClient(addressFamily);
            _serializer = serializer;

            _endPoint = endPoint;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="serializerType"></param>
        /// <param name="addressFamily"></param>
        public TestTcpClient(IPEndPoint endPoint, SerializerType serializerType = SerializerType.Json, AddressFamily addressFamily = AddressFamily.InterNetwork) :
            this(endPoint, null, addressFamily)
        {
            if (serializerType == SerializerType.Json)
            {
                _serializer = new Json_Serializer();
                _serializerType = SerializerType.Json;
            }
            else if (serializerType == SerializerType.Xml)
            {
                _serializer = new Xml_Serializer();
                _serializerType = SerializerType.Xml;
            }
            else if (serializerType == SerializerType.Binary)
            {
                _serializerType = SerializerType.Binary;
            }
            else if (serializerType == SerializerType.Custom)
            {
                _serializerType = SerializerType.Custom;
            }
        }


        #region Public methods

        /// <summary>
        /// Initializes a connection between the client and the server
        /// </summary>
        public void InitializeConnection()
        {
            // Connect to the server
            _client.Connect(_endPoint);

            // Setup server-event handler
            InitializeEventHandler();
        }

        SslStream _secureStream;

        public void InitializeConnectionSecure(string targetAuthenticationName)
        {
            _client.Connect(_endPoint);
            var networkStream = _client.GetStream();

            // Create a secure connection between the client and server
            _secureStream = new SslStream(
                networkStream,
                false,
                new RemoteCertificateValidationCallback(ValidateServerCertificate),
                null);

            // Try to authenticate
            try
            {
                _secureStream.AuthenticateAsClient(targetAuthenticationName);
            }
            catch (AuthenticationException e)
            {
                Debugger.Break();

                networkStream.Close();
                _client.Close();
                _secureStream.Close();
            }

            InitializeEventHandlerSecure(_secureStream);
        }


        private Func<NetworkMessage> _action;
        private readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

        /// <summary>
        /// Sends some data to the server an waits for a response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="path"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public TReturn Send<T, TReturn>(string path, T obj = default)
        {
            // Holds the data that was received
            TReturn data = default;

            try
            {
                // Stop client from handling server events
                _handleReceivedEvents = false;

                // The network message that will be sent to the server
                var message = new NetworkMessage()
                {
                    Path = path,
                };

                // If the request should contain data
                if (obj != null)
                {
                    // Serialze the data 
                    message.Message = _serializer.Serialize(obj);

                    // Set the type of data (To which type of object it should be serialized to)
                    message.MessageTypeName = typeof(T).AssemblyQualifiedName;
                };

                // Sent the data to the server
                _client.Client.Send(_serializer.Serialize(message));

                // Wait for a response from the server
                data = WaitForMessage<TReturn>();
            }
            finally
            {
                // No matter what happes in the try block, allow the client to handle server-events again
                _handleReceivedEvents = true;
            };

            return data;
        }



        public NetworkMessage SendSecure<T>(string path, T obj = default)
        {
            NetworkMessage netMessage = new NetworkMessage()
            {
                Path = path,

                SerializerType = _serializerType,
            };

            if (obj != null)
            {
                netMessage.Message = _serializer.Serialize(obj);
                netMessage.MessageTypeName = typeof(T).AssemblyQualifiedName;
            };

            StringBuilder message = new StringBuilder(Encoding.UTF8.GetString(_serializer.Serialize(netMessage)));

            //SslStream secureStream = GetAuthenticatedStream("LocalHost");


            int size = message.Length;
            string sizeAsString = size.ToString();
            string sizePadded = sizeAsString.PadRight(8);

            message.Insert(0, sizePadded);

            for (int a = 0; a < sizeAsString.Length; a++)
            {
                message[a] = sizeAsString[a];
            };

            byte[] messageBytes = Encoding.UTF8.GetBytes(message.ToString());

            NetworkMessage networkMessage = null;

            try
            {
                _handleReceivedEvents = false;
                _manualResetEvent.Reset();

                _secureStream.Write(messageBytes);
                _secureStream.Flush();

                _manualResetEvent.WaitOne();

                networkMessage = _action();
            }
            finally
            {
                _handleReceivedEvents = true;
                _action = null;
            }

            return networkMessage;
        }


        private void InitializeEventHandlerSecure(SslStream secureStream)
        {
            // Start a background thread
            Task.Run(() =>
            {
                try
                {

                    // Continiously try and see if there's any data avaiilable
                    while (true)
                    {
                        //SslStream secureStream = GetAuthenticatedStream("LocalHost");

                        byte[] sizeBuffer = new byte[8];
                        int readBytes = secureStream.Read(sizeBuffer, 0, 8);

                        if (readBytes == 0)
                        {
                            Debugger.Break();
                            return;
                        };

                        int requestSize = GetRequestSize(sizeBuffer);
                        byte[] buffer = new byte[requestSize];
                        readBytes = secureStream.Read(buffer);

                        // If the client sent a message to the server and is expecting a result
                        if (_handleReceivedEvents == false)
                        {
                            // If we reached here, all data was read. Deserilize the data to a ServerEvent 
                            NetworkMessage networkMessage = _serializer.Deserialize<NetworkMessage>(buffer);

                            _action = () => networkMessage;
                            _manualResetEvent.Set();

                            // Don't handle *this request
                            continue;
                        }

                        // If we reached here, all data was read. Deserilize the data to a ServerEvent 
                        ServerEvent serverEvent = _serializer.Deserialize<ServerEvent>(buffer);

                        // If the server event contains arguemnts
                        if (serverEvent.EventHasArgs == true)
                        {
                            // Get the argument type from the DataTypename
                            var argType = Type.GetType(serverEvent.DataTypename);

                            // Try to find an event with the corresponding name
                            _receivedEventsArgs.TryGetValue(serverEvent.EventName, out Action<object> action);

                            // Call the event and pass it the arguments 
                            action?.Invoke(_serializer.Deserialize(serverEvent.Data, argType));
                        }
                        // If no arguments present
                        else
                        {
                            // Try to find the event 
                            _receivedEvents.TryGetValue(serverEvent.EventName, out Action action);

                            // And call it
                            action?.Invoke();
                        };
                    };
                }
                catch(IOException ioException)
                {
                    _client.Close();
                    _secureStream.Close();
                }
            });
        }


        /// <summary>
        /// Register a client-side event 
        /// </summary>
        /// <param name="eventName"> The name of the event </param>
        /// <param name="action"> The action to execute </param>
        /// <returns></returns>
        public TestTcpClient AddReceivedEvent(string eventName, Action action)
        {
            // Try to add the event
            bool added = _receivedEvents.TryAdd(eventName, action);

            if (added == false)
                throw new Exception($"{eventName} event already exists");

            return this;
        }

        /// <summary>
        /// Register a client-side event, that can take an argument
        /// </summary>
        /// <param name="eventName"> The name of the event </param>
        /// <param name="action"> The action to execute </param>
        /// <returns></returns>
        public TestTcpClient AddReceivedEvent<T>(string eventName, Action<T> action)
        {
            // Try to add the event
            bool added = _receivedEventsArgs.TryAdd(eventName,
            // Because you obviously can't convert an Action<T> to Action<object>, 
            // I used a simple lambda that takes an object and converts it to T
            (object arg) =>
            {
                action((T)arg);
            });

            if (added == false)
                throw new Exception($"{eventName} event already exists");

            return this;
        }


        /// <summary>
        /// Closes the connection between the server and client
        /// </summary>
        public void Close()
        {
            // Close the underlying stream and socket connection 
            var networkStream = _client.GetStream();

            _client.Close();
            networkStream.Close();

            _secureStream.Close();
        }


        #endregion


        #region Private helpers

        /// <summary>
        /// Initializes server-event hanlding
        /// </summary>
        private void InitializeEventHandler()
        {
            // Start a background thread
            Task.Run(() =>
            {
                // Continiously try and see if there's any data avaiilable
                while (true)
                {
                    // Get the client's stream 
                    NetworkStream networkStream = _client.GetStream();

                    // Wait until data is present
                    while (networkStream.CanRead == true &&
                           networkStream.DataAvailable == false)
                        Thread.Sleep(1);

                    // If the stream isn't read-able 
                    if (networkStream.CanRead == false)
                        // Break out of the function
                        return;

                    // If the client sent a message to the server and is expecting a result
                    if (_handleReceivedEvents == false)
                        // Don't handle *this request
                        continue;

                    // Read the data
                    var completeRequest = ReadReceivedPacket(networkStream);

                    // If we reached here, all data was read. Deserilize the data to a ServerEvent 
                    ServerEvent serverEvent = _serializer.Deserialize<ServerEvent>(completeRequest);

                    // If the server event contains arguemnts
                    if (serverEvent.EventHasArgs == true)
                    {
                        // Get the argument type from the DataTypename
                        var argType = Type.GetType(serverEvent.DataTypename);

                        // Try to find an event with the corresponding name
                        _receivedEventsArgs.TryGetValue(serverEvent.EventName, out Action<object> action);

                        // Call the event and pass it the arguments 
                        action?.Invoke(_serializer.Deserialize(serverEvent.Data, argType));
                    }
                    // If no arguments present
                    else
                    {
                        // Try to find the event 
                        _receivedEvents.TryGetValue(serverEvent.EventName, out Action action);

                        // And call it
                        action?.Invoke();
                    };
                };
            });
        }

        /// <summary>
        /// Waits for a message to be received
        /// </summary>
        /// <typeparam name="T"> The type of message expected </typeparam>
        /// <returns></returns>
        private T WaitForMessage<T>()
        {
            // Get the client's stream
            NetworkStream networkStream = _client.GetStream();

            // Wait until there is actually data available
            while (networkStream.DataAvailable == false)
                Thread.Sleep(1);

            // Read the data
            var completeRequest = ReadReceivedPacket(networkStream);

            // Deserialize the packet into a NetworkMessage
            NetworkMessage data = _serializer.Deserialize<NetworkMessage>(completeRequest);

            // Further deserilize the inner message to T
            return _serializer.Deserialize<T>(data.Message);
        }


        /// <summary>
        /// Reads received packet entirely
        /// </summary>
        /// <param name="networkStream"> The stream containing the data </param>
        /// <returns></returns>
        private byte[] ReadReceivedPacket(NetworkStream networkStream)
        {
            // Allocate some buffers to store the request data
            byte[] buffer = new byte[1024];
            List<byte> completeRequest = new List<byte>();

            // White there is data present
            while (networkStream.DataAvailable == true)
            {
                // Read the data into the buffer
                int readBytes = networkStream.Read(buffer, 0, buffer.Length);

                // Insert the read data into the completeRequest list
                for (int a = 0; a < readBytes; a++)
                {
                    completeRequest.Add(buffer[a]);
                };
            };

            return completeRequest.ToArray();
        }

        #endregion



        private bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            return false;
        }



        private static NetworkMessage GetNetMessage(int requestSize, SslStream secureStream, ISerializer serializer)
        {
            byte[] buffer = new byte[requestSize];
            int readBytes = secureStream.Read(buffer);

            var receivedNetMessage = serializer.Deserialize<NetworkMessage>(buffer);

            return receivedNetMessage;
        }

        private static int GetRequestSize(byte[] buffer)
        {
            int size = Convert.ToInt32(Encoding.UTF8.GetString(buffer));

            return size;
        }

    };
};