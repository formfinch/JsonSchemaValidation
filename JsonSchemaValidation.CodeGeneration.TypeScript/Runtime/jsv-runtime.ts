// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// jsv-runtime.ts
//
// Shared runtime for TypeScript validators emitted by jsv-codegen.
// This file is authored TypeScript and is part of the generated
// validator ABI.

export type JsonObject = { [key: string]: JsonValue };
export type JsonArray = JsonValue[];
export type JsonValue = null | boolean | number | string | JsonArray | JsonObject;
export type JsonPointer = string;

export type ValidatorFn = (data: JsonValue, registry?: ValidatorRegistry) => boolean;

export interface FragmentValidator {
    validate(data: JsonValue, registry?: ValidatorRegistry): boolean;
    validateWithState?(data: JsonValue, evaluatedState: EvaluatedState, location?: JsonPointer, registry?: ValidatorRegistry): boolean;
    validateWithScope?(data: JsonValue, scope: CompiledValidatorScope, location?: JsonPointer, registry?: ValidatorRegistry): boolean;
    validateWithScopeAndState?(
        data: JsonValue,
        scope: CompiledValidatorScope,
        evaluatedState: EvaluatedState,
        location?: JsonPointer,
        registry?: ValidatorRegistry): boolean;
}

export interface ValidatorModule extends FragmentValidator {
    schemaUri: string | null;
    fragmentValidators: Record<string, FragmentValidator>;
}

export type ValidatorHandle = ValidatorFn | FragmentValidator | ValidatorModule;
export type ValidatorRegistry = { tryGetValidator(uri: string): ValidatorHandle | null } | null;

type SegmentData = { segment: string };
type SegmenterLike = { segment(input: string): Iterable<SegmentData> };
type SegmenterConstructor = new (
    locale?: string | string[],
    options?: { granularity?: "grapheme" | "word" | "sentence" }
) => SegmenterLike;
type IntlWithSegmenter = typeof Intl & { Segmenter?: SegmenterConstructor };
type RegexGroups = Record<string, string | undefined>;
export type DynamicAnchorValidator = ((
    data: JsonValue,
    scope: CompiledValidatorScope,
    evaluatedState: EvaluatedState,
    location?: JsonPointer,
    registry?: ValidatorRegistry
) => boolean) & { ignoresEvaluatedState?: boolean };

export type CompiledScopeEntry = {
    dynamicAnchors?: Record<string, DynamicAnchorValidator> | null;
    hasRecursiveAnchor?: boolean;
    rootValidator?: DynamicAnchorValidator | null;
};

