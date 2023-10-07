using System.Runtime.Serialization;

namespace JsonSchemaValidation.Exceptions
{
    [Serializable]
    internal class InvalidSchemaException : Exception
    {
        public InvalidSchemaException()
        {
        }

        public InvalidSchemaException(string? message) : base(message)
        {
        }

        public InvalidSchemaException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected InvalidSchemaException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}