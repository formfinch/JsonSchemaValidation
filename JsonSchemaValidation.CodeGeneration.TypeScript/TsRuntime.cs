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
            export type DynamicAnchorValidator = ((data: JsonValue, scope: CompiledValidatorScope, evaluatedState: EvaluatedState, location?: JsonPointer, registry?: ValidatorRegistry) => boolean) & { ignoresEvaluatedState?: boolean };
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
            /** Shared sentinel for generated dynamic-anchor delegates that explicitly ignore evaluated state. */
            export const EMPTY_EVALUATED_STATE: EvaluatedState;
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
            export function isValidIso13616Iban(value: unknown): boolean;
            export function isValidIso9362Bic(value: unknown): boolean;
            export function isValidIso2108Isbn(value: unknown): boolean;
            export function isValidNlBsn(value: unknown): boolean;
            export function isValidNlVat(value: unknown): boolean;
            export function isValidNlKvk(value: unknown): boolean;
            export function isValidNlPostcode(value: unknown): boolean;
            export function isValidNlPhone(value: unknown): boolean;
            export function isValidBePhone(value: unknown): boolean;
            export function isValidItuE164Phone(value: unknown): boolean;
            export function isValidBePostcode(value: unknown): boolean;
            export function isValidNlPlate(value: unknown): boolean;
            export function isValidGbPhone(value: unknown): boolean;
            export function isValidGbPostcode(value: unknown): boolean;
            export function isValidGbNhs(value: unknown): boolean;
            export function isValidGbVat(value: unknown): boolean;
            export function isValidGbCrn(value: unknown): boolean;
            export function isValidGbSortCode(value: unknown): boolean;
            export function isValidGbAccountNumber(value: unknown): boolean;
            export function isValidGbNino(value: unknown): boolean;
            export function isValidGbUpn(value: unknown): boolean;
            export function isValidGbPlate(value: unknown): boolean;
            export function isValidDePhone(value: unknown): boolean;
            export function isValidDePostcode(value: unknown): boolean;
            export function isValidDeVat(value: unknown): boolean;
            export function isValidDeIdnr(value: unknown): boolean;
            export function isValidDeStnr(value: unknown): boolean;
            export function isValidDeTradeRegister(value: unknown): boolean;
            export function isValidDeLeitweg(value: unknown): boolean;
            export function isValidDeRvnr(value: unknown): boolean;
            export function isValidDeKvnr(value: unknown): boolean;
            export function isValidDeIdCard(value: unknown): boolean;
            export function isValidDePassport(value: unknown): boolean;
            export function isValidDeWkn(value: unknown): boolean;
            export function isValidDePlate(value: unknown): boolean;
            export function isValidAtPhone(value: unknown): boolean;
            export function isValidAtPostcode(value: unknown): boolean;
            export function isValidAtVat(value: unknown): boolean;
            export function isValidAtSvnr(value: unknown): boolean;
            export function isValidAtFn(value: unknown): boolean;
            export function isValidAtTin(value: unknown): boolean;
            export function isValidAtPlate(value: unknown): boolean;
            export function isValidAtPassport(value: unknown): boolean;
            export function isValidLiPhone(value: unknown): boolean;
            export function isValidLiPostcode(value: unknown): boolean;
            export function isValidLiPeid(value: unknown): boolean;
            export function isValidLiPlate(value: unknown): boolean;
            export function isValidChPhone(value: unknown): boolean;
            export function isValidChPostcode(value: unknown): boolean;
            export function isValidChUid(value: unknown): boolean;
            export function isValidChVat(value: unknown): boolean;
            export function isValidChAhv(value: unknown): boolean;
            export function isValidChQrReference(value: unknown): boolean;
            export function isValidChQrIban(value: unknown): boolean;
            export function isValidChPlate(value: unknown): boolean;
            export function isValidChPassport(value: unknown): boolean;
            export function isValidLuPhone(value: unknown): boolean;
            export function isValidLuPostcode(value: unknown): boolean;
            export function isValidLuVat(value: unknown): boolean;
            export function isValidLuMatricule(value: unknown): boolean;
            export function isValidLuRcs(value: unknown): boolean;
            export function isValidLuPlate(value: unknown): boolean;
            """;
    }
}
