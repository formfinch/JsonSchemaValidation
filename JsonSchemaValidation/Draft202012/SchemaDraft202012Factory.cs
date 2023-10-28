using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012
{
    public class SchemaDraft202012Factory : ISchemaDraftFactory
    {
        private readonly IEnumerable<ISchemaDraftKeywordValidatorFactory> _keywordFactories;

        public string DraftVersion => "https://json-schema.org/draft/2020-12/schema";

        public SchemaDraft202012Factory(IEnumerable<ISchemaDraftKeywordValidatorFactory> keywordFactories)
        {
            this._keywordFactories = keywordFactories;
        }

        public ISchemaValidator CreateValidator(JsonElement schema)
        {
            var schemaValidator = new SchemaValidator();

            // Loop through each keyword factory to create keyword validators and add them to the schema validator.
            foreach (var keywordFactory in _keywordFactories)
            {
                var keywordValidator = keywordFactory.Create(schema);
                if (keywordValidator != null)
                {
                    schemaValidator.AddKeywordValidator(keywordValidator);
                }
            }
            return schemaValidator;
        }
    }
}
