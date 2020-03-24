namespace Core
{
    using System;

    /// <summary>
    /// A Client-side "event" that can be executed when called from the server
    /// </summary>
    public class ServerEvent
    {
        /// <summary>
        /// The name of the registered event
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// The message that will be sent to the cleint/server, as a byte array
        /// </summary>
        /// <remarks>
        /// This is a byte array because, it allows more consice .NET object serializaion with JSON 
        /// </remarks>
        public byte[] Data { get; set; }

        /// <summary>
        /// The "Assembly Quialified Type Name" of the serialized type. This property *MUST* be set in orded for the serialization to work properly
        /// </summary>
        public string DataTypename { get; set; }

        /// <summary>
        /// A boolean flag that indicates if this event has any properties that should be passed to the client's event
        /// </summary>
        public bool EventHasArgs => Data != null;

    };
};
