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
        /// A stream that provides a secure way to transfer data between hosts
        /// </summary>
        private SslStream _secureStream;



        /// <summary>
        /// The type of _serializer
        /// </summary>
        private ISerializer _serializer;

        /// <summary>
        /// The type of serializer used to serialzie/deserialize the data
        /// </summary>
        private SerializerType _serializerType;



        /// <summary>
        /// A dictionary that contains registered client side events
        /// </summary>
        private Dictionary<string, Action> _receivedEvents = new Dictionary<string, Action>();

        /// <summary>
        /// A dictionary that contains registered client side events that can take an argument
        /// </summary>
        private Dictionary<string, Action<object>> _receivedEventsArgs = new Dictionary<string, Action<object>>();



        /// <summary>
        /// The received response from the server, is set when the user sends some data to the server and expecting a result
        /// </summary>
        /// <remarks>
        /// Why on earth is this a Func ?
        /// I don't want to deal with reference mangling so when we return the Data to the used 
        /// it is cleared by the GC automagikaly
        /// </remarks>
        private Func<NetworkMessage> _receviedResponse;

        /// <summary>
        /// A reset event used to synchornize recevied respones when client sends a request to the server expecting a result
        /// </summary>
        private readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(false);


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



        #region In-secure

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

        #endregion



        #region Secure



        /// <summary>
        /// Initializes a secure connection using ssl
        /// </summary>
        /// <param name="targetAuthenticationName"> The name of the target host </param>
        public void InitializeConnectionSecure(string targetAuthenticationName)
        {
            // Start the connection
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
            // If authentication failed
            catch (AuthenticationException e)
            {
                Debugger.Break();

                networkStream.Close();
                _client.Close();
                _secureStream.Close();
            }

            // If the connection and authentication process went well, 
            // Start receving data
            InitializeEventHandlerSecure();
        }



        /// <summary>
        /// Sends some data to the server under a secure stream and wait for a response
        /// </summary>
        /// <typeparam name="T"> The type of data </typeparam>
        /// <param name="path"> The path to call in the server </param>
        /// <param name="obj"> The data to send </param>
        /// <returns></returns>
        public NetworkMessage SendSecure<T>(string path, T obj = default)
        {
            // The message that will be sent to the server
            NetworkMessage netMessage = new NetworkMessage()
            {
                Path = path,

                SerializerType = _serializerType,
            };

            // If the user wants the message to contain arguments
            if (obj != null)
            {
                // Serialize the data
                netMessage.Message = _serializer.Serialize(obj);
                netMessage.MessageTypeName = typeof(T).AssemblyQualifiedName;
            };


            try
            {
                // "Route" recieved responses so it will return the value here
                _handleReceivedEvents = false;

                // Reset the ResetEvent so we are able to wait for the request
                _manualResetEvent.Reset();

                // Actually send the data
                _secureStream.Write(GetRequestAsBytes(netMessage));

                // Wait until a response is received
                _manualResetEvent.WaitOne();

                // Return the data to the user
                return _receviedResponse();
            }
            finally
            {
                _handleReceivedEvents = true;
                _receviedResponse = null;
            }
        }



        #endregion



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

            _secureStream?.Close();
        }



        #endregion



        #region Private helpers


        /// <summary>
        /// Initializes a server response hanlder
        /// </summary>
        private void InitializeEventHandlerSecure()
        {
            // Start a background thread
            Task.Run(() =>
            {
                try
                {
                    // Continiously try and see if there's any data avaiilable
                    while (true)
                    {
                        // Read the first 8 bytes so we know the size of the request
                        byte[] sizeBuffer = new byte[8];
                        int readBytes = _secureStream.Read(sizeBuffer, 0, 8);


                        if (readBytes == 0)
                        {
                            Debugger.Break();
                            return;
                        };

                        // Get request size and read the request entirely without needing to call Read() multiple times
                        int requestSize = GetRequestSize(sizeBuffer);
                        byte[] buffer = new byte[requestSize];
                        readBytes = _secureStream.Read(buffer);

                        // If the client sent a message to the server and is expecting a result
                        if (_handleReceivedEvents == false)
                        {
                            // If we reached here, all data was read. Deserilize the data to a ServerEvent 
                            NetworkMessage networkMessage = _serializer.Deserialize<NetworkMessage>(buffer);

                            _receviedResponse = () => networkMessage;
                            _manualResetEvent.Set();

                            // Don't handle *this request
                            continue;
                        }

                        // If we reached here, all data was read. Deserilize the data to a ServerEvent 
                        ServerEvent serverEvent = _serializer.Deserialize<ServerEvent>(buffer);

                        // Handle the received server event
                        DelegateReceivedEvent(serverEvent);
                    };
                }
                catch (IOException ioException)
                {
                    _client.Close();
                    _secureStream.Close();
                }
            });
        }


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

                    DelegateReceivedEvent(serverEvent);
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


        /// <summary>
        /// Returns a request's size by reading the first 8 bytes
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private int GetRequestSize(byte[] buffer)
        {
            // Convert the buffer to a "read-able" string and convert the string to a number
            int size = Convert.ToInt32(Encoding.UTF8.GetString(buffer));

            return size;
        }


        /// <summary>
        /// Validates a certificate, This is code taken from Microsoft: https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netcore-3.1#examples
        /// I don't 100% know what is going on here. Yet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            return false;
        }


        /// <summary>
        /// "Converts" a <see cref="NetworkMessage"/> into a "formatted" byte[] buffer/pakcet
        /// </summary>
        /// <param name="request"> The request the will be sent to the server </param>
        /// <returns></returns>
        private byte[] GetRequestAsBytes(NetworkMessage request)
        {
            // Serialize the NetworkMessage
            byte[] serializerdMessageBytes = _serializer.Serialize(request);

            // The size of the message *After the operation
            int messageSize = serializerdMessageBytes.Length + 8;

            // Using a memory stream to manipulate the buffers
            using (MemoryStream memoryStream = new MemoryStream(messageSize))
            {
                // Get the size of the request,
                long size = serializerdMessageBytes.Length;
                // Turn it into a string with a 8 character padding
                string sizePadded = size.ToString().PadRight(8);

                // Write the request size into the MemoryStrem buffer
                memoryStream.Write(Encoding.UTF8.GetBytes(sizePadded));

                // Write the actuall request into the MemoryStrem buffer
                memoryStream.Write(serializerdMessageBytes);

                // Set the MemoryStream buffer in messageBytes
                return memoryStream.ToArray();
            };
        }


        /// <summary>
        /// "Delegates" a received <see cref="ServerEvent"/> to a <see cref="_receivedEvents"/> or <see cref="_receivedEventsArgs"/>
        /// </summary>
        /// <param name="serverEvent"></param>
        private void DelegateReceivedEvent(ServerEvent serverEvent)
        {
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
        }


        #endregion


    };
};