namespace Core
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    public class ServerEvent
    {
        public string EventName { get; set; }

        public byte[] Data { get; set; }
        
        public string DataTypename { get; set; }

        public bool EventHasArgs => Data != null;
    };
};
