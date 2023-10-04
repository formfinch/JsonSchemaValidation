using JsonSchemaValidation.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Abstractions.Keywords
{
    public interface IKeywordValidator
    {
        ValidationResult Validate(JsonElement instance);
    }
}
