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
                // Don't modify URN-style URIs (urn:, tag:, etc.) which use ':' but not '://'
                else if (!uri.Contains("://") && !IsAbsoluteUriWithoutAuthority(uri))
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

        /// <summary>
        /// Checks if a URI is an absolute URI that doesn't use authority (like URN, tag:, etc.)
        /// </summary>
        private static bool IsAbsoluteUriWithoutAuthority(string uri)
        {
            // Check for common schemes that don't use '://' authority
            // These schemes use 'scheme:path' format instead of 'scheme://host/path'
            return uri.StartsWith("urn:", StringComparison.OrdinalIgnoreCase) ||
                   uri.StartsWith("tag:", StringComparison.OrdinalIgnoreCase) ||
                   uri.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                   uri.StartsWith("tel:", StringComparison.OrdinalIgnoreCase);
        }
    }
}
