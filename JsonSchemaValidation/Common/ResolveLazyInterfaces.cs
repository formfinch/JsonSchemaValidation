using FormFinch.JsonSchemaValidation.Abstractions;

namespace FormFinch.JsonSchemaValidation.Common
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
