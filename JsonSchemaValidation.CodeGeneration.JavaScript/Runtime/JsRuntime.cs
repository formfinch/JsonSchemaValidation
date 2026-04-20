// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Reflection;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime;

/// <summary>
/// Accessor for the embedded jsv-runtime.js module source.
/// Emitted validators import from this runtime; CLI writes it next to generated files,
/// tests feed it to the Jint harness. Both paths use the same bytes.
/// </summary>
public static class JsRuntime
{
    private const string ResourceName =
        "FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime.jsv-runtime.js";

    /// <summary>
    /// The default filename validators expect the runtime to be saved as.
    /// Matches the default JsSchemaCodeGenerator.RuntimeImportSpecifier.
    /// </summary>
    public const string FileName = "jsv-runtime.js";

    /// <summary>
    /// Returns the runtime module source as a UTF-8 string.
    /// </summary>
    public static string GetSource()
    {
        var assembly = typeof(JsRuntime).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded runtime resource not found: {ResourceName}. " +
                "Build configuration issue — confirm Runtime/jsv-runtime.js is listed as EmbeddedResource.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
