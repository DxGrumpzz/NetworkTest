namespace Core
{
    using System;
    public interface ISerializer
    {
        public byte[] Serialize<T>(T obj);

        public T Deserialize<T>(byte[] data);

        public object Deserialize(byte[] data, Type type) { throw new NotImplementedException();  }

    }
};