using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaRepository
    {
        Uri AddSchema(JsonElement schema, Uri? fallbackUri = null);
        JsonElement GetSchema(Uri schemaUri);
    }
}
