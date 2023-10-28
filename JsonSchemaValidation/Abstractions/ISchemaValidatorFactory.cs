using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Abstractions
{
    public interface ISchemaValidatorFactory
    {
        ISchemaValidator GetValidator(Uri schemaUri);
    }
}
