using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012.Keywords
{
    internal class TypeKeywordHandler : IKeywordHandler
    {
        public ValidationResult Validate(JsonElement schemaKeyword, JsonElement jsonData)
        {
            throw new NotImplementedException();
        }
    }
}
