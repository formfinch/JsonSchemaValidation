namespace JsonSchemaValidation.DependencyInjection
{
    /// <summary>
    /// Options specific to JSON Schema Draft 2019-09 validation.
    /// </summary>
    public class Draft201909Options
    {
        /// <summary>
        /// When true, the 'format' keyword performs validation in addition to producing annotations.
        /// Default is false for Draft 2019-09, as format is annotation-only per spec.
        /// Format assertion can also be enabled via the format-assertion vocabulary in the schema.
        /// </summary>
        public bool FormatAssertionEnabled { get; set; } = false;
    }
}
