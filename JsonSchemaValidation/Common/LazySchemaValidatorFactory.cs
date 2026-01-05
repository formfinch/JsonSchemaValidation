using JsonSchemaValidation.Abstractions;

namespace JsonSchemaValidation.Common
{
    internal class LazySchemaValidatorFactory : ILazySchemaValidatorFactory
    {
        private ISchemaValidatorFactory? _schemaValidatorFactory;

        public ISchemaValidatorFactory? Value
        {
            get => _schemaValidatorFactory;
            set
            {
                _schemaValidatorFactory ??= value;
            }
        }
    }
}
