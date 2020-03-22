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