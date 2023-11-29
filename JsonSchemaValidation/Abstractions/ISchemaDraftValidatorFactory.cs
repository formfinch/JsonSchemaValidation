using JsonSchemaValidation.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaDraftValidatorFactory
    {
        string DraftVersion { get; }
        ISchemaValidator CreateValidator(SchemaMetadata schemaData);
    }
}
