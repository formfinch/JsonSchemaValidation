// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

// jsv-runtime.js
//
// Shared runtime for JavaScript validators emitted by jsv-codegen.
// This file IS the ABI. Emitted modules import named exports from here.
//
// ABI VERSION: mvp-0
//
// FROZEN (stable across MVP; breaking changes require ABI version bump):
//   Helpers:   deepEquals, escapeJsonPointer, getCachedRegex, graphemeLength, isInteger
//   Formats:   isValidDateTime, isValidDate, isValidTime, isValidDuration,
//              isValidEmail, isValidIdnEmail, isValidHostname, isValidIdnHostname,
//              isValidIpv4, isValidIpv6, isValidUri, isValidUriReference,
//              isValidUriTemplate, isValidIri, isValidIriReference,
//              isValidJsonPointer, isValidRelativeJsonPointer, isValidRegex,
//              isValidUuid
//   Extended catalog formats (added within mvp-0; imported only by validators
//   generated with --extended-formats, which therefore need a runtime that has
//   them; see FORMATS.md):
//              isValidIso13616Iban, isValidIso9362Bic, isValidIso2108Isbn, isValidNlBsn,
//              isValidNlVat, isValidNlKvk, isValidNlPostcode, isValidNlPhone, isValidBePhone
//   Validator module shape (what emitted modules export):
//              Each named export has a stable signature so cross-module
//              registry composition is safe. A module emits a subset of these
//              based on the features its schema requires; the default export
//              mirrors the set of named exports.
//                validate(data [, registry]): boolean
//                  — always emitted
//                validateWithState(data, evaluatedState, location [, registry])
//                  — emitted when the schema tracks unevaluated* annotations
//                validateWithScope(data, scope, location [, registry])
//                  — emitted when the schema tracks $dynamicRef scope
//                validateWithScopeAndState(data, scope, evaluatedState, location [, registry])
//                  — emitted when both are tracked
//                schemaUri: string | null
//                fragmentValidators: Record<uri, { validate, ... }>
//                  — emitted when the schema declares reachable $id / $anchor
//                    / $dynamicAnchor fragments
//
// PROVISIONAL (reserved names for deferred features; shape subject to change
// when the owning phase lands — do not depend on these in MVP consumers):
//   Registry / CompiledValidatorScope / EvaluatedState exports below implement
//   the ABI described above; $recursiveRef / $recursiveAnchor scope behavior
//   is not yet covered.
//
// All format exports below are implemented in this runtime and are part of
// the frozen MVP ABI that emitted validators consume.

// ---- Helpers (frozen) -----------------------------------------------------

/**
 * RFC 6901 JSON Pointer segment escape: '~' -> '~0', '/' -> '~1'.
 * @param {string} segment
 * @returns {string}
 */
export function escapeJsonPointer(segment) {
    return segment.replace(/~/g, "~0").replace(/\//g, "~1");
}

// Lazy Intl.Segmenter probe. If unavailable (non-ICU builds), fall back to
// code-point counting — matches surrogate pairs but not combining marks.
// The gap is documented in KNOWN_LIMITATIONS.md and is acceptable for MVP.
let _segmenter = null;
let _segmenterProbed = false;
/** @type {Map<string, RegExp>} */
// Process-global within a loaded runtime module. This grows with distinct
// schema patterns and intentionally does not evict to keep hot-path lookups
// trivial for generated validators and benchmark runs.
const _regexCache = new Map();
function _getSegmenter() {
    if (_segmenterProbed) return _segmenter;
    _segmenterProbed = true;
    try {
        if (typeof Intl !== "undefined" && typeof Intl.Segmenter === "function") {
            _segmenter = new Intl.Segmenter(undefined, { granularity: "grapheme" });
        }
    } catch {
        _segmenter = null;
    }
    return _segmenter;
}

/**
 * Returns a cached ECMAScript-unicode RegExp instance for a schema pattern.
 * Generated validators call through this helper so regex compilation happens
 * once per module load instead of once per validation.
 * @param {string} pattern
 * @returns {RegExp}
 */
export function getCachedRegex(pattern) {
    let regex = _regexCache.get(pattern);
    if (regex === undefined) {
        regex = new RegExp(pattern, "u");
        _regexCache.set(pattern, regex);
    }
    return regex;
}

/**
 * Counts grapheme clusters in a string. Mirrors C# StringInfo.LengthInTextElements.
 * @param {string} str
 * @returns {number}
 */
export function graphemeLength(str) {
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
        // eslint-disable-next-line no-unused-vars
        for (const _ of seg.segment(str)) count++;
        return count;
    }
    return Array.from(str).length;
}

