using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.DependencyInjection;
using JsonSchemaValidation.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Common
{
    public class SchemaValidatorFactory : ISchemaValidatorFactory
    {
        private readonly ISchemaRepository _schemaRepository;
        private readonly Dictionary<string, ISchemaDraftValidatorFactory> _draftFactories;

        public SchemaValidatorFactory(
            ISchemaRepository schemaRepository, 
            IEnumerable<ISchemaDraftValidatorFactory> draftFactories
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
            return CreateValidator(schemaMetaData);
        }

        public ISchemaValidator CreateValidator(SchemaMetadata schemaMetaData)
        {
            string version = schemaMetaData.DraftVersion!;
            if (!_draftFactories.TryGetValue(version, out ISchemaDraftValidatorFactory? draftFactory))
            {
                throw new NotImplementedException($"Validator for draft version {version} is not implemented.");
            }

            // Create and return the validator using the draft-specific factory
            return draftFactory.CreateValidator(schemaMetaData);
        }
    }
}
