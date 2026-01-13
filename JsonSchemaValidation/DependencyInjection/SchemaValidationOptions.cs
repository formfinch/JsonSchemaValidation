namespace JsonSchemaValidation.DependencyInjection
{
    public class SchemaValidationOptions
    {
        public string DefaultDraftVersion { get; set; } = "https://json-schema.org/draft/2020-12/schema";
        public bool EnableDraft202012 { get; set; } = true;
        public bool EnableDraft201909 { get; set; } = true;
        public bool EnableDraft7 { get; set; } = true;
        public bool EnableDraft6 { get; set; } = true;
        public bool EnableDraft4 { get; set; } = true;

        /// <summary>
        /// When true, the 'format' keyword performs validation in addition to producing annotations.
        /// When false (default), the 'format' keyword only produces annotations per Draft 2020-12 spec.
        /// </summary>
        public bool FormatAssertionEnabled { get; set; } = false;
    }
}
