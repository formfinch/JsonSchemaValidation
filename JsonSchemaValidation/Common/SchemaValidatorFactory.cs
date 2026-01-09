using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Draft202012.Keywords.Logic;
using JsonSchemaValidation.Repositories;

namespace JsonSchemaValidation.Common
{
    public class SchemaValidatorFactory : ISchemaValidatorFactory
    {
        private readonly ISchemaFactory _schemaFactory;
        private readonly ISchemaRepository _schemaRepository;
        private readonly Dictionary<string, ISchemaDraftValidatorFactory> _draftFactories;

        public SchemaValidatorFactory(
            ISchemaFactory schemaFactory,
            ISchemaRepository schemaRepository,
            IEnumerable<ISchemaDraftValidatorFactory> draftFactories
        )
        {
            _schemaFactory = schemaFactory;
            _schemaRepository = schemaRepository;
            _draftFactories = draftFactories.ToDictionary(
                draftFactory => draftFactory.DraftVersion,
                draftFactory => draftFactory,
                StringComparer.Ordinal);
        }

        public ISchemaValidator GetValidator(Uri schemaUri)
        {
            var schemaMetaData = _schemaRepository.GetSchema(schemaUri);
            var dereferencedSchema = _schemaFactory.CreateDereferencedSchema(schemaMetaData);
            var validator = CreateValidator(dereferencedSchema);

            // Wrap with scope awareness to push the root schema resource
            return new ScopeAwareSchemaValidator(validator, schemaMetaData);
        }

        public ISchemaValidator CreateValidator(SchemaMetadata schemaMetaData)
        {
            string version = schemaMetaData.DraftVersion!;
            if (!_draftFactories.TryGetValue(version, out ISchemaDraftValidatorFactory? draftFactory))
            {
                throw new NotSupportedException($"Validator for draft version {version} is not supported.");
            }

            // Create the validator using the draft-specific factory
            var validator = draftFactory.CreateValidator(schemaMetaData);

            // Check if this schema has its own $id (making it a distinct schema resource)
            // If so, wrap with ScopeAwareSchemaValidator to manage the dynamic scope
            var schemaId = schemaMetaData.Schema.GetIdProperty();
            if (!string.IsNullOrEmpty(schemaId))
            {
                return new ScopeAwareSchemaValidator(validator, schemaMetaData);
            }

            return validator;
        }
    }
}