/**
 * Returns true if v is a JSON integer for validation purposes.
 * Accepts finite Number values with no fractional part. Rejects BigInt to
 * stay consistent with the IEEE-754 semantics used by the C# compiled path
 * and by every numeric-constraint emitter (minimum/maximum/multipleOf guard
 * on `typeof === "number"`); allowing BigInt here would let `type: integer`
 * accept values that silently skip numeric constraints.
 * @param {unknown} v
 * @returns {boolean}
 */
export function isInteger(v) {
    if (typeof v !== "number") return false;
    if (!Number.isFinite(v)) return false;
    return Math.floor(v) === v;
}

/**
 * Deep structural equality for parsed JSON values. Numeric equality is ===
 * (IEEE-754), matching the C# compiled path.
 * @param {unknown} a
 * @param {unknown} b
 * @returns {boolean}
 */
export function deepEquals(a, b) {
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
        if (Array.isArray(b) || typeof b !== "object") return false;
        const ak = Object.keys(a);
        const bk = Object.keys(b);
        if (ak.length !== bk.length) return false;
        for (const k of ak) {
            if (!Object.prototype.hasOwnProperty.call(b, k)) return false;
            if (!deepEquals(a[k], b[k])) return false;
        }
        return true;
    }
    return false;
}

// ---- Evaluation annotation state (provisional) ----------------------------

/**
 * Tracks evaluated object properties and array items by instance location for
 * unevaluatedProperties / unevaluatedItems.
 */
export class EvaluatedState {
    constructor() {
        /** @type {Map<string, Set<string>>} */
        this._properties = new Map();
        /** @type {Map<string, number>} */
        this._itemsUpTo = new Map();
        /** @type {Map<string, Set<number>>} */
        this._itemIndices = new Map();
    }

    /** @param {string} loc @param {string} propertyName */
    markPropertyEvaluated(loc, propertyName) {
        let set = this._properties.get(loc);
        if (set === undefined) {
            set = new Set();
            this._properties.set(loc, set);
        }
        set.add(propertyName);
    }

    /** @param {string} loc @param {string} propertyName @returns {boolean} */
    isPropertyEvaluated(loc, propertyName) {
        return this._properties.get(loc)?.has(propertyName) === true;
    }

    /** @param {string} loc @param {number} index */
    markItemEvaluated(loc, index) {
        let set = this._itemIndices.get(loc);
        if (set === undefined) {
            set = new Set();
            this._itemIndices.set(loc, set);
        }
        set.add(index);
    }

    /** @param {string} loc @param {number} count */
    setEvaluatedItemsUpTo(loc, count) {
        const current = this._itemsUpTo.get(loc) ?? 0;
        if (count > current) this._itemsUpTo.set(loc, count);
    }

    /** @param {string} loc @param {number} index @returns {boolean} */
    isItemEvaluated(loc, index) {
        if (index < (this._itemsUpTo.get(loc) ?? 0)) return true;
        return this._itemIndices.get(loc)?.has(index) === true;
    }

    reset() {
        this._properties.clear();
        this._itemsUpTo.clear();
        this._itemIndices.clear();
    }

    /** @returns {EvaluatedState} */
    clone() {
        const clone = new EvaluatedState();
        for (const [loc, props] of this._properties) {
            clone._properties.set(loc, new Set(props));
        }
        for (const [loc, count] of this._itemsUpTo) {
            clone._itemsUpTo.set(loc, count);
        }
        for (const [loc, indices] of this._itemIndices) {
            clone._itemIndices.set(loc, new Set(indices));
        }
        return clone;
    }

    /** @param {EvaluatedState} other */
    mergeFrom(other) {
        for (const [loc, props] of other._properties) {
            let target = this._properties.get(loc);
            if (target === undefined) {
                target = new Set();
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
                target = new Set();
                this._itemIndices.set(loc, target);
            }
            for (const index of indices) target.add(index);
        }
    }

