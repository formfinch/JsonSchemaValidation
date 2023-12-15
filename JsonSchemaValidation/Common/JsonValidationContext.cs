using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.Common
{
    public class JsonValidationContext : IJsonValidationContext
    {
        private readonly JsonElement _data;

        public JsonValidationContext(JsonElement data)
        {
            _data = data;
        }

        public JsonElement Data
        {
            get { return _data; }
        }
    }
}
