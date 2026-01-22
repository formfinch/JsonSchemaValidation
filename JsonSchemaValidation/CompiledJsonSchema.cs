// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;
using FormFinch.JsonSchemaValidation.Abstractions;
using FormFinch.JsonSchemaValidation.Common;
using FormFinch.JsonSchemaValidation.Validation.Output;

namespace FormFinch.JsonSchemaValidation
{
    /// <summary>
    /// Internal implementation of <see cref="IJsonSchema"/>.
    /// </summary>
    internal sealed class CompiledJsonSchema : IJsonSchema
    {
        private readonly ISchemaValidator _validator;
        private readonly IJsonValidationContextFactory _contextFactory;

        /// <inheritdoc />
        public Uri SchemaUri { get; }

        internal CompiledJsonSchema(
            ISchemaValidator validator,
            IJsonValidationContextFactory contextFactory,
            Uri schemaUri)
        {
            _validator = validator;
            _contextFactory = contextFactory;
            SchemaUri = schemaUri;
        }

        /// <inheritdoc />
        public OutputUnit Validate(string instanceJson, OutputFormat format = OutputFormat.Basic)
        {
            using var doc = JsonDocument.Parse(instanceJson);
            return Validate(doc.RootElement, format);
        }

        /// <inheritdoc />
        public OutputUnit Validate(JsonElement instance, OutputFormat format = OutputFormat.Basic)
        {
            var context = _contextFactory.CreateContextForRoot(instance);
            return _validator.ValidateWithOutput(context, format);
        }

        /// <inheritdoc />
        public bool IsValid(string instanceJson)
        {
            using var doc = JsonDocument.Parse(instanceJson);
            return IsValid(doc.RootElement);
        }

        /// <inheritdoc />
        public bool IsValid(JsonElement instance)
        {
            var context = _contextFactory.CreateContextForRoot(instance);
            return _validator.IsValid(context);
        }
    }
}
