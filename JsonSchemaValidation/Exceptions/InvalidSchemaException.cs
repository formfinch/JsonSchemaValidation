// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.Exceptions
{
    /// <summary>
    /// Exception thrown when a JSON Schema is invalid or cannot be processed.
    /// </summary>
    /// <remarks>
    /// <b>Thread safety:</b> Exception instances are immutable after construction and safe to share.
    /// </remarks>
    public class InvalidSchemaException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSchemaException"/> class.
        /// </summary>
        public InvalidSchemaException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSchemaException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidSchemaException(string? message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSchemaException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public InvalidSchemaException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
