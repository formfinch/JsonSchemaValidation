// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// URI validation logic shared by uri, uri-reference, iri, iri-reference, and uri-template validators.

namespace FormFinch.JsonSchemaValidation.Draft7.Keywords.Format
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

            // RFC 3986: Check for characters that must be percent-encoded
            // These characters are not allowed unencoded in URIs (except in specific contexts)
            // Skip this check for URI templates (expander != null) since { and } are valid template syntax
            if (!iriSupport && expander == null && ContainsInvalidUriCharacters(uri))
            {
                return false;
            }

            if (canBeRelative)
            {
                if (uri.StartsWith("//", StringComparison.Ordinal))
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
                // Validate template syntax: braces must be balanced
                if (!HasBalancedBraces(uri))
                {
                    return false;
                }
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
        /// Checks for characters that are invalid in URIs per RFC 3986.
        /// These must be percent-encoded if used.
        /// </summary>
        private static bool ContainsInvalidUriCharacters(string uri)
        {
            foreach (char c in uri)
            {
                // Non-ASCII characters must be percent-encoded in URIs (but allowed in IRIs)
                if (c > 127)
                    return true;

                // Characters that must always be percent-encoded (RFC 3986 Section 2.4)
                // Space, <, >, ", {, }, |, \, ^, `
                if (c == ' ' || c == '<' || c == '>' || c == '"' ||
                    c == '{' || c == '}' || c == '|' || c == '\\' ||
                    c == '^' || c == '`')
                    return true;
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

        /// <summary>
        /// Checks if braces are balanced in a URI template (RFC 6570).
        /// Each '{' must have a matching '}' and nesting is not allowed.
        /// </summary>
        private static bool HasBalancedBraces(string template)
        {
            int depth = 0;
            foreach (char c in template)
            {
                if (c == '{')
                {
                    depth++;
                    // RFC 6570 doesn't allow nested braces
                    if (depth > 1)
                    {
                        return false;
                    }
                }
                else if (c == '}')
                {
                    depth--;
                    // More closing braces than opening
                    if (depth < 0)
                    {
                        return false;
                    }
                }
            }
            // All braces must be closed
            return depth == 0;
        }
    }
}
