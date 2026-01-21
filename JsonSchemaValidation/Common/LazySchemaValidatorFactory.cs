using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Common
{
    internal class LazySchemaValidatorFactory : ILazySchemaValidatorFactory
    {
        private volatile ISchemaValidatorFactory? _schemaValidatorFactory;

        public ISchemaValidatorFactory? Value
        {
            get => _schemaValidatorFactory;
            set => Interlocked.CompareExchange(ref _schemaValidatorFactory, value, null);
        }
    }
}
