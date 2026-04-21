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
//   Helpers:   deepEquals, escapeJsonPointer, graphemeLength, isInteger
//   Formats:   isValidDateTime, isValidDate, isValidTime, isValidDuration,
//              isValidEmail, isValidIdnEmail, isValidHostname, isValidIdnHostname,
//              isValidIpv4, isValidIpv6, isValidUri, isValidUriReference,
//              isValidUriTemplate, isValidIri, isValidIriReference,
//              isValidJsonPointer, isValidRelativeJsonPointer, isValidRegex,
//              isValidUuid
//   Validator module shape (what emitted modules export):
//              default export: { validate, schemaUri }
//              named exports:  validate(data): boolean, schemaUri: string | null
//
// PROVISIONAL (reserved names for deferred features; shape subject to change
// when the owning phase lands — do not depend on these in MVP consumers):
//   EvaluatedState        — unevaluatedProperties / unevaluatedItems tracking
//   CompiledValidatorScope — $dynamicRef / $recursiveRef scope stack
//   Registry              — external $ref resolution
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
 * Counts grapheme clusters in a string. Mirrors C# StringInfo.LengthInTextElements.
 * @param {string} str
 * @returns {number}
 */
export function graphemeLength(str) {
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

// ---- Format validators (frozen signatures) --------------------------------
//
// Each validator returns true for non-string inputs (format applies only to
// strings per the spec). For strings, it returns true iff the value matches
// the format. Implementations are pragmatic regex-based checks intended to
// match common test-suite verdicts; idn-* and iri-* variants fall back to
// their ASCII counterparts for MVP and are documented in KNOWN_LIMITATIONS.

function _nonString(v) { return typeof v !== "string"; }

const _reDateFullDate = /^(\d{4})-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$/;
// Time shape: HH:MM:SS[.fraction](Z|±HH:MM). Field ranges checked separately —
// a shape-only regex lets "25:00:00Z" through. Range check in isValidTime.
const _reTimePart = /^(\d{2}):(\d{2}):(\d{2})(\.\d+)?(Z|[+-]\d{2}:\d{2})$/i;
const _reDuration = /^P(?!$)(\d+Y)?(\d+M)?(\d+W)?(\d+D)?(T(?=\d)(\d+H)?(\d+M)?(\d+(\.\d+)?S)?)?$/;
const _reEmail = /^[^\s@"]+@[^\s@"]+\.[^\s@"]+$/;
const _reHostnameLabel = /^(?=.{1,63}$)[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?$/;
const _reIpv4Octet = /^(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)$/;
const _reIpv6 =
    /^(([0-9A-Fa-f]{1,4}:){7}[0-9A-Fa-f]{1,4}|([0-9A-Fa-f]{1,4}:){1,7}:|([0-9A-Fa-f]{1,4}:){1,6}:[0-9A-Fa-f]{1,4}|([0-9A-Fa-f]{1,4}:){1,5}(:[0-9A-Fa-f]{1,4}){1,2}|([0-9A-Fa-f]{1,4}:){1,4}(:[0-9A-Fa-f]{1,4}){1,3}|([0-9A-Fa-f]{1,4}:){1,3}(:[0-9A-Fa-f]{1,4}){1,4}|([0-9A-Fa-f]{1,4}:){1,2}(:[0-9A-Fa-f]{1,4}){1,5}|[0-9A-Fa-f]{1,4}:((:[0-9A-Fa-f]{1,4}){1,6})|:((:[0-9A-Fa-f]{1,4}){1,7}|:))$/;
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
    const hour = +m[1], minute = +m[2], second = +m[3];
    // RFC 3339: hour 00-23, minute 00-59, second 00-60 (leap second allowed).
    if (hour > 23 || minute > 59 || second > 60) return false;
    // Offsets: ±HH:MM with HH in 00-23 and MM in 00-59.
    const offset = m[5];
    if (offset && offset.toUpperCase() !== "Z") {
        const offHour = +offset.slice(1, 3);
        const offMin = +offset.slice(4, 6);
        if (offHour > 23 || offMin > 59) return false;
    }
    return true;
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
    return _reEmail.test(v);
}

/** @type {(v: unknown) => boolean} */
export const isValidIdnEmail = isValidEmail;

/** @type {(v: unknown) => boolean} */
export function isValidHostname(v) {
    if (_nonString(v)) return true;
    if (v.length > 253) return false;
    return v.split(".").every((label) => _reHostnameLabel.test(label));
}

/** @type {(v: unknown) => boolean} */
export const isValidIdnHostname = isValidHostname;

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
    return _reIpv6.test(v);
}

function _tryUrl(v, base) {
    try {
        // eslint-disable-next-line no-new
        new URL(v, base);
        return true;
    } catch {
        return false;
    }
}

/** @type {(v: unknown) => boolean} */
export function isValidUri(v) {
    if (_nonString(v)) return true;
    // Absolute URIs only — must parse standalone and contain a scheme.
    if (!/^[A-Za-z][A-Za-z0-9+\-.]*:/.test(v)) return false;
    return _tryUrl(v);
}

/** @type {(v: unknown) => boolean} */
export function isValidUriReference(v) {
    if (_nonString(v)) return true;
    return _tryUrl(v, "http://example.com/");
}

/** @type {(v: unknown) => boolean} */
export const isValidIri = isValidUri;

/** @type {(v: unknown) => boolean} */
export const isValidIriReference = isValidUriReference;

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
        // eslint-disable-next-line no-new
        new RegExp(v);
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
