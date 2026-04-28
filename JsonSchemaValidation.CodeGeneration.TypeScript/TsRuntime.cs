// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.
using System.Reflection;

namespace FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript;

/// <summary>
/// Accessor for the authored TypeScript runtime source.
/// </summary>
public static class TsRuntime
{
    private const string ResourceName =
        "FormFinch.JsonSchemaValidation.CodeGeneration.TypeScript.Runtime.jsv-runtime.ts";

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
        var assembly = typeof(TsRuntime).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded TypeScript runtime resource not found: {ResourceName}. " +
                "Build configuration issue - confirm Runtime/jsv-runtime.ts is listed as EmbeddedResource.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Returns declarations for validator-only compilation when the consumer
    /// supplies the runtime module separately.
    /// </summary>
    public static string GetDeclarationSource()
    {
        return """
            export type JsonObject = { [key: string]: JsonValue };
            export type JsonArray = JsonValue[];
            export type JsonValue = null | boolean | number | string | JsonArray | JsonObject;
            export type JsonPointer = string;
            export type ValidatorFn = (data: JsonValue, registry?: ValidatorRegistry) => boolean;
            export interface FragmentValidator {
              validate(data: JsonValue, registry?: ValidatorRegistry): boolean;
              validateWithState?(data: JsonValue, evaluatedState: EvaluatedState, location?: JsonPointer, registry?: ValidatorRegistry): boolean;
              validateWithScope?(data: JsonValue, scope: CompiledValidatorScope, location?: JsonPointer, registry?: ValidatorRegistry): boolean;
              validateWithScopeAndState?(data: JsonValue, scope: CompiledValidatorScope, evaluatedState: EvaluatedState, location?: JsonPointer, registry?: ValidatorRegistry): boolean;
            }
            export interface ValidatorModule extends FragmentValidator {
              schemaUri: string | null;
              fragmentValidators: Record<string, FragmentValidator>;
            }
            export type ValidatorHandle = ValidatorFn | FragmentValidator | ValidatorModule;
            export type ValidatorRegistry = { tryGetValidator(uri: string): ValidatorHandle | null } | null;
            export type DynamicAnchorValidator = (data: JsonValue, scope: CompiledValidatorScope, evaluatedStateOrLocation?: EvaluatedState | JsonPointer, locationOrRegistry?: JsonPointer | ValidatorRegistry, registry?: ValidatorRegistry) => boolean;
            export type CompiledScopeEntry = {
              dynamicAnchors?: Record<string, DynamicAnchorValidator> | null;
              hasRecursiveAnchor?: boolean;
              rootValidator?: DynamicAnchorValidator | null;
            };
            export function escapeJsonPointer(segment: string): string;
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
              isPropertyEvaluated(location: string, name: string): boolean;
              markItemEvaluated(location: string, index: number): void;
              isItemEvaluated(location: string, index: number): boolean;
              setEvaluatedItemsUpTo(location: string, count: number): void;
            }
            export class Registry {
              registerForUri(uri: string, validator: ValidatorHandle): void;
              tryGetValidator(uri: string): ValidatorHandle | null;
            }
            export class CompiledValidatorScope {
              static empty: CompiledValidatorScope;
              push(entry: CompiledScopeEntry | null): CompiledValidatorScope;
              tryResolveDynamicAnchor(anchorName: string): DynamicAnchorValidator | null;
            }
            export function isValidDate(value: unknown): boolean;
            export function isValidTime(value: unknown): boolean;
            export function isValidDateTime(value: unknown): boolean;
            export function isValidDuration(value: unknown): boolean;
            export function isValidEmail(value: unknown): boolean;
            export function isValidIdnEmail(value: unknown): boolean;
            export function isValidHostname(value: unknown): boolean;
            export function isValidIdnHostname(value: unknown): boolean;
            export function isValidIpv4(value: unknown): boolean;
            export function isValidIpv6(value: unknown): boolean;
            export function isValidUri(value: unknown): boolean;
            export function isValidUriReference(value: unknown): boolean;
            export function isValidIri(value: unknown): boolean;
            export function isValidIriReference(value: unknown): boolean;
            export function isValidUriTemplate(value: unknown): boolean;
            export function isValidJsonPointer(value: unknown): boolean;
            export function isValidRelativeJsonPointer(value: unknown): boolean;
            export function isValidRegex(value: unknown): boolean;
            export function isValidUuid(value: unknown): boolean;
            """;
    }
}
