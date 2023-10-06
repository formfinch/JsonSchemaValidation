using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace JsonSchemaValidationTests.TestCases
{
    internal class TestCaseLoader
    {
        private readonly IEnumerable<string> descriptions;

        public TestCaseLoader(IEnumerable<string> descriptions)
        {
            this.descriptions = descriptions;
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
                        if (descriptions == null || descriptions.Contains(test.Description))
                        {
                            yield return new object[] { test };
                        }
                    }
                }
            }
        }
    }
}