    /** @param {EvaluatedState} snapshot */
    restoreFrom(snapshot) {
        this.reset();
        this.mergeFrom(snapshot);
    }
}

/**
 * Minimal URI-addressed validator registry for generated modules that contain
 * external $ref. Values can be validator functions or objects with validate().
 */
export class Registry {
    constructor() {
        /** @type {Map<string, unknown>} */
        this._validators = new Map();
    }

    /** @param {string} uri @param {unknown} validator */
    registerForUri(uri, validator) {
        this._validators.set(uri, validator);
    }

    /** @param {string} uri @returns {unknown | null} */
    tryGetValidator(uri) {
        return this._validators.get(uri) ?? null;
    }
}

/**
 * Immutable dynamic scope stack for $dynamicRef resolution.
 */
export class CompiledValidatorScope {
    static empty = new CompiledValidatorScope(null, null);

    constructor(parent, entry) {
        this._parent = parent;
        this._entry = entry;
    }

    push(entry) {
        if (entry == null || entry.dynamicAnchors == null || Object.keys(entry.dynamicAnchors).length === 0) {
            return this;
        }
        return new CompiledValidatorScope(this, entry);
    }

    tryResolveDynamicAnchor(anchorName) {
        const entries = [];
        let current = this;
        while (current != null) {
            if (current._entry?.dynamicAnchors != null) {
                entries.push(current._entry);
            }
            current = current._parent;
        }

        for (let i = entries.length - 1; i >= 0; i--) {
            const validator = entries[i].dynamicAnchors[anchorName] ?? null;
            if (validator != null) return validator;
        }

        return null;
    }
}

// ---- Format validators (frozen signatures) --------------------------------
//
// Each validator returns true for non-string inputs (format applies only to
// strings per the spec). For strings, it returns true iff the value matches
// the format. Implementations are pragmatic regex-based checks intended to
// match common test-suite verdicts; idn-* and iri-* variants fall back to
// their ASCII counterparts for MVP and are documented in KNOWN_LIMITATIONS.

function _nonString(v) { return typeof v !== "string"; }

