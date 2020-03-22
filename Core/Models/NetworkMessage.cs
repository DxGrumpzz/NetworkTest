namespace Core
{
    public class NetworkMessage
    {
        public byte[] Message { get; set; }

        public string Path { get; set; } = "";

        public bool HasPath => string.IsNullOrWhiteSpace(Path);

        public string[] PathSegments => Path.Split('/');

        public string MessageTypeName { get; set; }

        public bool RequestHasArguments => Message != null ? true : false;
    }
};