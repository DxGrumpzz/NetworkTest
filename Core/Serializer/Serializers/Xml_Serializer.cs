namespace Core
{
    using System.IO;
    using System.Xml.Serialization;

    public class Xml_Serializer : ISerializer
    {
        public T Deserialize<T>(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                return (T)serializer.Deserialize(stream);
            };
        }

        public byte[] Serialize<T>(T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                serializer.Serialize(stream, obj);

                return stream.ToArray();
            };
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
