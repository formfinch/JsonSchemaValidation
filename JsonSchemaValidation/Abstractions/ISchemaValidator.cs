using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaValidator
    {
        void AddKeywordValidator(IKeywordValidator keywordValidator);
        ValidationResult Validate(JsonElement jsonData);
    }
}
