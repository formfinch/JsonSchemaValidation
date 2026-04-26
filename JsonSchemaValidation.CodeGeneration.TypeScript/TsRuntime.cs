// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using FormFinch.JsonSchemaValidation.CodeGeneration.JavaScript.Runtime;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript;

/// <summary>
/// Accessor for the TypeScript migration runtime source.
/// In this phase the runtime is valid TypeScript derived from the stable JS ABI,
/// keeping JS and TS paths byte-for-byte comparable after tsc erases comments.
/// </summary>
public static class TsRuntime
{
    private const string JavaScriptRuntimeHeader = "// jsv-runtime.js";

    /// <summary>
    /// The filename used for the TypeScript runtime source.
    /// </summary>
    public const string FileName = "jsv-runtime.ts";

    /// <summary>
    /// The declaration filename used when compiling validators without emitting
    /// the runtime module.
    /// </summary>
    public const string DeclarationFileName = "jsv-runtime.d.ts";

    /// <summary>
    /// Returns the runtime module source as TypeScript.
    /// </summary>
    public static string GetSource()
    {
        var source = JsRuntime.GetSource();
        if (!source.Contains(JavaScriptRuntimeHeader, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The JS runtime header marker '{JavaScriptRuntimeHeader}' was not found. " +
                "Update the TypeScript runtime projection instead of silently omitting @ts-nocheck.");
        }

        return "// @ts-nocheck" + Environment.NewLine +
            source.Replace(JavaScriptRuntimeHeader, "// jsv-runtime.ts", StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns declarations for validator-only compilation when the consumer
    /// supplies the runtime module separately.
    /// </summary>
    public static string GetDeclarationSource()
    {
        return """
            export function escapeJsonPointer(segment: unknown): string;
            export function getCachedRegex(pattern: string): RegExp;
            export function graphemeLength(value: string): number;
            export function isInteger(value: unknown): boolean;
            export function deepEquals(a: unknown, b: unknown): boolean;
            export class EvaluatedState {
              clone(): EvaluatedState;
              reset(): void;
              restoreFrom(other: EvaluatedState): void;
              mergeFrom(other: EvaluatedState): void;
              markPropertyEvaluated(location: string, name: string): void;
              markItemEvaluated(location: string, index: number): void;
              setEvaluatedItemsUpTo(location: string, count: number): void;
            }
            export class Registry {
              registerForUri(uri: string, validator: unknown): void;
              tryGetValidator(uri: string): unknown;
            }
            export class CompiledValidatorScope {
              static empty: CompiledValidatorScope;
            }
            export function isValidDate(value: string): boolean;
            export function isValidTime(value: string): boolean;
            export function isValidDateTime(value: string): boolean;
            export function isValidDuration(value: string): boolean;
            export function isValidEmail(value: string): boolean;
            export function isValidIdnEmail(value: string): boolean;
            export function isValidHostname(value: string): boolean;
            export function isValidIdnHostname(value: string): boolean;
            export function isValidIpv4(value: string): boolean;
            export function isValidIpv6(value: string): boolean;
            export function isValidUri(value: string): boolean;
            export function isValidUriReference(value: string): boolean;
            export function isValidIri(value: string): boolean;
            export function isValidIriReference(value: string): boolean;
            export function isValidUriTemplate(value: string): boolean;
            export function isValidJsonPointer(value: string): boolean;
            export function isValidRelativeJsonPointer(value: string): boolean;
            export function isValidRegex(value: string): boolean;
            export function isValidUuid(value: string): boolean;
            """;
    }
}
