namespace Core
{
    public enum SerializerType
    {
        Json = 0,
        Xml = 1,
        Binary = 2,
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
