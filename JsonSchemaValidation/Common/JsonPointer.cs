// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text;
using System.Threading;

namespace FormFinch.JsonSchemaValidation.Common
{
    /// <summary>
    /// Represents a JSON Pointer per RFC 6901.
    /// Immutable - all operations return new instances.
    /// </summary>
    /// <remarks>
    /// <b>Thread safety:</b> Instances are immutable and safe for concurrent use.
    /// The string representation is cached lazily in a thread-safe manner.
    /// </remarks>
    public sealed class JsonPointer
    {
        private readonly string[] _segments;
        private string? _cachedString;

        /// <summary>
        /// Gets the empty JSON Pointer, representing the root of the document.
        /// </summary>
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
            var cached = _cachedString;
            if (cached != null)
            {
                return cached;
            }

            if (_segments.Length == 0)
            {
                Interlocked.CompareExchange(ref _cachedString, "", null);
                return _cachedString!;
            }

            var sb = new StringBuilder();
            foreach (var segment in _segments)
            {
                sb.Append('/');
                AppendEscaped(sb, segment);
            }
            var value = sb.ToString();
            Interlocked.CompareExchange(ref _cachedString, value, null);
            return _cachedString!;
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

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is JsonPointer other)
            {
                return _segments.SequenceEqual(other._segments, StringComparer.Ordinal);
            }
            return false;
        }

        /// <inheritdoc />
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
