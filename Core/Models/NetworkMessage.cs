namespace Core
{
    /// <summary>
    /// A message that will be sent over the network to the client/server
    /// </summary>
    public class NetworkMessage
    {
        /// <summary>
        /// The message that will be sent to the cleint/server, as a byte array
        /// </summary>
        /// <remarks>
        /// This is a byte array because, it allows more consice .NET object serializaion with JSON 
        /// </remarks>
        public byte[] Message { get; set; }

        /// <summary>
        /// The path of the request. Must be '/' seperated
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// A boolean flag that indicates if this request has a path
        /// </summary>
        public bool HasPath => string.IsNullOrWhiteSpace(Path);

        /// <summary>
        /// Returns a split string array seperated by '/'
        /// </summary>
        public string[] PathSegments => Path.Split('/');

        /// <summary>
        /// The "Assembly Quialified Type Name" of the serialized type. This property *MUST* be set in orded for the serialization to work properly
        /// </summary>
        public string MessageTypeName { get; set; }

        /// <summary>
        /// A Boolean flag that indicates if this request contains any arguemnts that will be passed to an Action
        /// </summary>
        public bool RequestHasArguments => Message != null ? true : false;

        /// <summary>
        /// The type of serializer used to serialize this message
        /// </summary>
        public SerializerType SerializerType { get; set; }


        /// <summary>
        /// Deserializes this message as T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T MessageAs<T>()
        {
            if (SerializerType == SerializerType.Json)
            {
                return new Json_Serializer().Deserialize<T>(Message);
            };

            return default;
        }

    }
};