const _reDateFullDate = /^(\d{4})-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$/;
const _reTimePart = /^(?<hour>[01][0-9]|2[0-3]):(?<minute>[0-5][0-9]):(?<second>[0-5][0-9]|60)(?:\.[0-9]+)?(?:[zZ]|(?<sign>[+-])(?<offsetHour>[0-9]{2}):(?<offsetMinute>[0-9]{2}))$/;
const _reDuration = /^P(?=.)(?:\d+W|(?:\d+Y)?(?:\d+M)?(?:\d+D)?(?:T(?=\d)(?:\d+H)?(?:\d+M)?(?:\d+(?:\.\d+)?S)?)?)$/;
const _reBasicEmail = /^.+@.+$/;
const _reLocalPart = /^(?:(?:[A-Za-z0-9!#$%&'*+\-/=?^_`{|}~]+(?:\.[A-Za-z0-9!#$%&'*+\-/=?^_`{|}~]+)*)|(?:"(?:[\sA-Za-z0-9!#$%&'*+\-\/=?^_`{|}~.,:;<>[\]\\@]|\\.)+"))$/;
const _reIdnLocalPart = /^(?:(?:[\p{L}\p{N}!#$%&'*+\-/=?^_`{|}~]+(?:\.[\p{L}\p{N}!#$%&'*+\-/=?^_`{|}~]+)*)|(?:"(?:[\s\p{L}\p{N}!#$%&'*+\-\/=?^_`{|}~.,:;<>[\]\\@]|\\.)+"))$/u;
const _reDomainPart = /^(?:[A-Za-z0-9.-]+\.[A-Za-z]{2,}|\[(?:(?:\d{1,3}\.){3}\d{1,3}|IPv6:[0-9A-Fa-f:.]+)\])$/;
const _reHostnameLabel = /^(?=.{1,63}$)[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?$/;
const _reCombiningMark = /^\p{M}$/u;
const _reIpv4Octet = /^(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)$/;
const _reUuid = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const _reJsonPointer = /^(\/([^~/]|~0|~1)*)*$/;
const _reRelativeJsonPointer = /^(0|[1-9]\d*)(#|(\/([^~/]|~0|~1)*)*)$/;
const _reUriTemplate = /^([^\x00-\x20"'<>%\\^`{|}]|%[0-9A-Fa-f]{2}|\{[+#./;?&=,!@|]?((\w|\.|%[0-9A-Fa-f]{2})+(\*|:\d+)?)(,(\w|\.|%[0-9A-Fa-f]{2})+(\*|:\d+)?)*\})*$/;

/** @type {(v: unknown) => boolean} */
export function isValidDate(v) {
    if (_nonString(v)) return true;
    const m = _reDateFullDate.exec(v);
    if (!m) return false;
    const year = +m[1], month = +m[2], day = +m[3];
    const daysInMonth = [31, (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0 ? 29 : 28,
        31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
    return day <= daysInMonth[month - 1];
}

/** @type {(v: unknown) => boolean} */
export function isValidTime(v) {
    if (_nonString(v)) return true;
    const m = _reTimePart.exec(v);
    if (!m) return false;
    const groups = m.groups ?? {};
    const hour = Number.parseInt(groups.hour ?? "", 10);
    const minute = Number.parseInt(groups.minute ?? "", 10);
    const second = Number.parseInt(groups.second ?? "", 10);

    if (second === 60) {
        if (!_isValidLeapSecond(hour, minute, groups)) return false;
    } else if (!_tryDate(`1970-01-01T${v}`)) {
        return false;
    }

    if (groups.sign) {
        const offsetHours = Number.parseInt(groups.offsetHour ?? "", 10);
        const offsetMinutes = Number.parseInt(groups.offsetMinute ?? "", 10);
        if (offsetHours > 23 || offsetMinutes > 59) return false;
    }
    return true;
}

function _isValidLeapSecond(hour, minute, groups) {
    if (!groups.sign) {
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

function _tryDate(value) {
    try {
        return !Number.isNaN(Date.parse(value));
    } catch {
        return false;
    }
}

/** @type {(v: unknown) => boolean} */
export function isValidDateTime(v) {
    if (_nonString(v)) return true;
    const tIdx = v.search(/[Tt]/);
    if (tIdx < 0) return false;
    return isValidDate(v.slice(0, tIdx)) && isValidTime(v.slice(tIdx + 1));
}

/** @type {(v: unknown) => boolean} */
export function isValidDuration(v) {
    if (_nonString(v)) return true;
    return _reDuration.test(v);
}

/** @type {(v: unknown) => boolean} */
export function isValidEmail(v) {
    if (_nonString(v)) return true;
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

/** @type {(v: unknown) => boolean} */
export function isValidIdnEmail(v) {
    if (_nonString(v)) return true;
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

/** @type {(v: unknown) => boolean} */
export function isValidHostname(v) {
    if (_nonString(v)) return true;
    if (v.length === 0 || v.length > 253) return false;
    if (v.includes("\uFF0E") || v.includes("\u3002") || v.includes("\uFF61")) return false;
    return _validateHostnameLabels(v.split("."), false);
}

/** @type {(v: unknown) => boolean} */
export function isValidIdnHostname(v) {
    if (_nonString(v)) return true;
    if (v.length === 0) return false;

    const normalized = v.replace(/[\u3002\uFF0E\uFF61]/g, ".");
    return _validateHostnameLabels(normalized.split("."), true);
}

/** @type {(v: unknown) => boolean} */
export function isValidIpv4(v) {
    if (_nonString(v)) return true;
    const parts = v.split(".");
    if (parts.length !== 4) return false;
    return parts.every((p) => _reIpv4Octet.test(p));
}

/** @type {(v: unknown) => boolean} */
export function isValidIpv6(v) {
    if (_nonString(v)) return true;
    if (v.includes("%")) return false;
    const parts = v.split("::");
    if (parts.length > 2) return false;

    const hasIpv4Tail = v.includes(".");
    const parseGroups = (segment) => {
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

function _validateHostnameLabels(labels, allowUnicode) {
    if (labels.length === 0) return false;
    let totalLength = labels.length - 1;
    for (const label of labels) {
        totalLength += label.length;
        if (label.length === 0 || label.length > 63) return false;
        if (!_validateHostnameLabel(label, allowUnicode)) return false;
    }
    return totalLength <= 253;
}

function _validateHostnameLabel(label, allowUnicode) {
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

function _validateIdnLabelContextualRules(label) {
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

function _validateUnicodeHostnameLabel(label) {
    for (const c of label) {
        if (c === ".") return false;
    }
    return !label.startsWith("-") && !label.endsWith("-");
}

function _isCombiningMark(c) {
    return _reCombiningMark.test(c);
}

function _isGreek(c) {
    const cp = c.codePointAt(0);
    return (cp >= 0x0370 && cp <= 0x03FF) || (cp >= 0x1F00 && cp <= 0x1FFF);
}

function _isHebrew(c) {
    const cp = c.codePointAt(0);
    return cp >= 0x05D0 && cp <= 0x05EA;
}

function _isCjk(c) {
    const cp = c.codePointAt(0);
    return (cp >= 0x3040 && cp <= 0x309F) ||
        (cp >= 0x30A0 && cp <= 0x30FF && cp !== 0x30FB) ||
        (cp >= 0x31F0 && cp <= 0x31FF) ||
        (cp >= 0x3400 && cp <= 0x4DBF) ||
        (cp >= 0x4E00 && cp <= 0x9FFF);
}

function _isVirama(c) {
    const cp = c.codePointAt(0);
    return cp === 0x094D || cp === 0x09CD || cp === 0x0A4D || cp === 0x0ACD ||
        cp === 0x0B4D || cp === 0x0BCD || cp === 0x0C4D || cp === 0x0CCD ||
        cp === 0x0D4D || cp === 0x0DCA || cp === 0x0E3A || cp === 0x0F84 ||
        cp === 0x1039 || cp === 0x1714 || cp === 0x1734 || cp === 0x17D2 ||
        cp === 0x1A60 || cp === 0x1B44 || cp === 0x1BAA || cp === 0xA806 ||
        cp === 0xA8C4 || cp === 0xA953 || cp === 0xA9C0 || cp === 0xAAF6;
}

function _decodePunycodeLabel(input) {
    const base = 36;
    const tMin = 1;
    const tMax = 26;
    const skew = 38;
    const damp = 700;
    const initialBias = 72;
    const initialN = 128;

    const decodeDigit = (codePoint) => {
        if (codePoint >= 48 && codePoint <= 57) return codePoint - 22;
        if (codePoint >= 65 && codePoint <= 90) return codePoint - 65;
        if (codePoint >= 97 && codePoint <= 122) return codePoint - 97;
        return base;
    };

    const adapt = (delta, numPoints, firstTime) => {
        delta = firstTime ? Math.floor(delta / damp) : delta >> 1;
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
    const output = [];

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
        i %= (output.length + 1);
        output.splice(i, 0, n);
        i++;
    }

    try {
        return String.fromCodePoint(...output);
    } catch {
        return null;
    }
}

function _containsInvalidUriCharacters(uri, iriSupport, isTemplate) {
    if (isTemplate) return false;
    for (const c of uri) {
        if (!iriSupport && c.codePointAt(0) > 127) return true;
        if (c === " " || c === "<" || c === ">" || c === "\"" ||
            c === "{" || c === "}" || c === "|" || c === "\\" ||
            c === "^" || c === "`") {
            return true;
        }
    }
    return false;
}

function _isAbsoluteUriWithoutAuthority(uri) {
    const lower = uri.toLowerCase();
    return lower.startsWith("urn:") ||
        lower.startsWith("tag:") ||
        lower.startsWith("mailto:") ||
        lower.startsWith("news:") ||
        lower.startsWith("tel:");
}

function _validateUriLike(v, iriSupport, canBeRelative) {
    if (typeof v !== "string") return false;
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

function _validateAbsoluteUri(uri, iriSupport) {
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

function _validateAuthority(rest, iriSupport) {
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

/** @type {(v: unknown) => boolean} */
export function isValidUri(v) {
    if (_nonString(v)) return true;
    return /^[A-Za-z][A-Za-z0-9+\-.]*:/.test(v) && _validateUriLike(v, false, false);
}

/** @type {(v: unknown) => boolean} */
export function isValidUriReference(v) {
    if (_nonString(v)) return true;
    return _validateUriLike(v, false, true);
}

/** @type {(v: unknown) => boolean} */
export function isValidIri(v) {
    if (_nonString(v)) return true;
    return /^[A-Za-z][A-Za-z0-9+\-.]*:/.test(v) && _validateUriLike(v, true, false);
}

/** @type {(v: unknown) => boolean} */
export function isValidIriReference(v) {
    if (_nonString(v)) return true;
    return _validateUriLike(v, true, true);
}

/** @type {(v: unknown) => boolean} */
export function isValidUriTemplate(v) {
    if (_nonString(v)) return true;
    return _reUriTemplate.test(v);
}

/** @type {(v: unknown) => boolean} */
export function isValidJsonPointer(v) {
    if (_nonString(v)) return true;
    return _reJsonPointer.test(v);
}

/** @type {(v: unknown) => boolean} */
export function isValidRelativeJsonPointer(v) {
    if (_nonString(v)) return true;
    return _reRelativeJsonPointer.test(v);
}

/** @type {(v: unknown) => boolean} */
export function isValidRegex(v) {
    if (_nonString(v)) return true;
    try {
        // Validate against the stricter ECMA-262 grammar rather than Annex B's
        // permissive legacy identity escapes such as "\a".
        // eslint-disable-next-line no-new
        new RegExp(v, "u");
        return true;
    } catch {
        return false;
    }
}

/** @type {(v: unknown) => boolean} */
export function isValidUuid(v) {
    if (_nonString(v)) return true;
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
const _extIbanLengths = {
    AD: 24, AE: 23, AL: 28, AT: 20, AZ: 28, BA: 20, BE: 16, BG: 22, BH: 22, BI: 27, BR: 29, BY: 28,
    CH: 21, CR: 22, CY: 28, CZ: 24, DE: 22, DJ: 27, DK: 18, DO: 28, EE: 20, EG: 29, ES: 24, FI: 18,
    FK: 18, FO: 18, FR: 27, GB: 22, GE: 22, GI: 23, GL: 18, GR: 27, GT: 28, HN: 28, HR: 21, HU: 28,
    IE: 22, IL: 23, IQ: 23, IS: 26, IT: 27, JO: 30, KW: 30, KZ: 20, LB: 28, LC: 32, LI: 21, LT: 20,
    LU: 20, LV: 21, LY: 25, MC: 27, MD: 24, ME: 22, MK: 19, MN: 20, MR: 27, MT: 31, MU: 30, NI: 28,
    NL: 18, NO: 15, OM: 23, PK: 24, PL: 28, PS: 29, PT: 25, QA: 29, RO: 24, RS: 22, RU: 33, SA: 24,
    SC: 31, SD: 18, SE: 24, SI: 19, SK: 24, SM: 27, SO: 23, ST: 25, SV: 28, TL: 23, TN: 24, TR: 26,
    UA: 29, VA: 22, VG: 24, XK: 20, YE: 30,
};

function _extIsDigit(c) {
    return c.length === 1 && c >= "0" && c <= "9";
}

function _extIsLetter(c) {
    return c.length === 1 && ((c >= "A" && c <= "Z") || (c >= "a" && c <= "z"));
}

function _extIsSpace(c) {
    return c === " " || c === "\u00A0";
}

function _extUpper(c) {
    return c >= "a" && c <= "z" ? String.fromCharCode(c.charCodeAt(0) - 32) : c;
}

function _extTrim(value) {
    let start = 0;
    let end = value.length;
    while (start < end && _extTrimChars.includes(value.charAt(start))) start++;
    while (end > start && _extTrimChars.includes(value.charAt(end - 1))) end--;
    return value.slice(start, end);
}

// Removes separators and upper-cases ASCII letters. Null on any other character or an
// empty result.
function _extCompact(value, separators, allowLetters) {
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
function _extMod97(text) {
    let remainder = 0;
    for (let i = 0; i < text.length; i++) {
        const code = text.charCodeAt(i);
        remainder = code >= 48 && code <= 57
            ? (remainder * 10 + (code - 48)) % 97
            : (remainder * 100 + (code - 55)) % 97;
    }
    return remainder;
}

function _extElfproef(nineDigits) {
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

function _extIban(value) {
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

function _extBic(value) {
    const bic = _extCompact(value, _extSpaces, true);
    if (bic === null || (bic.length !== 8 && bic.length !== 11)) return false;
    // Business party prefix and suffix are alphanumeric (ISO 9362:2014); the country code is letters.
    return _extIsLetter(bic.charAt(4)) && _extIsLetter(bic.charAt(5));
}

function _extStartsWithIsbn(text) {
    return text.length >= 4 &&
        _extUpper(text.charAt(0)) === "I" && _extUpper(text.charAt(1)) === "S" &&
        _extUpper(text.charAt(2)) === "B" && _extUpper(text.charAt(3)) === "N";
}

function _extIsbn13(value) {
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

function _extNlBsn(value) {
    let digits = _extCompact(value, _extSpacesDotsAndDashes, false);
    if (digits === null) return false;
    if (digits.length === 8) digits = "0" + digits;
    return digits.length === 9 && _extElfproef(digits);
}

function _extNlVat(value) {
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

function _extNlKvk(value) {
    const digits = _extCompact(value, _extSpaces, false);
    return digits !== null && digits.length === 8 && /[1-9]/.test(digits);
}

function _extNlPostcode(value) {
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

// Significant (national) digits of a phone number for one country calling code, or null.
// Accepts +CC, 00CC and national 0 prefixes, an optional trunk 0 after the country code
// (written as "(0)" or not), and separators.
function _extParsePhone(value, countryCode) {
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

    let rest;
    if (international) {
        if (!digits.startsWith(countryCode)) return null;
        rest = digits.slice(countryCode.length);
    } else if (digits.startsWith("00")) {
        if (!digits.startsWith(countryCode, 2)) return null;
        rest = digits.slice(2 + countryCode.length);
    } else {
        if (digits.length < 2 || digits.charAt(0) !== "0") return null;
        return digits.slice(1);
    }
    if (rest.startsWith("0")) rest = rest.slice(1);
    if (rest.length === 0 || rest.charAt(0) === "0") return null;
    return rest;
}

function _extNlPhone(value) {
    const national = _extParsePhone(value, "31");
    return national !== null && national.length === 9;
}

function _extBePhone(value) {
    const national = _extParsePhone(value, "32");
    if (national === null) return false;
    if (national.length === 8) return true;
    return national.length === 9 && national.charAt(0) === "4" && national.charAt(1) >= "5";
}

export function isValidIso13616Iban(v) {
    if (_nonString(v)) return true;
    return _extIban(v);
}

export function isValidIso9362Bic(v) {
    if (_nonString(v)) return true;
    return _extBic(v);
}

export function isValidIso2108Isbn(v) {
    if (_nonString(v)) return true;
    return _extIsbn13(v);
}

export function isValidNlBsn(v) {
    if (_nonString(v)) return true;
    return _extNlBsn(v);
}

export function isValidNlVat(v) {
    if (_nonString(v)) return true;
    return _extNlVat(v);
}

export function isValidNlKvk(v) {
    if (_nonString(v)) return true;
    return _extNlKvk(v);
}

export function isValidNlPostcode(v) {
    if (_nonString(v)) return true;
    return _extNlPostcode(v);
}

export function isValidNlPhone(v) {
    if (_nonString(v)) return true;
    return _extNlPhone(v);
}

export function isValidBePhone(v) {
    if (_nonString(v)) return true;
    return _extBePhone(v);
}

// ---- Provisional deferred-feature shapes (reference-only, do not depend on) --
//
// @typedef {object} EvaluatedState
// @property {(loc: string, propertyName: string) => void} markPropertyEvaluated
// @property {(loc: string, propertyName: string) => boolean} isPropertyEvaluated
// @property {(loc: string, index: number) => void} markItemEvaluated
// @property {(loc: string, index: number) => boolean} isItemEvaluated
// @property {() => void} reset
// @property {() => EvaluatedState} clone
// @property {(other: EvaluatedState) => void} mergeFrom
//
// @typedef {object} CompiledScopeEntry
// @property {Record<string, Function> | null} dynamicAnchors
// @property {boolean} hasRecursiveAnchor
// @property {Function | null} rootValidator
//
// @typedef {object} CompiledValidatorScope
// @property {(entry: CompiledScopeEntry) => CompiledValidatorScope} push
//
// @typedef {object} Registry
// @property {(hash: string, validator: unknown) => void} registerByHash
// @property {(uri: string, validator: unknown) => void} registerForUri
// @property {(uri: string) => unknown | null} tryGetValidator
