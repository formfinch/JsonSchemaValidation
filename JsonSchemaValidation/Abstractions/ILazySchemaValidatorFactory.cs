namespace FormFinch.JsonSchemaValidation.Abstractions
{
    public interface ILazySchemaValidatorFactory
    {
        public ISchemaValidatorFactory? Value { get; set; }
    }
}
