using System.Text;

namespace JsonSchemaValidation.Common
{
    /// <summary>
    /// Represents a JSON Pointer per RFC 6901.
    /// Immutable - all operations return new instances.
    /// </summary>
    public sealed class JsonPointer
    {
        private readonly string[] _segments;
        private string? _cachedString;

        public static JsonPointer Empty { get; } = new([]);

        private JsonPointer(string[] segments)
        {
            _segments = segments;
        }

        /// <summary>
        /// Appends a property name segment to the pointer.
        /// </summary>
        public JsonPointer Append(string segment)
        {
            var newSegments = new string[_segments.Length + 1];
            _segments.CopyTo(newSegments, 0);
            newSegments[_segments.Length] = segment;
            return new JsonPointer(newSegments);
        }

        /// <summary>
        /// Appends an array index segment to the pointer.
        /// </summary>
        public JsonPointer Append(int index)
        {
            return Append(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the parent pointer (removes the last segment).
        /// Returns Empty if this is already empty or has only one segment.
        /// </summary>
        public JsonPointer Parent()
        {
            if (_segments.Length <= 1)
            {
                return Empty;
            }

            var newSegments = new string[_segments.Length - 1];
            Array.Copy(_segments, newSegments, _segments.Length - 1);
            return new JsonPointer(newSegments);
        }

        /// <summary>
        /// Returns the JSON Pointer string representation (e.g., "/foo/0/bar").
        /// Empty pointer returns empty string. Result is cached for performance.
        /// </summary>
        public override string ToString()
        {
            if (_cachedString != null)
            {
                return _cachedString;
            }

            if (_segments.Length == 0)
            {
                _cachedString = "";
                return _cachedString;
            }

            var sb = new StringBuilder();
            foreach (var segment in _segments)
            {
                sb.Append('/');
                AppendEscaped(sb, segment);
            }
            _cachedString = sb.ToString();
            return _cachedString;
        }

        /// <summary>
        /// Appends an escaped segment to the StringBuilder.
        /// </summary>
        private static void AppendEscaped(StringBuilder sb, string segment)
        {
            foreach (var c in segment)
            {
                switch (c)
                {
                    case '~':
                        sb.Append("~0");
                        break;
                    case '/':
                        sb.Append("~1");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
        }

        /// <summary>
        /// Parses a JSON Pointer string into a JsonPointer instance.
        /// </summary>
        public static JsonPointer Parse(string pointer)
        {
            if (string.IsNullOrEmpty(pointer))
            {
                return Empty;
            }

            if (!pointer.StartsWith('/'))
            {
                throw new ArgumentException("JSON Pointer must start with '/' or be empty", nameof(pointer));
            }

            var parts = pointer[1..].Split('/');
            var segments = new string[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                segments[i] = Unescape(parts[i]);
            }
            return new JsonPointer(segments);
        }

        /// <summary>
        /// Unescapes special characters per RFC 6901:
        /// ~1 becomes /
        /// ~0 becomes ~
        /// </summary>
        private static string Unescape(string segment)
        {
            return segment.Replace("~1", "/").Replace("~0", "~");
        }

        public override bool Equals(object? obj)
        {
            if (obj is JsonPointer other)
            {
                return _segments.SequenceEqual(other._segments, StringComparer.Ordinal);
            }
            return false;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var segment in _segments)
            {
                hash.Add(segment);
            }
            return hash.ToHashCode();
        }
    }
}
