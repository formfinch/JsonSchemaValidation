// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
namespace FormFinch.JsonSchemaValidation.Exceptions
{
    public class InvalidSchemaException : Exception
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
    }
}
