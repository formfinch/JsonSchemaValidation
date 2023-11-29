using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class ItemsValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ISchemaValidatorFactory _schemaValidatorFactory;

        public ItemsValidatorFactory(ISchemaFactory schemaFactory, ISchemaValidatorFactory schemaValidatorFactory)
        {
            _schemaFactory = schemaFactory;
            _schemaValidatorFactory = schemaValidatorFactory;
        }

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if(schema.ValueKind == JsonValueKind.Object)
            {
                SchemaMetadata itemSchemaData = _schemaFactory.CreateDereferencedSchema(schemaData);
                var validator = _schemaValidatorFactory.CreateValidator(itemSchemaData);
                return new ItemValidator(validator);
            }

            if (schema.ValueKind == JsonValueKind.Array)
            {
                List<ISchemaValidator> validators = new();
                foreach (JsonElement element in schema.EnumerateArray())
                {
                    SchemaMetadata itemSchema = new(schemaData);
                    itemSchema.Schema = schema;

                    SchemaMetadata itemSchemaData = _schemaFactory.CreateDereferencedSchema(itemSchema);
                    var validator = _schemaValidatorFactory.CreateValidator(itemSchemaData);
                    validators.Add(validator);
                }
                return new ItemsValidator(validators);
            }

            throw new InvalidSchemaException("Item content must be an array or an object");
        }
    }
}
