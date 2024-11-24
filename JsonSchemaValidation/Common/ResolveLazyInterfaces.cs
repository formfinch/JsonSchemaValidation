using JsonSchemaValidation.Abstractions;

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
