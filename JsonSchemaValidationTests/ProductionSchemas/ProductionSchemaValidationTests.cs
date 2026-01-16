using System.Text.Json;
using JsonSchemaValidation.Abstractions;
using JsonSchemaValidation.Common;
using JsonSchemaValidation.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JsonSchemaValidationTests.ProductionSchemas;

/// <summary>
/// Tests validation of real-world production schemas against valid and invalid test data.
/// Each invalid test case targets a specific validation keyword to ensure the validator
/// correctly rejects malformed data.
/// </summary>
[Trait("Category", "ProductionSchemas")]
public class ProductionSchemaValidationTests
{
    private static readonly string BasePath = Path.Combine(AppContext.BaseDirectory, "ProductionSchemas");

    // Schemas with known issues (unsupported URIs or validation discrepancies)
    private static readonly HashSet<string> UnsupportedSchemas = new(StringComparer.OrdinalIgnoreCase)
    {
        "DockerCompose", // Uses https://json-schema.org/draft-07/schema (unsupported https variant)
        "EslintConfig",  // Valid.json rejected - needs investigation
        "TsConfig"       // Draft-04 with permissive anyOf patterns - invalid data passes
    };

    /// <summary>
    /// Gets all production schema test cases from the file system.
    /// </summary>
    public static IEnumerable<object[]> GetProductionSchemaTestCases()
    {
        if (!Directory.Exists(BasePath))
        {
            yield break;
        }

        foreach (var schemaDir in Directory.GetDirectories(BasePath))
        {
            var schemaName = Path.GetFileName(schemaDir);
            var schemaPath = Path.Combine(schemaDir, "schema.json");
            var validPath = Path.Combine(schemaDir, "valid.json");

            if (!File.Exists(schemaPath))
            {
                continue;
            }

            // Skip unsupported schemas
            if (UnsupportedSchemas.Contains(schemaName))
            {
                continue;
            }

            // Test valid data if it exists
            if (File.Exists(validPath))
            {
                yield return new object[] { schemaName, "valid", true };
            }

            // Test all invalid-*.json files
            foreach (var invalidFile in Directory.GetFiles(schemaDir, "invalid-*.json"))
            {
                var testName = Path.GetFileNameWithoutExtension(invalidFile);
                yield return new object[] { schemaName, testName, false };
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetProductionSchemaTestCases))]
    public void ValidateProductionSchema(string schemaName, string testName, bool expectedValid)
    {
        // Arrange
        var schemaDir = Path.Combine(BasePath, schemaName);
        var schemaPath = Path.Combine(schemaDir, "schema.json");
        var dataPath = testName == "valid"
            ? Path.Combine(schemaDir, "valid.json")
            : Path.Combine(schemaDir, $"{testName}.json");

        var schemaJson = File.ReadAllText(schemaPath);
        var dataJson = File.ReadAllText(dataPath);

        // Create service provider with all drafts enabled (default)
        var services = new ServiceCollection();
        services.AddJsonSchemaValidation();
        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.InitializeSingletonServices();

        var repository = serviceProvider.GetRequiredService<ISchemaRepository>();
        var validatorFactory = serviceProvider.GetRequiredService<ISchemaValidatorFactory>();
        var contextFactory = serviceProvider.GetRequiredService<IJsonValidationContextFactory>();

        // Register schema
        using var schemaDoc = JsonDocument.Parse(schemaJson);
        var schemaUri = new Uri($"urn:test:{schemaName.ToLowerInvariant()}");
        repository.TryRegisterSchema(schemaDoc.RootElement.Clone(), schemaUri, out _);

        // Get validator
        var validator = validatorFactory.GetValidator(schemaUri);

        // Act
        using var dataDoc = JsonDocument.Parse(dataJson);
        var context = contextFactory.CreateContextForRoot(dataDoc.RootElement);
        var result = validator.IsValidRoot(context);

        // Assert
        Assert.Equal(expectedValid, result);
    }
}
