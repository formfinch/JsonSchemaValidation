namespace JsonSchemaValidation.DependencyInjection
{
    /// <summary>
    /// Options specific to JSON Schema Draft 7 validation.
    /// </summary>
    public class Draft7Options
    {
        /// <summary>
        /// When true, the 'format' keyword performs validation in addition to producing annotations.
        /// Default is true for Draft 7, as format was traditionally asserting in early drafts.
        /// </summary>
        public bool FormatAssertionEnabled { get; set; } = true;

        /// <summary>
        /// When true, the 'contentEncoding' and 'contentMediaType' keywords perform validation
        /// in addition to producing annotations.
        /// Default is false, as these keywords are annotation-only per JSON Schema spec.
        /// </summary>
        public bool ContentAssertionEnabled { get; set; } = false;
    }
}