export function escapeJsonPointer(segment: string): string {
    return segment.replace(/~/g, "~0").replace(/\//g, "~1");
}

let _segmenter: SegmenterLike | null = null;
let _segmenterProbed = false;
const _regexCache = new Map<string, RegExp>();

function _getSegmenter(): SegmenterLike | null {
    if (_segmenterProbed) return _segmenter;
    _segmenterProbed = true;
    try {
        const segmenterCtor = (Intl as IntlWithSegmenter).Segmenter;
        if (typeof segmenterCtor === "function") {
            _segmenter = new segmenterCtor(undefined, { granularity: "grapheme" });
        }
    } catch {
        _segmenter = null;
    }
    return _segmenter;
}

export function getCachedRegex(pattern: string): RegExp {
    let regex = _regexCache.get(pattern);
    if (regex === undefined) {
        regex = new RegExp(pattern, "u");
        _regexCache.set(pattern, regex);
    }
    return regex;
}

export function graphemeLength(str: string): number {
    if (str.length === 0) return 0;
    let asciiOnly = true;
    for (let i = 0; i < str.length; i++) {
        if (str.charCodeAt(i) > 0x7F) {
            asciiOnly = false;
            break;
        }
    }
    if (asciiOnly) return str.length;

    const seg = _getSegmenter();
    if (seg !== null) {
        let count = 0;
        for (const _segment of seg.segment(str)) count++;
        return count;
    }
    return Array.from(str).length;
}

export function isInteger(v: unknown): boolean {
    if (typeof v !== "number") return false;
    if (!Number.isFinite(v)) return false;
    return Math.floor(v) === v;
}

export function deepEquals(a: unknown, b: unknown): boolean {
    if (a === b) return true;
    if (typeof a !== typeof b) return false;
    if (a === null || b === null) return a === b;
    if (Array.isArray(a)) {
        if (!Array.isArray(b)) return false;
        if (a.length !== b.length) return false;
        for (let i = 0; i < a.length; i++) {
            if (!deepEquals(a[i], b[i])) return false;
        }
        return true;
    }
    if (typeof a === "object") {
        if (Array.isArray(b) || typeof b !== "object" || b === null) return false;
        const aObject = a as Record<string, unknown>;
        const bObject = b as Record<string, unknown>;
        const ak = Object.keys(aObject);
        const bk = Object.keys(bObject);
        if (ak.length !== bk.length) return false;
        for (const k of ak) {
            if (!Object.prototype.hasOwnProperty.call(bObject, k)) return false;
            if (!deepEquals(aObject[k], bObject[k])) return false;
        }
        return true;
    }
    return false;
}

export class EvaluatedState {
    private readonly _properties = new Map<string, Set<string>>();
    private readonly _itemsUpTo = new Map<string, number>();
    private readonly _itemIndices = new Map<string, Set<number>>();

    markPropertyEvaluated(loc: JsonPointer, propertyName: string): void {
        let set = this._properties.get(loc);
        if (set === undefined) {
            set = new Set<string>();
            this._properties.set(loc, set);
        }
        set.add(propertyName);
    }

    isPropertyEvaluated(loc: JsonPointer, propertyName: string): boolean {
        return this._properties.get(loc)?.has(propertyName) === true;
    }

    markItemEvaluated(loc: JsonPointer, index: number): void {
        let set = this._itemIndices.get(loc);
        if (set === undefined) {
            set = new Set<number>();
            this._itemIndices.set(loc, set);
        }
        set.add(index);
    }

    setEvaluatedItemsUpTo(loc: JsonPointer, count: number): void {
        const current = this._itemsUpTo.get(loc) ?? 0;
        if (count > current) this._itemsUpTo.set(loc, count);
    }

    isItemEvaluated(loc: JsonPointer, index: number): boolean {
        if (index < (this._itemsUpTo.get(loc) ?? 0)) return true;
        return this._itemIndices.get(loc)?.has(index) === true;
    }

    reset(): void {
        this._properties.clear();
        this._itemsUpTo.clear();
        this._itemIndices.clear();
    }

    clone(): EvaluatedState {
        const clone = new EvaluatedState();
        for (const [loc, props] of this._properties) {
            clone._properties.set(loc, new Set<string>(props));
        }
        for (const [loc, count] of this._itemsUpTo) {
            clone._itemsUpTo.set(loc, count);
        }
        for (const [loc, indices] of this._itemIndices) {
            clone._itemIndices.set(loc, new Set<number>(indices));
        }
        return clone;
    }

    mergeFrom(other: EvaluatedState): void {
        for (const [loc, props] of other._properties) {
            let target = this._properties.get(loc);
            if (target === undefined) {
                target = new Set<string>();
                this._properties.set(loc, target);
            }
            for (const prop of props) target.add(prop);
        }
        for (const [loc, count] of other._itemsUpTo) {
            this.setEvaluatedItemsUpTo(loc, count);
        }
        for (const [loc, indices] of other._itemIndices) {
            let target = this._itemIndices.get(loc);
            if (target === undefined) {
                target = new Set<number>();
                this._itemIndices.set(loc, target);
            }
            for (const index of indices) target.add(index);
        }
    }

    restoreFrom(snapshot: EvaluatedState): void {
        this.reset();
        this.mergeFrom(snapshot);
    }
}

// Shared sentinel for generated dynamic-anchor delegates that explicitly ignore evaluated state.
export const EMPTY_EVALUATED_STATE = new EvaluatedState();

export class Registry {
    private readonly _validators = new Map<string, ValidatorHandle>();

    registerForUri(uri: string, validator: ValidatorHandle): void {
        this._validators.set(uri, validator);
    }

    tryGetValidator(uri: string): ValidatorHandle | null {
        return this._validators.get(uri) ?? null;
    }
}

export class CompiledValidatorScope {
    static readonly empty = new CompiledValidatorScope(null, null);

    private constructor(
        private readonly _parent: CompiledValidatorScope | null,
        private readonly _entry: CompiledScopeEntry | null
    ) {
    }

    push(entry: CompiledScopeEntry | null): CompiledValidatorScope {
        if (entry == null || entry.dynamicAnchors == null || Object.keys(entry.dynamicAnchors).length === 0) {
            return this;
        }
        return new CompiledValidatorScope(this, entry);
    }

    tryResolveDynamicAnchor(anchorName: string): DynamicAnchorValidator | null {
        const entries: CompiledScopeEntry[] = [];
        let current: CompiledValidatorScope | null = this;
        while (current != null) {
            if (current._entry?.dynamicAnchors != null) {
                entries.push(current._entry);
            }
            current = current._parent;
        }

        for (let i = entries.length - 1; i >= 0; i--) {
            const validator = entries[i].dynamicAnchors?.[anchorName] ?? null;
            if (validator != null) return validator;
        }

        return null;
    }
}

function _isString(v: unknown): v is string {
    return typeof v === "string";
}

const _reDateFullDate = /^(\d{4})-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$/;
const _reTimePart = /^([01][0-9]|2[0-3]):([0-5][0-9]):([0-5][0-9]|60)(?:\.[0-9]+)?(?:[zZ]|([+-])([0-9]{2}):([0-9]{2}))$/;
const _reDuration = /^P(?=.)(?:\d+W|(?:\d+Y)?(?:\d+M)?(?:\d+D)?(?:T(?=\d)(?:\d+H)?(?:\d+M)?(?:\d+(?:\.\d+)?S)?)?)$/;
const _reBasicEmail = /^.+@.+$/;
const _reLocalPart = /^(?:(?:[A-Za-z0-9!#$%&'*+\-/=?^_`{|}~]+(?:\.[A-Za-z0-9!#$%&'*+\-/=?^_`{|}~]+)*)|(?:"(?:[\sA-Za-z0-9!#$%&'*+\-\/=?^_`{|}~.,:;<>[\]\\@]|\\.)+"))$/;
const _reIdnLocalPart = new RegExp(
    "^(?:(?:[\\p{L}\\p{N}!#$%&'*+\\-/=?^_`{|}~]+(?:\\.[\\p{L}\\p{N}!#$%&'*+\\-/=?^_`{|}~]+)*)|(?:\"(?:[\\s\\p{L}\\p{N}!#$%&'*+\\-/=?^_`{|}~.,:;<>\\[\\]\\\\@]|\\\\.)+\"))$",
    "u");
const _reDomainPart = /^(?:[A-Za-z0-9.-]+\.[A-Za-z]{2,}|\[(?:(?:\d{1,3}\.){3}\d{1,3}|IPv6:[0-9A-Fa-f:.]+)\])$/;
const _reHostnameLabel = /^(?=.{1,63}$)[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?$/;
const _reCombiningMark = new RegExp("^\\p{M}$", "u");
const _reIpv4Octet = /^(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)$/;
const _reUuid = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const _reJsonPointer = /^(\/([^~/]|~0|~1)*)*$/;
const _reRelativeJsonPointer = /^(0|[1-9]\d*)(#|(\/([^~/]|~0|~1)*)*)$/;
const _reUriTemplate = /^([^\x00-\x20"'<>%\\^`{|}]|%[0-9A-Fa-f]{2}|\{[+#./;?&=,!@|]?((\w|\.|%[0-9A-Fa-f]{2})+(\*|:\d+)?)(,(\w|\.|%[0-9A-Fa-f]{2})+(\*|:\d+)?)*\})*$/;

export function isValidDate(v: unknown): boolean {
    if (!_isString(v)) return true;
    const m = _reDateFullDate.exec(v);
    if (!m) return false;
    const year = Number.parseInt(m[1], 10);
    const month = Number.parseInt(m[2], 10);
    const day = Number.parseInt(m[3], 10);
    const daysInMonth = [
        31,
        (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0 ? 29 : 28,
        31, 30, 31, 30, 31, 31, 30, 31, 30, 31
    ];
    return day <= daysInMonth[month - 1];
}

export function isValidTime(v: unknown): boolean {
    if (!_isString(v)) return true;
    const m = _reTimePart.exec(v);
    if (!m) return false;
    const groups: RegexGroups = {
        sign: m[4],
        offsetHour: m[5],
        offsetMinute: m[6]
    };
    const hour = Number.parseInt(m[1], 10);
    const minute = Number.parseInt(m[2], 10);
    const second = Number.parseInt(m[3], 10);

    if (second === 60) {
        if (!_isValidLeapSecond(hour, minute, groups)) return false;
    } else if (!_tryDate(`1970-01-01T${v}`)) {
        return false;
    }

    if (groups.sign !== undefined) {
        const offsetHours = Number.parseInt(groups.offsetHour ?? "", 10);
        const offsetMinutes = Number.parseInt(groups.offsetMinute ?? "", 10);
        if (offsetHours > 23 || offsetMinutes > 59) return false;
    }
    return true;
}

function _isValidLeapSecond(hour: number, minute: number, groups: RegexGroups): boolean {
    if (groups.sign === undefined) {
        return hour === 23 && minute === 59;
    }

    const offsetHours = Number.parseInt(groups.offsetHour ?? "", 10);
    const offsetMinutes = Number.parseInt(groups.offsetMinute ?? "", 10);
    if (offsetHours > 23 || offsetMinutes > 59) return false;

    const localMinutes = hour * 60 + minute;
    const offsetTotalMinutes = offsetHours * 60 + offsetMinutes;
    const utcMinutes = groups.sign === "+"
        ? localMinutes - offsetTotalMinutes
        : localMinutes + offsetTotalMinutes;

    return ((utcMinutes % 1440) + 1440) % 1440 === (23 * 60 + 59);
}

function _tryDate(value: string): boolean {
    try {
        return !Number.isNaN(Date.parse(value));
    } catch {
        return false;
    }
}

export function isValidDateTime(v: unknown): boolean {
    if (!_isString(v)) return true;
    const tIdx = v.search(/[Tt]/);
    if (tIdx < 0) return false;
    return isValidDate(v.slice(0, tIdx)) && isValidTime(v.slice(tIdx + 1));
}

export function isValidDuration(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _reDuration.test(v);
}

export function isValidEmail(v: unknown): boolean {
    if (!_isString(v)) return true;
    if (v.length > 254 || !_reBasicEmail.test(v)) return false;
    const atIndex = v.lastIndexOf("@");
    if (atIndex < 0) return false;
    const localPart = v.slice(0, atIndex);
    const domainPart = v.slice(atIndex + 1);
    if (!_reLocalPart.test(localPart) || !_reDomainPart.test(domainPart)) {
        return false;
    }

    if (domainPart.startsWith("[") && domainPart.endsWith("]")) {
        const address = domainPart.slice(1, -1);
        if (address.startsWith("IPv6:")) {
            return isValidIpv6(address.slice(5));
        }
        return isValidIpv4(address);
    }

    return isValidHostname(domainPart);
}

export function isValidIdnEmail(v: unknown): boolean {
    if (!_isString(v)) return true;
    if (v.length > 254 || !_reBasicEmail.test(v)) return false;
    const atIndex = v.lastIndexOf("@");
    if (atIndex < 0) return false;
    const localPart = v.slice(0, atIndex);
    const domainPart = v.slice(atIndex + 1);
    if (!_reIdnLocalPart.test(localPart)) {
        return false;
    }

    if (domainPart.startsWith("[") && domainPart.endsWith("]")) {
        const address = domainPart.slice(1, -1);
        if (address.startsWith("IPv6:")) {
            return isValidIpv6(address.slice(5));
        }
        return isValidIpv4(address);
    }

    return isValidIdnHostname(domainPart);
}

export function isValidHostname(v: unknown): boolean {
    if (!_isString(v)) return true;
    if (v.length === 0 || v.length > 253) return false;
    if (v.includes("\uFF0E") || v.includes("\u3002") || v.includes("\uFF61")) return false;
    return _validateHostnameLabels(v.split("."), false);
}

export function isValidIdnHostname(v: unknown): boolean {
    if (!_isString(v)) return true;
    if (v.length === 0) return false;

    const normalized = v.replace(/[\u3002\uFF0E\uFF61]/g, ".");
    return _validateHostnameLabels(normalized.split("."), true);
}

export function isValidIpv4(v: unknown): boolean {
    if (!_isString(v)) return true;
    const parts = v.split(".");
    if (parts.length !== 4) return false;
    return parts.every((p) => _reIpv4Octet.test(p));
}

export function isValidIpv6(v: unknown): boolean {
    if (!_isString(v)) return true;
    if (v.includes("%")) return false;
    const parts = v.split("::");
    if (parts.length > 2) return false;

    const hasIpv4Tail = v.includes(".");
    const parseGroups = (segment: string): string[] | null => {
        if (segment.length === 0) return [];
        const groups = segment.split(":");
        for (let i = 0; i < groups.length; i++) {
            const group = groups[i];
            if (group.length === 0) return null;
            if (i === groups.length - 1 && hasIpv4Tail && group.includes(".")) {
                if (!isValidIpv4(group)) return null;
                groups[i] = "ipv4";
                continue;
            }
            if (!/^[0-9A-Fa-f]{1,4}$/.test(group)) return null;
        }
        return groups;
    };

    const left = parseGroups(parts[0]);
    const right = parts.length === 2 ? parseGroups(parts[1]) : [];
    if (left === null || right === null) return false;

    const groupCount = left.length + right.length;
    const targetCount = hasIpv4Tail ? 7 : 8;
    if (parts.length === 1) {
        return groupCount === targetCount;
    }

    return groupCount < targetCount;
}

function _validateHostnameLabels(labels: string[], allowUnicode: boolean): boolean {
    if (labels.length === 0) return false;
    let totalLength = labels.length - 1;
    for (const label of labels) {
        totalLength += label.length;
        if (label.length === 0 || label.length > 63) return false;
        if (!_validateHostnameLabel(label, allowUnicode)) return false;
    }
    return totalLength <= 253;
}

function _validateHostnameLabel(label: string, allowUnicode: boolean): boolean {
    if (label.startsWith("-") || label.endsWith("-")) return false;
    if (label.length >= 4 && label[2] === "-" && label[3] === "-" && !/^xn--/i.test(label)) {
        return false;
    }

    if (/^xn--/i.test(label)) {
        if (/^xn--$/i.test(label) || /^xn--.$/i.test(label)) return false;
        const decoded = _decodePunycodeLabel(label.slice(4));
        if (decoded === null || decoded.length === 0) return false;
        if (_isCombiningMark(decoded[0])) return false;
        return _validateIdnLabelContextualRules(decoded);
    }

    const isAscii = /^[\x00-\x7F]+$/.test(label);
    if (isAscii) {
        return _reHostnameLabel.test(label);
    }

    if (!allowUnicode) return false;
    if (_isCombiningMark(label[0])) return false;
    return _validateUnicodeHostnameLabel(label) && _validateIdnLabelContextualRules(label);
}

function _validateIdnLabelContextualRules(label: string): boolean {
    let hasNonAscii = false;
    let hasKatakanaMiddleDot = false;
    let hasCjk = false;
    let hasArabicIndic = false;
    let hasExtendedArabicIndic = false;

    for (let i = 0; i < label.length; i++) {
        const c = label[i];
        if (c.charCodeAt(0) > 127) hasNonAscii = true;

        if (c === "\u302E" || c === "\u302F" || c === "\u0640" || c === "\u07FA" ||
            c === "\u303B" || (c >= "\u3031" && c <= "\u3035")) {
            return false;
        }

        switch (c) {
            case "\u00B7":
                if (i === 0 || i === label.length - 1 ||
                    label[i - 1].toLowerCase() !== "l" ||
                    label[i + 1].toLowerCase() !== "l") {
                    return false;
                }
                break;
            case "\u0375":
                if (i === label.length - 1 || !_isGreek(label[i + 1])) return false;
                break;
            case "\u05F3":
            case "\u05F4":
                if (i === 0 || !_isHebrew(label[i - 1])) return false;
                break;
            case "\u30FB":
                hasKatakanaMiddleDot = true;
                break;
            case "\u200D":
                if (i === 0 || !_isVirama(label[i - 1])) return false;
                break;
        }

        if (c >= "\u0660" && c <= "\u0669") hasArabicIndic = true;
        if (c >= "\u06F0" && c <= "\u06F9") hasExtendedArabicIndic = true;
        if (_isCjk(c)) hasCjk = true;
    }

    if (hasArabicIndic && hasExtendedArabicIndic) return false;
    if (hasKatakanaMiddleDot && !hasCjk) return false;

    if (label.length >= 4 && label[2] === "-" && label[3] === "-") {
        if (hasNonAscii) return false;
        if (/^xn--/i.test(label)) {
            const punycode = label.slice(4);
            const lastHyphen = punycode.lastIndexOf("-");
            const basic = lastHyphen >= 0 ? punycode.slice(0, lastHyphen) : punycode;
            if (basic.length >= 4 && basic[2] === "-" && basic[3] === "-") {
                return false;
            }
        }
    }

    return true;
}

function _validateUnicodeHostnameLabel(label: string): boolean {
    for (const c of label) {
        if (c === ".") return false;
    }
    return !label.startsWith("-") && !label.endsWith("-");
}

function _isCombiningMark(c: string): boolean {
    return _reCombiningMark.test(c);
}

function _isGreek(c: string): boolean {
    const cp = c.codePointAt(0)!;
    return (cp >= 0x0370 && cp <= 0x03FF) || (cp >= 0x1F00 && cp <= 0x1FFF);
}

function _isHebrew(c: string): boolean {
    const cp = c.codePointAt(0)!;
    return cp >= 0x05D0 && cp <= 0x05EA;
}

function _isCjk(c: string): boolean {
    const cp = c.codePointAt(0)!;
    return (cp >= 0x3040 && cp <= 0x309F) ||
        (cp >= 0x30A0 && cp <= 0x30FF && cp !== 0x30FB) ||
        (cp >= 0x31F0 && cp <= 0x31FF) ||
        (cp >= 0x3400 && cp <= 0x4DBF) ||
        (cp >= 0x4E00 && cp <= 0x9FFF);
}

function _isVirama(c: string): boolean {
    const cp = c.codePointAt(0)!;
    return cp === 0x094D || cp === 0x09CD || cp === 0x0A4D || cp === 0x0ACD ||
        cp === 0x0B4D || cp === 0x0BCD || cp === 0x0C4D || cp === 0x0CCD ||
        cp === 0x0D4D || cp === 0x0DCA || cp === 0x0E3A || cp === 0x0F84 ||
        cp === 0x1039 || cp === 0x1714 || cp === 0x1734 || cp === 0x17D2 ||
        cp === 0x1A60 || cp === 0x1B44 || cp === 0x1BAA || cp === 0xA806 ||
        cp === 0xA8C4 || cp === 0xA953 || cp === 0xA9C0 || cp === 0xAAF6;
}

function _decodePunycodeLabel(input: string): string | null {
    const base = 36;
    const tMin = 1;
    const tMax = 26;
    const skew = 38;
    const damp = 700;
    const initialBias = 72;
    const initialN = 128;

    const decodeDigit = (codePoint: number): number => {
        if (codePoint >= 48 && codePoint <= 57) return codePoint - 22;
        if (codePoint >= 65 && codePoint <= 90) return codePoint - 65;
        if (codePoint >= 97 && codePoint <= 122) return codePoint - 97;
        return base;
    };

    const adapt = (deltaValue: number, numPoints: number, firstTime: boolean): number => {
        let delta = firstTime ? Math.floor(deltaValue / damp) : deltaValue >> 1;
        delta += Math.floor(delta / numPoints);
        let k = 0;
        while (delta > (((base - tMin) * tMax) >> 1)) {
            delta = Math.floor(delta / (base - tMin));
            k += base;
        }
        return k + Math.floor(((base - tMin + 1) * delta) / (delta + skew));
    };

    let n = initialN;
    let i = 0;
    let bias = initialBias;
    const output: number[] = [];

    const basic = input.lastIndexOf("-");
    if (basic >= 0) {
        for (let j = 0; j < basic; j++) {
            output.push(input.charCodeAt(j));
        }
    }

    let index = basic >= 0 ? basic + 1 : 0;
    while (index < input.length) {
        const oldI = i;
        let w = 1;
        for (let k = base; ; k += base) {
            if (index >= input.length) return null;
            const digit = decodeDigit(input.charCodeAt(index++));
            if (digit >= base) return null;
            i += digit * w;
            const t = k <= bias ? tMin : (k >= bias + tMax ? tMax : k - bias);
            if (digit < t) break;
            w *= (base - t);
        }

        bias = adapt(i - oldI, output.length + 1, oldI === 0);
        n += Math.floor(i / (output.length + 1));
        i %= output.length + 1;
        output.splice(i, 0, n);
        i++;
    }

    try {
        return String.fromCodePoint(...output);
    } catch {
        return null;
    }
}

function _containsInvalidUriCharacters(uri: string, iriSupport: boolean, isTemplate: boolean): boolean {
    if (isTemplate) return false;
    for (const c of uri) {
        if (!iriSupport && c.codePointAt(0)! > 127) return true;
        if (c === " " || c === "<" || c === ">" || c === "\"" ||
            c === "{" || c === "}" || c === "|" || c === "\\" ||
            c === "^" || c === "`") {
            return true;
        }
    }
    return false;
}

function _isAbsoluteUriWithoutAuthority(uri: string): boolean {
    const lower = uri.toLowerCase();
    return lower.startsWith("urn:") ||
        lower.startsWith("tag:") ||
        lower.startsWith("mailto:") ||
        lower.startsWith("news:") ||
        lower.startsWith("tel:");
}

function _validateUriLike(v: string, iriSupport: boolean, canBeRelative: boolean): boolean {
    if (v.length === 0) return canBeRelative;
    if (v.trim().length === 0) return false;
    if (_containsInvalidUriCharacters(v, iriSupport, false)) return false;

    if (/^[A-Za-z][A-Za-z0-9+\-.]*:/.test(v)) {
        return _validateAbsoluteUri(v, iriSupport);
    }

    if (!canBeRelative) return false;
    if (v.startsWith("//")) {
        return _validateAuthority(v.slice(2), iriSupport);
    }
    return true;
}

function _validateAbsoluteUri(uri: string, iriSupport: boolean): boolean {
    if (_isAbsoluteUriWithoutAuthority(uri)) {
        return /^[A-Za-z][A-Za-z0-9+\-.]*:[^\s]*$/.test(uri);
    }

    const colon = uri.indexOf(":");
    if (colon <= 0) return false;
    const rest = uri.slice(colon + 1);
    if (!rest.startsWith("//")) {
        return true;
    }
    return _validateAuthority(rest.slice(2), iriSupport);
}

function _validateAuthority(rest: string, iriSupport: boolean): boolean {
    const authorityEnd = rest.search(/[/?#]/);
    const authority = authorityEnd >= 0 ? rest.slice(0, authorityEnd) : rest;
    if (authority.length === 0) return false;

    let hostPort = authority;
    const at = authority.lastIndexOf("@");
    if (at >= 0) {
        const userInfo = authority.slice(0, at);
        if (userInfo.includes("[") || userInfo.includes("]")) return false;
        hostPort = authority.slice(at + 1);
        if (hostPort.length === 0) return false;
    }

    let host = hostPort;
    if (hostPort.startsWith("[")) {
        const close = hostPort.indexOf("]");
        if (close <= 0) return false;
        host = hostPort.slice(1, close);
        const suffix = hostPort.slice(close + 1);
        if (suffix.length > 0) {
            if (!suffix.startsWith(":")) return false;
            if (!/^\:\d*$/.test(suffix)) return false;
        }
        return isValidIpv6(host);
    }

    const lastColon = hostPort.lastIndexOf(":");
    if (lastColon >= 0 && hostPort.indexOf(":") === lastColon) {
        const maybePort = hostPort.slice(lastColon + 1);
        if (/^\d+$/.test(maybePort)) {
            host = hostPort.slice(0, lastColon);
        }
    }

    if (host.length === 0 || host.includes("[") || host.includes("]")) return false;
    if (/^[0-9.]+$/.test(host)) return isValidIpv4(host);
    return iriSupport ? isValidIdnHostname(host) : isValidHostname(host);
}

export function isValidUri(v: unknown): boolean {
    if (!_isString(v)) return true;
    return /^[A-Za-z][A-Za-z0-9+\-.]*:/.test(v) && _validateUriLike(v, false, false);
}

export function isValidUriReference(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _validateUriLike(v, false, true);
}

export function isValidIri(v: unknown): boolean {
    if (!_isString(v)) return true;
    return /^[A-Za-z][A-Za-z0-9+\-.]*:/.test(v) && _validateUriLike(v, true, false);
}

export function isValidIriReference(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _validateUriLike(v, true, true);
}

export function isValidUriTemplate(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _reUriTemplate.test(v);
}

export function isValidJsonPointer(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _reJsonPointer.test(v);
}

export function isValidRelativeJsonPointer(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _reRelativeJsonPointer.test(v);
}

export function isValidRegex(v: unknown): boolean {
    if (!_isString(v)) return true;
    try {
        new RegExp(v, "u");
        return true;
    } catch {
        return false;
    }
}

export function isValidUuid(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _reUuid.test(v);
}

// ---- Extended format catalog (opt-in; see FORMATS.md) ----------------------
//
// Mirrors FormFinch.JsonSchemaValidation.Formats.ExtendedFormatValidators. Each check
// accepts the documented input variants (separators, prefixes, any case) and never
// changes the value. Validators import these only when generated with
// --extended-formats. Strings are processed per UTF-16 code unit, like the C# checks.

const _extTrimChars = " \u00A0\t\r\n";
const _extSpaces = " \u00A0";
const _extSpacesAndDashes = " \u00A0-";
const _extSpacesAndDots = " \u00A0.";
const _extSpacesDotsAndDashes = " \u00A0.-";
const _extPhoneSeparators = " \u00A0-./";

// IBAN length per country, from the SWIFT IBAN Registry release 101 (see FORMATS.md).
const _extIbanLengths: Readonly<Record<string, number>> = {
    AD: 24, AE: 23, AL: 28, AT: 20, AZ: 28, BA: 20, BE: 16, BG: 22, BH: 22, BI: 27, BR: 29, BY: 28,
    CH: 21, CR: 22, CY: 28, CZ: 24, DE: 22, DJ: 27, DK: 18, DO: 28, EE: 20, EG: 29, ES: 24, FI: 18,
    FK: 18, FO: 18, FR: 27, GB: 22, GE: 22, GI: 23, GL: 18, GR: 27, GT: 28, HN: 28, HR: 21, HU: 28,
    IE: 22, IL: 23, IQ: 23, IS: 26, IT: 27, JO: 30, KW: 30, KZ: 20, LB: 28, LC: 32, LI: 21, LT: 20,
    LU: 20, LV: 21, LY: 25, MC: 27, MD: 24, ME: 22, MK: 19, MN: 20, MR: 27, MT: 31, MU: 30, NI: 28,
    NL: 18, NO: 15, OM: 23, PK: 24, PL: 28, PS: 29, PT: 25, QA: 29, RO: 24, RS: 22, RU: 33, SA: 24,
    SC: 31, SD: 18, SE: 24, SI: 19, SK: 24, SM: 27, SO: 23, ST: 25, SV: 28, TL: 23, TN: 24, TR: 26,
    UA: 29, VA: 22, VG: 24, XK: 20, YE: 30,
};

function _extIsDigit(c: string): boolean {
    return c.length === 1 && c >= "0" && c <= "9";
}

function _extIsLetter(c: string): boolean {
    return c.length === 1 && ((c >= "A" && c <= "Z") || (c >= "a" && c <= "z"));
}

function _extIsSpace(c: string): boolean {
    return c === " " || c === "\u00A0";
}

function _extUpper(c: string): string {
    return c >= "a" && c <= "z" ? String.fromCharCode(c.charCodeAt(0) - 32) : c;
}

function _extTrim(value: string): string {
    let start = 0;
    let end = value.length;
    while (start < end && _extTrimChars.includes(value.charAt(start))) start++;
    while (end > start && _extTrimChars.includes(value.charAt(end - 1))) end--;
    return value.slice(start, end);
}

// Removes separators and upper-cases ASCII letters. Null on any other character or an
// empty result.
function _extCompact(value: string, separators: string, allowLetters: boolean): string | null {
    const text = _extTrim(value);
    let out = "";
    for (let i = 0; i < text.length; i++) {
        const c = text.charAt(i);
        if (separators.includes(c)) continue;
        if (!(_extIsDigit(c) || (allowLetters && _extIsLetter(c)))) return null;
        out += _extUpper(c);
    }
    return out.length > 0 ? out : null;
}

// ISO 7064 mod 97-10 over an upper-case alphanumeric string; letters count as 10..35.
function _extMod97(text: string): number {
    let remainder = 0;
    for (let i = 0; i < text.length; i++) {
        const code = text.charCodeAt(i);
        remainder = code >= 48 && code <= 57
            ? (remainder * 10 + (code - 48)) % 97
            : (remainder * 100 + (code - 55)) % 97;
    }
    return remainder;
}

function _extElfproef(nineDigits: string): boolean {
    let sum = 0;
    let allZero = true;
    for (let i = 0; i < 8; i++) {
        const d = nineDigits.charCodeAt(i) - 48;
        sum += d * (9 - i);
        allZero = allZero && d === 0;
    }
    const last = nineDigits.charCodeAt(8) - 48;
    allZero = allZero && last === 0;
    return !allZero && (sum - last) % 11 === 0;
}

function _extIban(value: string): boolean {
    const iban = _extCompact(value, _extSpaces, true);
    if (iban === null || iban.length < 5) return false;
    if (!_extIsLetter(iban.charAt(0)) || !_extIsLetter(iban.charAt(1)) ||
        !_extIsDigit(iban.charAt(2)) || !_extIsDigit(iban.charAt(3))) {
        return false;
    }
    const country = iban.slice(0, 2);
    if (!Object.prototype.hasOwnProperty.call(_extIbanLengths, country) || iban.length !== _extIbanLengths[country]) {
        return false;
    }
    return _extMod97(iban.slice(4) + iban.slice(0, 4)) === 1;
}

function _extBic(value: string): boolean {
    const bic = _extCompact(value, _extSpaces, true);
    if (bic === null || (bic.length !== 8 && bic.length !== 11)) return false;
    // Business party prefix and suffix are alphanumeric (ISO 9362:2014); the country code is letters.
    return _extIsLetter(bic.charAt(4)) && _extIsLetter(bic.charAt(5));
}

function _extStartsWithIsbn(text: string): boolean {
    return text.length >= 4 &&
        _extUpper(text.charAt(0)) === "I" && _extUpper(text.charAt(1)) === "S" &&
        _extUpper(text.charAt(2)) === "B" && _extUpper(text.charAt(3)) === "N";
}

function _extIsbn13(value: string): boolean {
    let text = _extTrim(value);
    if (_extStartsWithIsbn(text)) {
        text = text.slice(4);
        if (text.startsWith("-13")) text = text.slice(3);
        if (text.startsWith(":")) text = text.slice(1);
        text = _extTrim(text);
    }
    const isbn = _extCompact(text, _extSpacesAndDashes, false);
    if (isbn === null || isbn.length !== 13 || !(isbn.startsWith("978") || isbn.startsWith("979"))) return false;
    let sum = 0;
    for (let i = 0; i < 13; i++) {
        sum += (isbn.charCodeAt(i) - 48) * (i % 2 === 0 ? 1 : 3);
    }
    return sum % 10 === 0;
}

function _extNlBsn(value: string): boolean {
    let digits = _extCompact(value, _extSpacesDotsAndDashes, false);
    if (digits === null) return false;
    if (digits.length === 8) digits = "0" + digits;
    return digits.length === 9 && _extElfproef(digits);
}

function _extNlVat(value: string): boolean {
    const vat = _extCompact(value, _extSpacesAndDots, true);
    if (vat === null || vat.length !== 14 || vat.charAt(0) !== "N" || vat.charAt(1) !== "L" || vat.charAt(11) !== "B") {
        return false;
    }
    for (let i = 2; i < 14; i++) {
        if (i !== 11 && !_extIsDigit(vat.charAt(i))) return false;
    }
    // The suffix after B runs from 01 to 99; 00 is never issued.
    if (vat.charAt(12) === "0" && vat.charAt(13) === "0") return false;
    // Pre-2020 numbers carry an 11-check on the 9 digits; the 2020 btw-id passes mod-97.
    return _extElfproef(vat.slice(2, 11)) || _extMod97(vat) === 1;
}

function _extNlKvk(value: string): boolean {
    const digits = _extCompact(value, _extSpaces, false);
    return digits !== null && digits.length === 8 && /[1-9]/.test(digits);
}

function _extNlPostcode(value: string): boolean {
    let text = _extTrim(value);
    if (text.length === 7 && _extIsSpace(text.charAt(4))) text = text.slice(0, 4) + text.slice(5);
    if (text.length !== 6 || text.charAt(0) < "1" || text.charAt(0) > "9") return false;
    for (let i = 1; i < 4; i++) {
        if (!_extIsDigit(text.charAt(i))) return false;
    }
    if (!_extIsLetter(text.charAt(4)) || !_extIsLetter(text.charAt(5))) return false;
    // SA, SD and SS are not issued.
    const first = _extUpper(text.charAt(4));
    const second = _extUpper(text.charAt(5));
    return !(first === "S" && (second === "A" || second === "D" || second === "S"));
}

// Digits of a phone number and whether it starts with "+", or null. Accepts digits,
// separators (space, hyphen, dot, slash) and balanced, non-nested parentheses that
// contain at least one digit.
function _extReadPhoneDigits(value: string): { international: boolean; digits: string } | null {
    const text = _extTrim(value);
    if (text.length === 0) return null;
    const international = text.charAt(0) === "+";
    let digits = "";
    let inGroup = false;
    let groupHasDigit = false;
    for (let i = international ? 1 : 0; i < text.length; i++) {
        const c = text.charAt(i);
        if (_extIsDigit(c)) {
            digits += c;
            groupHasDigit = true;
        } else if (c === "(") {
            if (inGroup) return null;
            inGroup = true;
            groupHasDigit = false;
        } else if (c === ")") {
            if (!inGroup || !groupHasDigit) return null;
            inGroup = false;
        } else if (!_extPhoneSeparators.includes(c)) {
            return null;
        }
    }
    if (inGroup) return null;
    return { international, digits };
}

// Significant (national) digits of a phone number for one country calling code, or null.
// Accepts +CC, 00CC and national prefixes and separators. With a trunk prefix (the
// default), a national number starts with 0 and an optional trunk 0 may follow the
// country code (written as "(0)" or not). Countries without a trunk prefix (Liechtenstein,
// Luxembourg) write national numbers bare.
function _extParsePhone(value: string, countryCode: string, trunkPrefix = true): string | null {
    const read = _extReadPhoneDigits(value);
    if (read === null) return null;
    const digits = read.digits;
    let rest: string;
    if (read.international) {
        if (!digits.startsWith(countryCode)) return null;
        rest = digits.slice(countryCode.length);
    } else if (digits.startsWith("00")) {
        if (!digits.startsWith(countryCode, 2)) return null;
        rest = digits.slice(2 + countryCode.length);
    } else if (trunkPrefix) {
        if (digits.length < 2 || digits.charAt(0) !== "0") return null;
        return digits.slice(1);
    } else {
        if (digits.length === 0 || digits.charAt(0) === "0") return null;
        return digits;
    }
    if (trunkPrefix && rest.startsWith("0")) rest = rest.slice(1);
    if (rest.length === 0 || rest.charAt(0) === "0") return null;
    return rest;
}

function _extNlPhone(value: string): boolean {
    const national = _extParsePhone(value, "31");
    return national !== null && national.length === 9;
}

function _extBePhone(value: string): boolean {
    const national = _extParsePhone(value, "32");
    if (national === null) return false;
    if (national.length === 8) return true;
    return national.length === 9 && national.charAt(0) === "4" && national.charAt(1) >= "5";
}

// Letter/digit pattern of a compacted value: each ASCII letter becomes "L", each digit "D".
function _extShape(compact: string): string {
    let out = "";
    for (let i = 0; i < compact.length; i++) out += _extIsDigit(compact.charAt(i)) ? "D" : "L";
    return out;
}

function _extAllDigits(text: string): boolean {
    for (let i = 0; i < text.length; i++) {
        if (!_extIsDigit(text.charAt(i))) return false;
    }
    return true;
}

function _extAllZeros(digits: string): boolean {
    return !/[1-9]/.test(digits);
}

function _extE164Phone(value: string): boolean {
    const text = _extTrim(value);
    let start: number;
    if (text.startsWith("+")) start = 1;
    else if (text.startsWith("00")) start = 2;
    else return false;
    let digits = "";
    let inGroup = false;
    let groupStart = 0;
    let groups = 0;
    for (let i = start; i < text.length; i++) {
        const c = text.charAt(i);
        if (_extIsDigit(c)) {
            digits += c;
        } else if (c === "(") {
            if (inGroup) return false;
            inGroup = true;
            groupStart = digits.length;
            groups++;
        } else if (c === ")") {
            if (!inGroup || digits.length === groupStart) return false;
            inGroup = false;
            // A first group written "(0)" after the country code is a trunk prefix, not a digit.
            if (groups === 1 && groupStart > 0 && digits.length === groupStart + 1 && digits.charAt(groupStart) === "0") {
                digits = digits.slice(0, groupStart);
            }
        } else if (!_extPhoneSeparators.includes(c)) {
            return false;
        }
    }
    if (inGroup) return false;
    return digits.length >= 7 && digits.length <= 15 && digits.charAt(0) !== "0";
}

function _extBePostcode(value: string): boolean {
    return /^[1-9][0-9]{3}$/.test(_extTrim(value));
}

// RDW sidecodes 1-14 as letter (L) / digit (D) patterns.
const _extNlPlateSidecodes = [
    "LLDDDD", "DDDDLL", "DDLLDD", "LLDDLL", "LLLLDD", "DDLLLL", "DDLLLD",
    "DLLLDD", "LLDDDL", "LDDDLL", "LLLDDL", "LDDLLL", "DLLDDD", "DDDLLD",
];

function _extNlPlate(value: string): boolean {
    const plate = _extCompact(value, _extSpacesAndDashes, true);
    return plate !== null && plate.length === 6 && _extNlPlateSidecodes.includes(_extShape(plate));
}

function _extGbPhone(value: string): boolean {
    const national = _extParsePhone(value, "44");
    if (national === null) return false;
    if (national.length === 10) return "1235789".includes(national.charAt(0));
    if (national.length === 9) {
        // 9-digit numbers exist only in the mixed 01xxx(x) areas (not 011x or 01x1) and legacy 0800.
        return national.charAt(0) === "1"
            ? national.charAt(1) !== "1" && national.charAt(2) !== "1"
            : national.startsWith("800");
    }
    return false;
}

const _extGbPostcodePattern = /^(?:[A-PR-UWYZ][0-9][0-9]?|[A-PR-UWYZ][A-HK-Y][0-9][0-9]?|[A-PR-UWYZ][0-9][ABCDEFGHJKPSTUW]|[A-PR-UWYZ][A-HK-Y][0-9][ABEHMNPRVWXY])[0-9][ABD-HJLNP-UW-Z]{2}$/;

function _extGbPostcode(value: string): boolean {
    let text = _extTrim(value);
    if (text.length >= 6 && _extIsSpace(text.charAt(text.length - 4))) {
        text = text.slice(0, text.length - 4) + text.slice(text.length - 3);
    }
    const postcode = _extCompact(text, "", true);
    return postcode !== null && (postcode === "GIR0AA" || _extGbPostcodePattern.test(postcode));
}

function _extGbNhs(value: string): boolean {
    const digits = _extCompact(value, _extSpacesAndDashes, false);
    if (digits === null || digits.length !== 10 || _extAllZeros(digits)) return false;
    // Weights 10..2 on the first nine digits and 1 on the check digit; a check value of 10
    // is never issued, and no single digit satisfies it.
    let sum = 0;
    for (let i = 0; i < 10; i++) sum += (10 - i) * (digits.charCodeAt(i) - 48);
    return sum % 11 === 0;
}

function _extGbVat(value: string): boolean {
    let vat = _extCompact(value, _extSpacesDotsAndDashes, true);
    if (vat === null) return false;
    if (vat.startsWith("GB") || vat.startsWith("XI")) vat = vat.slice(2);
    if (vat.length === 5 && (vat.startsWith("GD") || vat.startsWith("HA"))) {
        const rest = vat.slice(2);
        if (!_extAllDigits(rest)) return false;
        const n = parseInt(rest, 10);
        // Government departments GD000-GD499, health authorities HA500-HA999.
        return vat.charAt(0) === "G" ? n < 500 : n >= 500;
    }
    if ((vat.length !== 9 && vat.length !== 12) || !_extAllDigits(vat)) return false;
    if (_extAllZeros(vat.slice(0, 7))) return false;
    const weights = [8, 7, 6, 5, 4, 3, 2, 10, 1];
    let sum = 0;
    for (let i = 0; i < 9; i++) sum += weights[i] * (vat.charCodeAt(i) - 48);
    const remainder = sum % 97;
    // Classic mod 97, or the 9755 series (55 added before mod 97) for numbers from 100 upwards.
    return remainder === 0 || (remainder === 42 && vat.charAt(0) !== "0");
}

// Companies House company number prefixes (see FORMATS.md, gb-crn).
const _extGbCrnPrefixes = [
    "AC", "ZC", "FC", "GE", "LP", "OC", "SE", "SA", "SZ", "SF", "GS", "SL", "SO", "SC",
    "ES", "NA", "NZ", "NF", "GN", "NL", "NC", "R0", "NI", "EN", "IP", "SP", "IC", "SI",
    "NP", "NV", "RC", "SR", "NR", "NO", "BR", "CE", "CS", "OE", "PC", "SG",
];

function _extGbCrn(value: string): boolean {
    const crn = _extCompact(value, _extSpaces, true);
    if (crn === null || crn.length !== 8) return false;
    if (_extAllDigits(crn)) return !_extAllZeros(crn);
    const number = crn.slice(2);
    return _extGbCrnPrefixes.includes(crn.slice(0, 2)) && _extAllDigits(number) && !_extAllZeros(number);
}

function _extGbSortCode(value: string): boolean {
    const digits = _extCompact(value, _extSpacesAndDashes, false);
    return digits !== null && digits.length === 6 && !_extAllZeros(digits);
}

function _extGbAccountNumber(value: string): boolean {
    const digits = _extCompact(value, _extSpacesAndDashes, false);
    return digits !== null && digits.length === 8 && !_extAllZeros(digits);
}

const _extGbNinoPattern = /^[A-CEGHJ-PR-TW-Z][A-CEGHJ-NPR-TW-Z][0-9]{6}[A-D]?$/;
const _extGbNinoExcludedPrefixes = ["BG", "GB", "KN", "NK", "NT", "TN", "ZZ"];

function _extGbNino(value: string): boolean {
    const nino = _extCompact(value, _extSpaces, true);
    return nino !== null && _extGbNinoPattern.test(nino) && !_extGbNinoExcludedPrefixes.includes(nino.slice(0, 2));
}

// DfE UPN check alphabet: A-Z without I, O and S.
const _extGbUpnAlphabet = "ABCDEFGHJKLMNPQRTUVWXYZ";

function _extGbUpn(value: string): boolean {
    const upn = _extCompact(value, _extSpaces, true);
    if (upn === null || upn.length !== 13) return false;
    if (!_extGbUpnAlphabet.includes(upn.charAt(0)) || !_extAllDigits(upn.slice(1, 12))) return false;
    const last = upn.charAt(12);
    if (!_extIsDigit(last) && !_extGbUpnAlphabet.includes(last)) return false;
    let sum = 0;
    for (let i = 1; i < 13; i++) {
        const c = upn.charAt(i);
        const charValue = _extIsDigit(c) ? c.charCodeAt(0) - 48 : _extGbUpnAlphabet.indexOf(c);
        sum += (i + 1) * charValue;
    }
    return _extGbUpnAlphabet.charAt(sum % 23) === upn.charAt(0);
}

const _extGbPlatePattern = /^(?:[A-HJ-PR-Y]{2}[0-9]{2}[A-HJ-PR-Z]{3}|[A-Z][1-9][0-9]{0,2}[A-Z]{3}|[A-Z]{3}[1-9][0-9]{0,2}[A-Z]|[A-Z]{1,3}[1-9][0-9]{0,3}|[1-9][0-9]{0,3}[A-Z]{1,3})$/;

function _extGbPlate(value: string): boolean {
    const plate = _extCompact(value, _extSpaces, true);
    return plate !== null && plate.length <= 7 && _extGbPlatePattern.test(plate);
}

const _extSpacesDotsDashesAndSlashes = "  .-/";
const _extSpacesDashesAndSlashes = "  -/";

// ISO 7064 MOD 11,10 over a digit string whose last digit is the check digit.
function _extMod1110(digits: string): boolean {
    let product = 10;
    for (let i = 0; i < digits.length - 1; i++) {
        let sum = (digits.charCodeAt(i) - 48 + product) % 10;
        if (sum === 0) sum = 10;
        product = (sum * 2) % 11;
    }
    let check = 11 - product;
    if (check === 10) check = 0;
    return check === digits.charCodeAt(digits.length - 1) - 48;
}

// Sum of the digit sums of digit x weight products, mod 10.
function _extDigitSumCheck(digits: string, weights: number[]): number {
    let sum = 0;
    for (let i = 0; i < weights.length; i++) {
        const product = (digits.charCodeAt(i) - 48) * weights[i];
        sum += Math.floor(product / 10) + (product % 10);
    }
    return sum % 10;
}

function _extDePhone(value: string): boolean {
    const national = _extParsePhone(value, "49");
    if (national === null) return false;
    if (national.startsWith("15")) return national.length === 11;
    if (/^(?:16[023]|17)/.test(national)) return national.length === 10 || national.length === 11;
    return national.length >= 6 && national.length <= 13;
}

function _extDePostcode(value: string): boolean {
    const text = _extTrim(value);
    return text.length === 5 && _extAllDigits(text) && !text.startsWith("00");
}

function _extDeVat(value: string): boolean {
    const vat = _extCompact(value, _extSpaces, true);
    if (vat === null || vat.length !== 11 || !vat.startsWith("DE")) return false;
    const digits = vat.slice(2);
    return _extAllDigits(digits) && digits.charAt(0) !== "0" && _extMod1110(digits);
}

function _extDeIdnr(value: string): boolean {
    const idnr = _extCompact(value, _extSpacesDotsDashesAndSlashes, false);
    if (idnr === null || idnr.length !== 11 || idnr.charAt(0) === "0") return false;
    // Among the first 10 digits exactly one digit value occurs twice or three times; a
    // tripled digit may not stand three in a row.
    const counts = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    for (let i = 0; i < 10; i++) counts[idnr.charCodeAt(i) - 48]++;
    let repeated = -1;
    for (let d = 0; d < 10; d++) {
        if (counts[d] > 3) return false;
        if (counts[d] > 1) {
            if (repeated >= 0) return false;
            repeated = d;
        }
    }
    if (repeated < 0) return false;
    if (counts[repeated] === 3) {
        const triple = String(repeated).repeat(3);
        if (idnr.slice(0, 10).includes(triple)) return false;
    }
    return _extMod1110(idnr);
}

function _extDeStnr(value: string): boolean {
    const stnr = _extCompact(value, _extSpacesDashesAndSlashes, false);
    if (stnr === null) return false;
    if (stnr.length === 10 || stnr.length === 11) return true;
    if (stnr.length !== 13 || stnr.charAt(4) !== "0") return false;
    if (!/^(?:10|11|21|22|23|24|26|27|28|30|31|32|40|41|5[0-9]|9[0-9])/.test(stnr)) return false;
    const nrw = stnr.charAt(0) === "5";
    const bezirk = nrw ? stnr.slice(5, 9) : stnr.slice(5, 8);
    if (["000", "998", "999", "0000", "0998", "0999"].includes(bezirk)) return false;
    if (/^(?:9|30|40|10|32|31|41)/.test(stnr) && parseInt(bezirk, 10) < 100) return false;
    if (nrw && parseInt(stnr.slice(9, 13), 10) <= 9) return false;
    return !(stnr.charAt(0) === "9" && stnr.slice(5) === "99999999");
}

function _extDeTradeRegister(value: string): boolean {
    const number = _extCompact(value, _extSpaces, true);
    return number !== null && /^(?:HRA|HRB|GNR|GSR|PR|VR)[1-9][0-9]{0,5}[A-Z]{0,3}$/.test(number);
}

const _extDeLeitwegGrobLengths = [2, 3, 5, 8, 9, 12];

function _extDeLeitweg(value: string): boolean {
    const text = _extTrim(value);
    let upper = "";
    for (let i = 0; i < text.length; i++) upper += _extUpper(text.charAt(i));
    const match = /^([0-9]{2,12})(?:-([0-9A-Z]{1,30}))?-([0-9]{2})$/.exec(upper);
    if (match === null) return false;
    const grob = match[1];
    if (!_extDeLeitwegGrobLengths.includes(grob.length)) return false;
    const land = parseInt(grob.slice(0, 2), 10);
    if (!((land >= 1 && land <= 16) || land === 99)) return false;
    return _extMod97(grob + (match[2] ?? "") + match[3]) === 1;
}

function _extDeRvnr(value: string): boolean {
    const rvnr = _extCompact(value, _extSpaces, true);
    if (rvnr === null || rvnr.length !== 12 || !_extAllDigits(rvnr.slice(0, 8)) ||
        !_extIsLetter(rvnr.charAt(8)) || !_extAllDigits(rvnr.slice(9))) {
        return false;
    }
    // The letter becomes its two-digit alphabet position (A=01 ... Z=26).
    const letter = String(rvnr.charCodeAt(8) - 64).padStart(2, "0");
    const digits = rvnr.slice(0, 8) + letter + rvnr.slice(9, 11);
    return _extDigitSumCheck(digits, [2, 1, 2, 5, 7, 1, 2, 1, 2, 1, 2, 1]) === rvnr.charCodeAt(11) - 48;
}

function _extDeKvnr(value: string): boolean {
    const kvnr = _extCompact(value, _extSpaces, true);
    if (kvnr === null || kvnr.length !== 10 || !_extIsLetter(kvnr.charAt(0)) || !_extAllDigits(kvnr.slice(1))) {
        return false;
    }
    const digits = String(kvnr.charCodeAt(0) - 64).padStart(2, "0") + kvnr.slice(1, 9);
    return _extDigitSumCheck(digits, [1, 2, 1, 2, 1, 2, 1, 2, 1, 2]) === kvnr.charCodeAt(9) - 48;
}

// German ID card and passport numbers: 9 characters from the document alphabet and an
// optional ICAO 9303 check digit (weights 7, 3, 1; letters count as 10..35).
function _extDeDocumentNumber(value: string): boolean {
    const number = _extCompact(value, _extSpaces, true);
    if (number === null || !/^[0-9CFGHJKLMNPRTVWXYZ]{9}[0-9]?$/.test(number)) return false;
    if (number.length === 9) return true;
    const weights = [7, 3, 1];
    let sum = 0;
    for (let i = 0; i < 9; i++) {
        const c = number.charAt(i);
        const charValue = _extIsDigit(c) ? c.charCodeAt(0) - 48 : c.charCodeAt(0) - 55;
        sum += charValue * weights[i % 3];
    }
    return sum % 10 === number.charCodeAt(9) - 48;
}

function _extDeWkn(value: string): boolean {
    const wkn = _extCompact(value, _extSpaces, true);
    return wkn !== null && /^[0-9A-HJ-NP-Z]{6}$/.test(wkn);
}

// District 1-3 letters (umlauts allowed), Erkennung 1-2 letters without umlauts.
function _extIsDePlateSplit(district: string, erkennung: string): boolean {
    return district.length >= 1 && district.length <= 3 &&
        erkennung.length >= 1 && erkennung.length <= 2 && /^[A-Z]+$/.test(erkennung);
}

function _extDePlate(value: string): boolean {
    const text = _extTrim(value);
    let upper = "";
    for (let i = 0; i < text.length; i++) {
        const c = text.charAt(i);
        upper += c === "ä" ? "Ä" : c === "ö" ? "Ö" : c === "ü" ? "Ü" : _extUpper(c);
    }
    const match = /^([A-ZÄÖÜ]+(?:[  -]+[A-ZÄÖÜ]+)*)[  -]*([1-9][0-9]{0,3})(?:[  ]*([EH]))?$/.exec(upper);
    if (match === null) return false;
    const groups = match[1].split(/[  -]+/);
    let validSplit = false;
    if (groups.length === 1) {
        const run = groups[0];
        for (let k = 1; k <= 3 && k < run.length; k++) {
            if (_extIsDePlateSplit(run.slice(0, k), run.slice(k))) validSplit = true;
        }
    } else if (groups.length === 2) {
        validSplit = _extIsDePlateSplit(groups[0], groups[1]);
    }
    if (!validSplit) return false;
    const count = groups.join("").length + match[2].length;
    return match[3] !== undefined ? count <= 7 : count <= 8;
}

// Four digits, the first not 0, with no separators (Austria, Belgium, Liechtenstein).
function _extFourDigitPostcode(value: string): boolean {
    return /^[1-9][0-9]{3}$/.test(_extTrim(value));
}

// Luhn doubling: twice the digit, minus 9 when above 9.
function _extDoubleDigit(digit: number): number {
    return digit * 2 > 9 ? digit * 2 - 9 : digit * 2;
}

function _extAtPhone(value: string): boolean {
    const national = _extParsePhone(value, "43");
    return national !== null && national.length >= 5 && national.length <= 13;
}

function _extAtVat(value: string): boolean {
    const vat = _extCompact(value, _extSpaces, true);
    if (vat === null || vat.length !== 11 || !vat.startsWith("ATU") || !_extAllDigits(vat.slice(3))) return false;
    // Digits 1, 3, 5, 7 count as-is; 2, 4, 6 are doubled with 9 subtracted above 9.
    let sum = 0;
    for (let i = 0; i < 7; i++) {
        const d = vat.charCodeAt(3 + i) - 48;
        sum += i % 2 === 0 ? d : _extDoubleDigit(d);
    }
    return (10 - ((sum + 4) % 10)) % 10 === vat.charCodeAt(10) - 48;
}

function _extAtSvnr(value: string): boolean {
    const svnr = _extCompact(value, _extSpaces, false);
    if (svnr === null || svnr.length !== 10 || svnr.charAt(0) === "0") return false;
    const weights = [3, 7, 9, 0, 5, 8, 4, 2, 1, 6];
    let sum = 0;
    for (let i = 0; i < 10; i++) sum += weights[i] * (svnr.charCodeAt(i) - 48);
    const check = sum % 11;
    // A remainder of 10 is never issued.
    return check < 10 && check === svnr.charCodeAt(3) - 48;
}

// Firmenbuchnummer check letters, indexed by the number mod 17.
const _extAtFnCheckLetters = "ABDFGHIKMPSTVWXYZ";

function _extAtFn(value: string): boolean {
    let fn = _extCompact(value, _extSpaces, true);
    if (fn === null) return false;
    if (fn.startsWith("FN")) fn = fn.slice(2);
    if (!/^[1-9][0-9]{0,5}[A-Z]$/.test(fn)) return false;
    const number = parseInt(fn.slice(0, fn.length - 1), 10);
    return _extAtFnCheckLetters.charAt(number % 17) === fn.charAt(fn.length - 1);
}

function _extAtTin(value: string): boolean {
    const tin = _extCompact(value, _extSpacesDashesAndSlashes, false);
    if (tin === null || tin.length !== 9 || _extAllZeros(tin)) return false;
    let sum = 0;
    for (let i = 0; i < 8; i++) {
        const d = tin.charCodeAt(i) - 48;
        sum += i % 2 === 0 ? d : _extDoubleDigit(d);
    }
    return (10 - (sum % 10)) % 10 === tin.charCodeAt(8) - 48;
}

function _extAtPlate(value: string): boolean {
    const plate = _extCompact(value, _extSpacesAndDashes, true);
    return plate !== null && plate.length <= 8 &&
        /^(?:[A-PR-Z]{1,2}[1-9][0-9]{0,4}[A-PR-Z]{1,3}|[A-PR-Z]{1,7}[1-9][0-9]{0,4})$/.test(plate);
}

function _extAtPassport(value: string): boolean {
    const passport = _extCompact(value, _extSpaces, true);
    return passport !== null && /^[A-Z]{1,2}[0-9]{7}$/.test(passport);
}

function _extLiPhone(value: string): boolean {
    const national = _extParsePhone(value, "423", false);
    if (national === null) return false;
    if (national.length === 7) return "234789".includes(national.charAt(0));
    if (national.length === 9) return "56".includes(national.charAt(0));
    return false;
}

function _extLiPostcode(value: string): boolean {
    if (!_extFourDigitPostcode(value)) return false;
    const code = parseInt(_extTrim(value), 10);
    return code >= 9485 && code <= 9498;
}

function _extLiPeid(value: string): boolean {
    const peid = _extCompact(value, _extSpacesAndDots, false);
    return peid !== null && peid.length <= 12 && peid.replace(/^0+/, "").length >= 4;
}

function _extLiPlate(value: string): boolean {
    const plate = _extCompact(value, _extSpacesAndDashes, true);
    return plate !== null && /^FL[1-9][0-9]{0,4}$/.test(plate);
}

// ISO 3166-2:CH canton codes, as used on licence plates.
const _extChCantons = [
    "AG", "AI", "AR", "BE", "BL", "BS", "FR", "GE", "GL", "GR", "JU", "LU", "NE", "NW",
    "OW", "SG", "SH", "SO", "SZ", "TG", "TI", "UR", "VD", "VS", "ZG", "ZH",
];

// Recursive mod 10 carry table (Swiss QR reference, formerly ESR).
const _extChQrReferenceTable = [0, 9, 4, 6, 8, 2, 7, 1, 3, 5];

// Verhoeff dihedral group and permutation tables.
const _extVerhoeffD = [
    [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [1, 2, 3, 4, 0, 6, 7, 8, 9, 5],
    [2, 3, 4, 0, 1, 7, 8, 9, 5, 6], [3, 4, 0, 1, 2, 8, 9, 5, 6, 7],
    [4, 0, 1, 2, 3, 9, 5, 6, 7, 8], [5, 9, 8, 7, 6, 0, 4, 3, 2, 1],
    [6, 5, 9, 8, 7, 1, 0, 4, 3, 2], [7, 6, 5, 9, 8, 2, 1, 0, 4, 3],
    [8, 7, 6, 5, 9, 3, 2, 1, 0, 4], [9, 8, 7, 6, 5, 4, 3, 2, 1, 0],
];

const _extVerhoeffP = [
    [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [1, 5, 7, 6, 2, 8, 3, 0, 9, 4],
    [5, 8, 0, 3, 7, 9, 6, 1, 4, 2], [8, 9, 1, 6, 0, 4, 3, 5, 2, 7],
    [9, 4, 5, 3, 1, 2, 6, 8, 7, 0], [4, 2, 8, 6, 5, 7, 3, 9, 0, 1],
    [2, 7, 9, 3, 8, 0, 6, 4, 1, 5], [7, 0, 4, 6, 9, 1, 3, 2, 5, 8],
];

// UID digits: weights 5 4 3 2 7 6 5 4, check (11 - sum mod 11) mod 11; 10 is never issued.
function _extChUidCheck(digits: string): boolean {
    if (digits.length !== 9 || !_extAllDigits(digits) || _extAllZeros(digits)) return false;
    const weights = [5, 4, 3, 2, 7, 6, 5, 4];
    let sum = 0;
    for (let i = 0; i < 8; i++) sum += weights[i] * (digits.charCodeAt(i) - 48);
    const check = (11 - (sum % 11)) % 11;
    return check !== 10 && check === digits.charCodeAt(8) - 48;
}

// Four digits, the first not 0, optionally preceded by a country prefix such as "CH-" (any case).
function _extPrefixedFourDigitPostcode(value: string, prefix: string): boolean {
    let text = _extTrim(value);
    if (text.length === prefix.length + 4) {
        for (let i = 0; i < prefix.length; i++) {
            if (_extUpper(text.charAt(i)) !== prefix.charAt(i)) return false;
        }
        text = text.slice(prefix.length);
    }
    return /^[1-9][0-9]{3}$/.test(text);
}

function _extLuhn(digits: string): boolean {
    let sum = 0;
    for (let p = 0; p < digits.length; p++) {
        const d = digits.charCodeAt(digits.length - 1 - p) - 48;
        sum += p % 2 === 1 ? _extDoubleDigit(d) : d;
    }
    return sum % 10 === 0;
}

function _extVerhoeff(digits: string): boolean {
    let c = 0;
    for (let p = 0; p < digits.length; p++) {
        c = _extVerhoeffD[c][_extVerhoeffP[p % 8][digits.charCodeAt(digits.length - 1 - p) - 48]];
    }
    return c === 0;
}

function _extChPhone(value: string): boolean {
    const national = _extParsePhone(value, "41");
    return national !== null && national.length === 9 && national.charAt(0) >= "2";
}

function _extChUid(value: string): boolean {
    const uid = _extCompact(value, _extSpacesDotsAndDashes, true);
    return uid !== null && uid.length === 12 && uid.startsWith("CHE") && _extChUidCheck(uid.slice(3));
}

function _extChVat(value: string): boolean {
    const vat = _extCompact(value, _extSpacesDotsAndDashes, true);
    if (vat === null || vat.length < 12 || !vat.startsWith("CHE")) return false;
    return ["", "MWST", "TVA", "IVA"].includes(vat.slice(12)) && _extChUidCheck(vat.slice(3, 12));
}

function _extChAhv(value: string): boolean {
    const ahv = _extCompact(value, _extSpacesAndDots, false);
    if (ahv === null || ahv.length !== 13 || !ahv.startsWith("756")) return false;
    let sum = 0;
    for (let i = 0; i < 13; i++) sum += (ahv.charCodeAt(i) - 48) * (i % 2 === 0 ? 1 : 3);
    return sum % 10 === 0;
}

function _extChQrReference(value: string): boolean {
    const reference = _extCompact(value, _extSpaces, false);
    if (reference === null || reference.length !== 27 || _extAllZeros(reference)) return false;
    let carry = 0;
    for (let i = 0; i < 26; i++) carry = _extChQrReferenceTable[(carry + reference.charCodeAt(i) - 48) % 10];
    return (10 - carry) % 10 === reference.charCodeAt(26) - 48;
}

function _extChQrIban(value: string): boolean {
    if (!_extIban(value)) return false;
    const iban = _extCompact(value, _extSpaces, true);
    if (iban === null || !(iban.startsWith("CH") || iban.startsWith("LI"))) return false;
    const iid = iban.slice(4, 9);
    return _extAllDigits(iid) && iid >= "30000" && iid <= "31999";
}

function _extChPlate(value: string): boolean {
    const plate = _extCompact(value, _extSpacesDotsAndDashes, true);
    if (plate === null || plate.length < 3 || plate.length > 8) return false;
    const number = plate.slice(2);
    return _extChCantons.includes(plate.slice(0, 2)) && _extAllDigits(number) && number.charAt(0) !== "0";
}

function _extChPassport(value: string): boolean {
    const number = _extCompact(value, _extSpaces, true);
    return number !== null && /^[A-HJ-NP-Z][0-9A-HJ-NP-Z]{7}$/.test(number);
}

function _extLuPhone(value: string): boolean {
    const national = _extParsePhone(value, "352", false);
    if (national === null || national.charAt(0) < "2") return false;
    return national.charAt(0) === "6" ? national.length === 9 : national.length >= 4 && national.length <= 11;
}

function _extLuVat(value: string): boolean {
    const vat = _extCompact(value, _extSpacesDotsAndDashes, true);
    if (vat === null || vat.length !== 10 || !vat.startsWith("LU")) return false;
    const digits = vat.slice(2);
    if (!_extAllDigits(digits) || _extAllZeros(digits)) return false;
    return parseInt(digits.slice(0, 6), 10) % 89 === parseInt(digits.slice(6), 10);
}

function _extLuMatricule(value: string): boolean {
    const matricule = _extCompact(value, _extSpacesDotsAndDashes, false);
    if (matricule === null || matricule.length !== 13) return false;
    // Both check digits are computed over the same first 11 digits.
    const body = matricule.slice(0, 11);
    return _extLuhn(body + matricule.charAt(11)) && _extVerhoeff(body + matricule.charAt(12));
}

function _extLuRcs(value: string): boolean {
    const rcs = _extCompact(value, _extSpaces, true);
    return rcs !== null && /^[A-Z][1-9][0-9]{0,5}$/.test(rcs);
}

function _extLuPlate(value: string): boolean {
    const plate = _extCompact(value, _extSpacesAndDashes, true);
    return plate !== null && /^(?:[A-HJ-NP-Z]{2}[0-9]{4}|[A-HJ-NP-Z]{2}[0-9]{2}|[0-9]{4,5})$/.test(plate);
}

export function isValidIso13616Iban(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extIban(v);
}

export function isValidIso9362Bic(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extBic(v);
}

export function isValidIso2108Isbn(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extIsbn13(v);
}

export function isValidNlBsn(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extNlBsn(v);
}

export function isValidNlVat(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extNlVat(v);
}

export function isValidNlKvk(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extNlKvk(v);
}

export function isValidNlPostcode(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extNlPostcode(v);
}

export function isValidNlPhone(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extNlPhone(v);
}

export function isValidBePhone(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extBePhone(v);
}

export function isValidItuE164Phone(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extE164Phone(v);
}

export function isValidBePostcode(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extBePostcode(v);
}

export function isValidNlPlate(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extNlPlate(v);
}

export function isValidGbPhone(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extGbPhone(v);
}

export function isValidGbPostcode(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extGbPostcode(v);
}

export function isValidGbNhs(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extGbNhs(v);
}

export function isValidGbVat(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extGbVat(v);
}

export function isValidGbCrn(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extGbCrn(v);
}

export function isValidGbSortCode(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extGbSortCode(v);
}

export function isValidGbAccountNumber(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extGbAccountNumber(v);
}

export function isValidGbNino(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extGbNino(v);
}

export function isValidGbUpn(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extGbUpn(v);
}

export function isValidGbPlate(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extGbPlate(v);
}

export function isValidDePhone(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDePhone(v);
}

export function isValidDePostcode(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDePostcode(v);
}

export function isValidDeVat(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDeVat(v);
}

export function isValidDeIdnr(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDeIdnr(v);
}

export function isValidDeStnr(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDeStnr(v);
}

export function isValidDeTradeRegister(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDeTradeRegister(v);
}

export function isValidDeLeitweg(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDeLeitweg(v);
}

export function isValidDeRvnr(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDeRvnr(v);
}

export function isValidDeKvnr(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDeKvnr(v);
}

export function isValidDeIdCard(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDeDocumentNumber(v);
}

export function isValidDePassport(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDeDocumentNumber(v);
}

export function isValidDeWkn(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDeWkn(v);
}

export function isValidDePlate(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extDePlate(v);
}

export function isValidAtPhone(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extAtPhone(v);
}

export function isValidAtPostcode(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extFourDigitPostcode(v);
}

export function isValidAtVat(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extAtVat(v);
}

export function isValidAtSvnr(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extAtSvnr(v);
}

export function isValidAtFn(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extAtFn(v);
}

export function isValidAtTin(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extAtTin(v);
}

export function isValidAtPlate(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extAtPlate(v);
}

export function isValidAtPassport(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extAtPassport(v);
}

export function isValidLiPhone(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extLiPhone(v);
}

export function isValidLiPostcode(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extLiPostcode(v);
}

export function isValidLiPeid(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extLiPeid(v);
}

export function isValidLiPlate(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extLiPlate(v);
}

export function isValidChPhone(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extChPhone(v);
}

export function isValidChPostcode(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extPrefixedFourDigitPostcode(v, "CH-");
}

export function isValidChUid(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extChUid(v);
}

export function isValidChVat(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extChVat(v);
}

export function isValidChAhv(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extChAhv(v);
}

export function isValidChQrReference(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extChQrReference(v);
}

export function isValidChQrIban(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extChQrIban(v);
}

export function isValidChPlate(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extChPlate(v);
}

export function isValidChPassport(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extChPassport(v);
}

export function isValidLuPhone(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extLuPhone(v);
}

export function isValidLuPostcode(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extPrefixedFourDigitPostcode(v, "L-");
}

export function isValidLuVat(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extLuVat(v);
}

export function isValidLuMatricule(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extLuMatricule(v);
}

export function isValidLuRcs(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extLuRcs(v);
}

export function isValidLuPlate(v: unknown): boolean {
    if (!_isString(v)) return true;
    return _extLuPlate(v);
}
