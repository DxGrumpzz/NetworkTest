namespace Core
{
    using System;
    using System.Text.Json;

    /// <summary>
    /// The "default" implementation of a Json serializer, using System.Text.Json 
    /// </summary>
    public class Json_Serializer : ISerializer
    {
        /// <summary>
        /// Options that can be passed to the json serializer
        /// </summary>
        private JsonSerializerOptions _options;


        public Json_Serializer(JsonSerializerOptions options = default)
        {
            _options = options;
        }


        public byte[] Serialize<T>(T obj)
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj, _options);
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