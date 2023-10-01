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
        void RegisterSchema(Uri uri, JsonDocument schema);
        JsonDocument GetSchema(Uri uri);
    }
}
