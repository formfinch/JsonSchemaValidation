using JsonSchemaValidation.Abstractions.Keywords;
using JsonSchemaValidation.Draft202012.Keywords;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Draft202012
{
    public static class JsonSchemaDraft
    {
        private static readonly IEnumerable<IKeywordValidatorFactory> factories = new IKeywordValidatorFactory[]
        {
            new MinimumValidatorFactory(),
            new MaximumValidatorFactory()
        };

        public static IEnumerable<IKeywordValidatorFactory> Factories
        {
            get {
                return factories;
            }
        }
    }
}
