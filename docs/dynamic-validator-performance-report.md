# Dynamic Validator Performance Report

## Summary
Benchmarks show that the dynamic validator’s latency and allocations rise sharply with schema complexity. The primary drivers appear to be repeated dynamic scope traversal, deep metadata copies during $dynamicRef resolution, and lack of caching for dynamic reference validators. This report highlights likely hot paths and proposes concrete, high-impact tasks to reduce CPU and memory usage.

---

## Key Findings

1) **$dynamicRef resolution is allocation-heavy and O(n²) in scope depth**
- `DynamicRefValidator.ResolveDynamicRef` enumerates the dynamic scope using `Skip(i).Any()` and `ElementAt(i)`, which re-traverses the enumerable for each iteration.
- This creates repeated enumerators and grows costs with scope depth.

2) **Dynamic anchors create deep `SchemaMetadata` copies**
- On dynamic anchor hits, `ResolveDynamicRef` constructs `new SchemaMetadata(schemaResource)`, which copies `Anchors`, `DynamicAnchors`, `References`, and other fields.
- In complex schemas with frequent $dynamicRef, this produces significant allocations.

3) **Dynamic ref validators are not cached**
- `$ref` uses a cached lazy validator, but `$dynamicRef` does a fresh `GetSchema(...)` + `CreateValidator(...)` on every call.
- Repeated dynamic refs to the same anchor multiply this cost.

4) **SchemaRepository.GetSchema always clones metadata**
- Even for non-fragment URIs, `GetSchema` returns a new `SchemaMetadata` copy.
- This contributes to higher allocations in any path that repeatedly resolves schemas at runtime.

5) **Fast-path contexts still allocate per sub-schema**
- In `IsValid`, applicators create new fast contexts for every branch/sub-schema.
- Complex schemas (allOf/anyOf/oneOf/if/then/else) therefore allocate heavily even in fast paths.

---

## High-Impact Tasks (Best ROI)

1) **Replace dynamic scope traversal LINQ with a single pass**
- **Why:** Remove repeated enumeration and avoid O(n²) behavior.
- **Where:** `ValidationScope` + `DynamicRefValidator`.
- **Approach:** Add a `GetDynamicScopeSnapshot()` that returns a `SchemaMetadata[]` (outermost → innermost) via `Stack.ToArray()` + reverse index loop, then iterate by index.
- **Expected impact:** Significant CPU + allocation reduction for complex dynamic scopes.

2) **Cache resolved dynamic validators**
- **Why:** Avoid re-creating validators for identical resolved URIs.
- **Where:** `DynamicRefValidator`.
- **Approach:** Add a `ConcurrentDictionary<Uri, ISchemaValidator>` and a simple “last resolved” fast path.
- **Expected impact:** Large savings for repeated dynamic refs.

3) **Avoid deep SchemaMetadata copies for dynamic anchors**
- **Why:** Current anchor resolution clones metadata each time.
- **Where:** `SchemaRepository` + `SchemaMetadata` + `DynamicRefValidator`.
- **Approach options:**
  - Store anchors as `Dictionary<string, SchemaMetadata>` instead of `JsonElement`.
  - Add a lightweight “view” metadata type that shares dictionaries and only overrides `Schema`/`SchemaUri`.
  - Add a `CreateShallowView` factory to avoid copying.
- **Expected impact:** Major allocation reduction when resolving dynamic anchors.

4) **Stop cloning SchemaMetadata for non-fragment lookups**
- **Why:** Cloning even for base URIs adds repeated allocations.
- **Where:** `SchemaRepository.GetSchema`.
- **Approach:** Return canonical metadata for non-fragment URIs; only clone when modifications are required (e.g., fallback keyword filtering).
- **Expected impact:** Broad allocation reduction across validation.

---

## Medium-Impact Tasks

5) **Pre-parse and cache $dynamicRef URI/fragment**
- **Why:** Avoid repeated `Uri.TryCreate` and fragment parsing.
- **Where:** `DynamicRefValidator`.
- **Approach:** Precompute resolved base/fragment and JSON-pointer status in constructor.
- **Expected impact:** Small but consistent CPU savings.

6) **Pool or reuse fast validation contexts**
- **Why:** Fast contexts allocate per sub-schema in complex graphs.
- **Where:** `JsonValidationContextFactory` + applicator validators.
- **Approach:** Introduce a small pool or resettable contexts for fast paths when annotations aren’t needed.
- **Expected impact:** Moderate allocation reduction in complex `IsValid` paths.

---

## Lower-Impact Tasks

7) **Reduce Uri allocations when constructing anchored schema URIs**
- **Where:** `DynamicRefValidator.ResolveDynamicRef`.
- **Approach:** Cache anchor URIs or store base+fragment separately to avoid `new Uri(...)` per hit.
- **Expected impact:** Small but helps in hot loops.

---

## Suggested Implementation Sequence

1) Add `GetDynamicScopeSnapshot()` and replace LINQ traversal.
2) Add dynamic validator caching in `$dynamicRef`.
3) Introduce shallow metadata views or anchor metadata caching.
4) Optimize `GetSchema` to avoid unnecessary cloning.
5) Pre-parse `$dynamicRef` and consider context pooling.

---

## Benchmarking / Validation

- Add or extend benchmarks for:
  - deep dynamic scope depth
  - repeated $dynamicRef to same anchor
- Use `MemoryDiagnoser` and dotnet profiling to confirm allocation hotspots.
- Re-run the complex benchmark scenario to verify reductions in allocations and mean time.
