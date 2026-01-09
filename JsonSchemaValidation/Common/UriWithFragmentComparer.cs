namespace JsonSchemaValidation.Common
{
    public class UriWithFragmentComparer : IEqualityComparer<Uri>
    {
        public bool Equals(Uri? x, Uri? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            // Compare URIs considering their fragments
            return string.Equals(x.GetLeftPart(UriPartial.Path), y.GetLeftPart(UriPartial.Path), StringComparison.Ordinal) &&
                   string.Equals(x.Fragment, y.Fragment, StringComparison.Ordinal);
        }

        public int GetHashCode(Uri obj)
        {
            // Compute a hash code that includes the fragment
            int hashPath = StringComparer.Ordinal.GetHashCode(obj.GetLeftPart(UriPartial.Path));

            string fragment = obj.Fragment ?? string.Empty;
            int hashFragment = StringComparer.Ordinal.GetHashCode(fragment);

            return hashPath ^ hashFragment; // You can use a different hash combining strategy if desired
        }
    }
}
