using JsonSchemaValidation.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012
{
    public class SchemaRepository : ISchemaRepository
    {
        public JsonDocument GetSchema(Uri uri)
        {
            throw new NotImplementedException();
        }

        public void RegisterSchema(Uri uri, JsonDocument schema)
        {
            throw new NotImplementedException();
        }
    }
}
