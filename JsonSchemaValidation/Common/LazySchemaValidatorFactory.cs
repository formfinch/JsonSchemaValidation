using JsonSchemaValidation.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Common
{
    internal class LazySchemaValidatorFactory : ILazySchemaValidatorFactory
    {
        private ISchemaValidatorFactory? _schemaValidatorFactory;

        public ISchemaValidatorFactory? Value
        {
            get => _schemaValidatorFactory;
            set {
                _schemaValidatorFactory ??= value;
            }
        }
    }
}
