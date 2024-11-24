namespace JsonSchemaValidation.DependencyInjection
{
    public class SchemaValidationOptions
    {
        public string DefaultDraftVersion { get; set; } = "https://json-schema.org/draft/2020-12/schema";
        public bool EnableDraft202012 { get; set; } = true;
        public bool EnableDraft4 { get; set; } = true;

        // Add other options if needed
    }
}
