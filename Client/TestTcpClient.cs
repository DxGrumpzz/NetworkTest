namespace Client
{
    using Core;

    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    public class TestTcpClient
    {
        private bool _handleReceivedEvents = true;

        private TcpClient _client;
        private IPEndPoint _endPoint;

        private ISerializer _serializer;

        private Dictionary<string, Action> _receivedEvents = new Dictionary<string, Action>();
        private Dictionary<string, Action<object>> _receivedEventsArgs = new Dictionary<string, Action<object>>();


        public TestTcpClient(IPEndPoint endPoint, ISerializer serializer, AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            _client = new TcpClient(addressFamily);
            _serializer = serializer;

            _endPoint = endPoint;
        }

        public TestTcpClient(IPEndPoint endPoint, SerializerType serializerType = SerializerType.Json, AddressFamily addressFamily = AddressFamily.InterNetwork) :
            this(endPoint, null, addressFamily)
        {
            if (serializerType == SerializerType.Json)
            {
                _serializer = new Json_Serializer();
            }
            else if (serializerType == SerializerType.Xml)
            {
                _serializer = new Xml_Serializer();
            }
            else if (serializerType == SerializerType.Binary)
            {

            };
        }

        /*
        public TReturn Send<T, TReturn>(string path, T obj)
        {
            byte[] buffer = new byte[1024];
            List<byte> completeRequest = new List<byte>();


            NetworkStream networkStream = _client.GetStream();

            _handleReceivedEvents = false;

            _client.Client.Send(_serializer.Serialize(new NetworkMessage()
            {
                Path = path,
                Message = _serializer.Serialize(obj),
                MessageTypeName = typeof(T).AssemblyQualifiedName,
            }));

            while (networkStream.DataAvailable == false)
                Thread.Sleep(1);


            while (networkStream.DataAvailable == true)
            {
                int readBytes = networkStream.Read(buffer, 0, buffer.Length);

                for (int a = 0; a < readBytes; a++)
                {
                    completeRequest.Add(buffer[a]);
                };
            };

            TReturn data = _serializer.Deserialize<TReturn>(completeRequest.ToArray());

            _handleReceivedEvents = true;

            return data;
        }
        */



        public TReturn Send<T, TReturn>(string path, T obj)
        {
            TReturn data = default;

            try
            {
            _handleReceivedEvents = false;

                var message = new NetworkMessage()
            {
                Path = path,
                };

                if(obj != null)
                {
                    message.Message = _serializer.Serialize(obj);
                    message.MessageTypeName = typeof(T).AssemblyQualifiedName;
                };

                _client.Client.Send(_serializer.Serialize(message));

                data = WaitForMessage<TReturn>();
            }
            finally
            {
            _handleReceivedEvents = true;
            };

            return data;
        }

        public void InitializeConnection()
        {
            _client.Connect(_endPoint);

            InitializeEventHandler();
        }


        public TestTcpClient AddReceivedEvent(string eventName, Action action)
        {
            bool added = _receivedEvents.TryAdd(eventName, action);

            if (added == false)
                throw new Exception($"{eventName} event already exists");

            return this;
        }

        public TestTcpClient AddReceivedEvent<T>(string eventName, Action<T> action)
        {
            bool added = _receivedEventsArgs.TryAdd(eventName, 
            (object arg) =>
            {
                action((T)arg);
            });

            if (added == false)
                throw new Exception($"{eventName} event already exists");

            return this;
        }


        public void Close()
        {
            var networkStream = _client.GetStream();

            _client.Close();
            networkStream.Close();
        }


        private void InitializeEventHandler()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    NetworkStream networkStream = _client.GetStream();

                    while (networkStream.CanRead == true &&
                           networkStream.DataAvailable == false)
                        Thread.Sleep(1);

                    if (networkStream.CanRead == false)
                        return;

                    if (_handleReceivedEvents == false)
                        continue;


                    byte[] buffer = new byte[1024];
                    List<byte> completeRequest = new List<byte>();

                    while (networkStream.DataAvailable == true)
                    {
                        int readBytes = networkStream.Read(buffer, 0, buffer.Length);

                        for (int a = 0; a < readBytes; a++)
                        {
                            completeRequest.Add(buffer[a]);
                        };
                    };

                    ServerEvent serverEvent = _serializer.Deserialize<ServerEvent>(completeRequest.ToArray());


                    if (serverEvent.EventHasArgs == true)
                    {
                        var argType = Type.GetType(serverEvent.DataTypename);

                        _receivedEventsArgs.TryGetValue(serverEvent.EventName, out Action<object> action);

                        action?.Invoke(_serializer.Deserialize(serverEvent.Data, argType));
                    }
                    else
                    {
                        _receivedEvents.TryGetValue(serverEvent.EventName, out Action action);
                        action?.Invoke();
                    };
                };
            });
        }


        private T WaitForMessage<T>()
        {
            NetworkStream networkStream = _client.GetStream();

            while (networkStream.DataAvailable == false)
                Thread.Sleep(1);

            byte[] buffer = new byte[1024];
            List<byte> completeRequest = new List<byte>();

            while (networkStream.DataAvailable == true)
            {
                int readBytes = networkStream.Read(buffer, 0, buffer.Length);

                for (int a = 0; a < readBytes; a++)
                {
                    completeRequest.Add(buffer[a]);
                };
            };

            NetworkMessage data = _serializer.Deserialize<NetworkMessage>(completeRequest.ToArray());

            return _serializer.Deserialize<T>(data.Message);
        }

    };
};


/*
public TReturn Send<T, TReturn>(T obj)
{
    byte[] buffer = new byte[1024];
    List<byte> completeRequest = new List<byte>();


    NetworkStream networkStream = _client.GetStream();

    _handleReceivedEvents = false;

    _client.Client.Send(_serializer.Serialize(new NetworkMessage()
    {
        Message = obj
    }));

    while (networkStream.DataAvailable == false)
        Thread.Sleep(1);


    while (networkStream.DataAvailable == true)
    {
        int readBytes = networkStream.Read(buffer, 0, buffer.Length);

        for (int a = 0; a < readBytes; a++)
        {
            completeRequest.Add(buffer[a]);
        };
    };

    TReturn data = _serializer.Deserialize<TReturn>(completeRequest.ToArray());

    _handleReceivedEvents = true;

    return data;
}
*/
