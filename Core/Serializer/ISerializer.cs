namespace Core
{
    using System;

    /// <summary>
    /// A basic serializer defenition 
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serialize the object to a byte array
        /// </summary>
        /// <typeparam name="T"> The type of the object </typeparam>
        /// <param name="obj"> the object to serialize </param>
        /// <returns></returns>
        public byte[] Serialize<T>(T obj);

        /// <summary>
        /// Deserialize the byte array to an object
        /// </summary>
        /// <typeparam name="T"> The type of object to deserialize </typeparam>
        /// <param name="data"> The data to deserialize </param>
        /// <returns></returns>
        public T Deserialize<T>(byte[] data);

        /// <summary>
        /// Deseirlizes a byte array to arbitrary .NET object using System.Type as a "type argument"
        /// </summary>
        /// <param name="data"> The data to deserialize </param>
        /// <param name="type"> the object's type as a System.Type </param>
        /// <returns></returns>
        public object Deserialize(byte[] data, Type type) { throw new NotImplementedException();  }

    }
};