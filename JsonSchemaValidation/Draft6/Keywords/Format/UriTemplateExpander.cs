// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// Draft behavior: Identical in Draft 4, Draft 6, Draft 7, Draft 2019-09, Draft 2020-12
// URI template expansion logic for RFC 6570 uri-template format validation.

using System.Text;
using System.Text.RegularExpressions;

namespace FormFinch.JsonSchemaValidation.Draft6.Keywords.Format
{
    internal class UriTemplateExpander
    {
        private static readonly TimeSpan defaultMatchTimeout = TimeSpan.FromSeconds(3);

        private static readonly string expressionPattern = @"\{(?<operator>[+#./;?&=,!@|]?)?(?<variableList>[^}]+)\}";
        private static readonly string variableListPattern = @"(?<varspec>[^,}]+)";
        private static readonly string varspecPattern = @"(?<varname>[a-z0-9_%.]+)(?<modifierLevel4>:[0-9]{1,4}|[*])?";

        private readonly Regex expressionRegex;
        private readonly Regex variableListRegex;
        private readonly Regex varspecRegex;

        public UriTemplateExpander()
        {
            var options = RegexOptions.IgnoreCase;
            expressionRegex = new Regex(expressionPattern, options, defaultMatchTimeout);
            variableListRegex = new Regex(variableListPattern, options, defaultMatchTimeout);
            varspecRegex = new Regex(varspecPattern, options, defaultMatchTimeout);
        }

        public string ExpandTemplate(string uriTemplate)
        {
            var expressionMatches = expressionRegex.Matches(uriTemplate);
            StringBuilder expandedTemplate = new StringBuilder(uriTemplate);

            foreach (Match expressionMatch in expressionMatches)
            {
                string operatorGroup = expressionMatch.Groups["operator"].Value;
                string variableList = expressionMatch.Groups["variableList"].Value;

                string separator = DetermineSeparator(operatorGroup);
                string prefix = DeterminePrefix(operatorGroup);

                List<string> variables = new List<string>();
                foreach (Match varspecMatch in variableListRegex.Matches(variableList))
                {
                    string varspec = varspecMatch.Groups["varspec"].Value;
                    variables.Add(ProcessVarSpec(varspec, operatorGroup, separator));
                }

                if (variables.Count > 0)
                {
                    StringBuilder replaceText = new(prefix);
                    replaceText.Append(string.Join(separator, variables));
                    expandedTemplate.Replace(expressionMatch.Value, replaceText.ToString());
                }
            }
            return expandedTemplate.ToString();
        }

        private static string DetermineSeparator(string operatorGroup)
        {
            return operatorGroup switch
            {
                "." => ".",
                "/" => "/",
                ";" => ";",
                "?" => "&",
                "&" => "&",
                _ => ","
            };
        }

        private static string DeterminePrefix(string operatorGroup)
        {
            return operatorGroup switch
            {
                "#" => "#",
                "/" => "/",
                "?" => "?",
                "&" => "&",
                _ => ""
            };
        }

        private string ProcessVarSpec(string varspec, string operatorGroup, string separator)
        {
            bool assignmentStyle = string.Equals(operatorGroup, ";", StringComparison.Ordinal) || string.Equals(operatorGroup, "?", StringComparison.Ordinal) || string.Equals(operatorGroup, "&", StringComparison.Ordinal);

            var varspecDetails = varspecRegex.Match(varspec);
            string varname = varspecDetails.Groups["varname"].Value;
            string modifierLevel4 = varspecDetails.Groups["modifierLevel4"].Value;
            bool listExpansion = string.Equals(modifierLevel4, "*", StringComparison.Ordinal);

            if (assignmentStyle && listExpansion)
            {
                return $"var1=one{separator}var2=two{separator}var3=three";
            }

            if (assignmentStyle && !listExpansion)
            {
                return $"{varname}=value";
            }

            if (!assignmentStyle && listExpansion)
            {
                return $"one{separator}two{separator}three";
            }

            return "value";
        }
    }
}
