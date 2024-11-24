namespace JsonSchemaValidation.Common
{
    public class UriWithFragmentComparer : IEqualityComparer<Uri>
    {
        public bool Equals(Uri? x, Uri? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            // Compare URIs considering their fragments
            return x.GetLeftPart(UriPartial.Path) == y.GetLeftPart(UriPartial.Path) &&
                   x.Fragment == y.Fragment;
        }

        public int GetHashCode(Uri obj)
        {
            // Compute a hash code that includes the fragment
            int hashPath = obj.GetLeftPart(UriPartial.Path).GetHashCode();

            string fragment = (obj.Fragment ?? string.Empty);
            int hashFragment = fragment.GetHashCode();

            return hashPath ^ hashFragment; // You can use a different hash combining strategy if desired
        }
    }
}
