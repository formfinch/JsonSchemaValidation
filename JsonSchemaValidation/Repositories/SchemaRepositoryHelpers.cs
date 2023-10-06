using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonSchemaValidation.Repositories
{
    public class SchemaRepositoryHelpers
    {
        public static Uri GenerateRandomSchemaId()
        {
            var guid = Guid.NewGuid();
            return new Uri($"urn:schema:{guid}");
        }
    }
}
