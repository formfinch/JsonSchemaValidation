using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.Draft202012.Interfaces;
using JsonSchemaValidation.Draft202012.Keywords.Format;
using JsonSchemaValidation.Exceptions;
using JsonSchemaValidation.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class IdValidatorFactory : ISchemaDraftKeywordValidatorFactory
    {
        private readonly ISchemaRepository _schemaRepository;

        public IdValidatorFactory(ISchemaRepository schemaRepository)
        {
            _schemaRepository = schemaRepository;
        }

        public IKeywordValidator? Create(SchemaMetadata schemaData)
        {
            var schema = schemaData.Schema;

            if (schema.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!schema.TryGetProperty("$id", out var idElement))
            {
                return null;
            }

            // Standard behavior is obvious misformatting is ignored
            if (idElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var uriValidation = new UriValidationLogic(canBeRelative: true);
            var idText = idElement.ToString();
            var isValid = uriValidation.IsValidUri(idText);
            if (!isValid)
            {
                throw new InvalidSchemaException("The '$id' keyword must be a string representing a valid URI - reference.");
            }

            var rxPattern = new Regex($"^[^#]*#?$");
            if (!rxPattern.IsMatch(idText))
            {
                throw new InvalidSchemaException("The '$id' keyword cannot contain fragments. To use fragments, refer to the '$anchor' keyword.");
            }

            if (Uri.TryCreate(schemaData.SchemaUri, idText, out Uri? fullId))
            {
                var subSchemaData = new SchemaMetadata(schema, schemaData.DraftVersion, fullId);
                subSchemaData.UseSchemaUriForRegistration = true;
                _schemaRepository.TryAddSchema(subSchemaData, out _);
            }

            return null;
        }
    }
}
