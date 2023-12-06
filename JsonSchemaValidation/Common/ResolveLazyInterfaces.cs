using JsonSchemaValidation.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Common
{
    internal class ResolveLazyInterfaces
    {
        public ResolveLazyInterfaces(
            ILazySchemaValidatorFactory lazySchemaValidatorFactory, ISchemaValidatorFactory schemaValidatorFactory
        )
        {
            lazySchemaValidatorFactory.Value = schemaValidatorFactory;
        }
    }
}
