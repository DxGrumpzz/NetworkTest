namespace Core
{
    using System;
    using System.Text;
    using System.Text.Json;

    public class Json_Serializer : ISerializer
    {
        private JsonSerializerOptions _options;


        public Json_Serializer(JsonSerializerOptions options = default)
        {
            _options = options;
        }


        public byte[] Serialize<T>(T obj)
        {
            var b = JsonSerializer.SerializeToUtf8Bytes(obj, _options);
            string s = Encoding.UTF8.GetString(b);

            return b;
        }


        public object Deserialize(byte[] data, Type type)
        {
            return JsonSerializer.Deserialize(data, type, _options);
        }

        public T Deserialize<T>(byte[] data)
        {
            return JsonSerializer.Deserialize<T>(data, _options);
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
