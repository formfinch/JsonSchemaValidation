// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace FormFinch.JsonSchemaValidationTests.TestCases
{
    internal class TestCaseLoader
    {
        private readonly IEnumerable<string> keywords;

        public TestCaseLoader(IEnumerable<string> keywords)
        {
            this.keywords = keywords ?? throw new ArgumentNullException(nameof(keywords));
        }

        public IEnumerable<object[]> LoadTestCases(string path)
        {
            var files = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                var tests = JsonSerializer.Deserialize<List<TestCase>>(content);
                if (tests is not null) // Check for null just to be safe
                {
                    foreach (var test in tests)
                    {
                        var normalizedFile = file.Replace('\\', '/');
                    var keyword = keywords!.FirstOrDefault(kw =>
                            kw != null
                            && normalizedFile.EndsWith($"{kw.Replace('\\', '/')}.json", StringComparison.CurrentCultureIgnoreCase)
                            && kw.Replace('\\', '/').EndsWith(Path.GetFileNameWithoutExtension(file)));
                        if (keyword != null)
                        {
                            yield return new object[] { test };
                        }
                    }
                }
            }
        }
    }
}
