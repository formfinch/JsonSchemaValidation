using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace JsonSchemaValidation.Common
{
    internal class JsonElementComparison
    {
        public bool DeepEquals(JsonElement src, JsonElement trg)
        {
            if (src.ValueKind != trg.ValueKind)
            {
                return false;
            }

            switch (src.ValueKind)
            {
                case JsonValueKind.Object:
                    return CompareObjects(src, trg);
                case JsonValueKind.Array:
                    return CompareArrays(src, trg);
                case JsonValueKind.String:
                    return CompareStrings(src, trg);
                case JsonValueKind.Number:
                    return CompareNumbers(src, trg);
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    return true;  // For these types, reaching here implies they are the same since their ValueKind matched
                default:
                    throw new System.InvalidOperationException($"Unknown JsonValueKind: {src.ValueKind}");
            }
        }

        private bool CompareObjects(JsonElement src, JsonElement trg)
        {
            if (src.GetRawText() == trg.GetRawText()) return true;

            var srcProperties = src.EnumerateObject();
            var trgProperties = trg.EnumerateObject();

            var srcCount = 0;
            foreach (var prop in srcProperties)
            {
                srcCount++;
                if (!trg.TryGetProperty(prop.Name, out var trgValue))
                {
                    return false; // Property exists in src but not in trg
                }

                if (!DeepEquals(prop.Value, trgValue))
                {
                    return false; // Values for this property are not the same
                }
            }

            var trgCount = 0;
            foreach (var _ in trgProperties) trgCount++;

            if (srcCount != trgCount)
            {
                return false; // Different number of properties
            }

            return true;
        }

        private bool CompareArrays(JsonElement src, JsonElement trg)
        {
            if (src.GetRawText() == trg.GetRawText()) return true;

            var srcArray = src.EnumerateArray();
            var trgArray = trg.EnumerateArray();

            var srcArrayCount = 0;
            var trgArrayCount = 0;
            while (srcArray.MoveNext())
            {
                srcArrayCount++;
                if (!trgArray.MoveNext())
                {
                    return false;
                }

                trgArrayCount++;
                if (!DeepEquals(srcArray.Current, trgArray.Current))
                {
                    return false;
                }
            }

            if (trgArray.MoveNext()) trgArrayCount++;

            if (srcArrayCount != trgArrayCount)
            {
                return false;
            }

            return true;
        }

        private bool CompareStrings(JsonElement src, JsonElement trg)
        {
            return src.GetString() == trg.GetString();
        }

        private bool CompareNumbers(JsonElement src, JsonElement trg)
        {
            return src.GetDecimal() == trg.GetDecimal();
        }
    }
}
