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
