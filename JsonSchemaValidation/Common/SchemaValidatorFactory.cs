using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Common
{
    public class SchemaValidatorFactory : ISchemaValidatorFactory
    {
        private readonly ISchemaRepository _schemaRepository;
        private readonly Dictionary<string, ISchemaDraftFactory> _draftFactories;

        public SchemaValidatorFactory(
            ISchemaRepository schemaRepository, 
            IEnumerable<ISchemaDraftFactory> draftFactories
        )
        {
            _schemaRepository = schemaRepository;
            _draftFactories = draftFactories.ToDictionary(
                draftFactory => draftFactory.DraftVersion,
                draftFactory => draftFactory);
        }

        public ISchemaValidator GetValidator(Uri schemaUri)
        {
            var schemaMetaData = _schemaRepository.GetSchema(schemaUri);
            string version = schemaMetaData.DraftVersion!;
            if (!_draftFactories.TryGetValue(version, out ISchemaDraftFactory? draftFactory))
            {
                throw new NotImplementedException($"Validator for draft version {version} is not implemented.");
            }

            // Create and return the validator using the draft-specific factory
            return draftFactory.CreateValidator(schemaMetaData.Schema);
        }
    }
}
