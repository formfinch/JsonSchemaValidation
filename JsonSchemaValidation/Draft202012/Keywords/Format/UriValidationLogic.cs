namespace JsonSchemaValidation.Draft202012.Keywords.Format
{
    internal class UriValidationLogic
    {
        private readonly bool iriSupport;
        private readonly bool canBeRelative;
        private readonly UriTemplateExpander? expander;

        public UriValidationLogic(bool iriSupport = false, bool canBeRelative = false, bool isTemplate = false)
        {
            if (isTemplate)
            {
                this.iriSupport = false; // Turning off IRI support for templates
                this.canBeRelative = true; // Allowing relative URIs for templates
                this.expander = new UriTemplateExpander();
            }
            else
            {
                this.iriSupport = iriSupport;
                this.canBeRelative = canBeRelative;
                this.expander = null;
            }
        }

        public bool IsValidUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return false;
            }

            if (canBeRelative)
            {
                if (uri.StartsWith("//"))
                {
                    uri = "scheme:" + uri;
                }
                else if (!uri.Contains("://"))
                {
                    uri = "scheme://host" + uri;
                }
            }

            if (expander != null)
            {
                uri = expander.ExpandTemplate(uri);
            }

            if (Uri.TryCreate(uri, UriKind.Absolute, out Uri? validatedUri))
            {
                if (iriSupport
                    && !string.IsNullOrWhiteSpace(validatedUri.PathAndQuery)
                    && validatedUri.HostNameType != UriHostNameType.IPv6
                    && !string.IsNullOrWhiteSpace(validatedUri.Host)
                    && !string.IsNullOrWhiteSpace(validatedUri.IdnHost))
                {
                    var iri = uri.Replace(validatedUri.Host, validatedUri.IdnHost);
                    if (Uri.TryCreate(iri, UriKind.Absolute, out Uri? validatedIri))
                    {
                        validatedUri = validatedIri;
                    }
                }
                return validatedUri.IsWellFormedOriginalString();
            }
            return false;
        }
    }
}
