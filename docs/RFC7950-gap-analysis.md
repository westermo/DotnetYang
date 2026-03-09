# RFC 7950 Feature Gap Analysis & Remediation Plan

**Project:** dotnetYang — Roslyn source generator for YANG → C#  
**Date:** 2025-02-26  
**Standard:** RFC 7950 (YANG 1.1), RFC 6020 (YANG 1.0)  
**Status:** Phases 1-3 IMPLEMENTED ✅

---

## Executive Summary

The dotnetYang project has **solid coverage** of the core YANG data modeling features. The parser handles all keywords, all 19 builtin types are implemented, and the main code-generation pipeline (module → container/list/leaf → XML serialization) is mature. A systematic comparison against RFC 7950 revealed **18 feature gaps** ranging from critical (deviations silently ignored) to low priority (convenience features like key-based equality).

**Phases 1-3 have been implemented**, fixing all P0/P1/P2 gaps:
- ✅ `deviation`/`deviate` fully implemented with all four modes
- ✅ `grouping` now accepts `action`/`notification` per RFC 7950 §7.12
- ✅ `refine` correctly replaces singleton sub-statements instead of duplicating
- ✅ `anyxml`/`anydata` now support XML parsing (read) via `ReadInnerXml()`
- ✅ `Must` attribute includes `error-app-tag`/`error-message` properties
- ✅ Submodule `PrefixToNamespaceTable` propagation fixed
- ✅ `Cardinality.OneOrMore` validation added
- ✅ `Leaf` reads Default/Mandatory/Type lazily to support refine/deviate
- ✅ `error-app-tag` and `deviate` added to `StatementFactory`
- ✅ `anydata` added to `Container.PermittedChildren`

---

## Gap Inventory

| # | Gap | Priority | Complexity | RFC Section | Status |
|---|-----|----------|-----------|-------------|--------|
| 1 | `deviation`/`deviate` parsed but never applied | P0 Critical | L | §7.20.3 | ✅ Done |
| 2 | `grouping` missing `action`/`notification` in PermittedChildren | P1 High | S | §7.12 | ✅ Done |
| 3 | `refine` uses insert instead of replace for singleton statements | P1 High | M | §7.13.2 | ✅ Done |
| 4 | `anyxml` has no XML parsing (read) support | P2 Medium | M | §7.10 | ✅ Done |
| 5 | `anydata` has no XML parsing (read) support | P2 Medium | M | §7.11 | ✅ Done |
| 6 | `when` XPath expression not evaluated at runtime | P3 Low | — | §7.21.5 | By Design |
| 7 | `must` XPath expression not evaluated at runtime | P3 Low | — | §7.5.3 | By Design |
| 8 | `if-feature` not used for conditional code generation | P3 Low | M | §7.20.2 | Future |
| 9 | Submodule `belongs-to` prefix/namespace propagation incomplete | P3 Low | S | §7.2.2 | ✅ Done |
| 10 | `ordered-by user` not reflected in edit-config semantics | P3 Low | — | §7.7.7 | By Design |
| 11 | `unique` constraint not enforced | P3 Low | S | §7.8.3 | Future |
| 12 | `mandatory` on `choice` not enforced | P3 Low | S | §7.9.4 | Future |
| 13 | `min-elements`/`max-elements` not enforced at runtime | P3 Low | S | §7.7.3/4 | Future |
| 14 | `key` not used for Equals/GetHashCode generation | P3 Low | M | §7.8.2 | Future |
| 15 | `instance-identifier` is a string wrapper, no path resolution | P3 Low | — | §9.13 | By Design |
| 16 | YANG 1.0 backward compatibility not enforced | P3 Low | M | RFC 6020 | Future |
| 17 | `error-app-tag` from `must` not propagated to attribute | P3 Low | S | §7.5.4.2 | ✅ Done |
| 18 | Multiple revisions of same module overwrite each other | P3 Low | M | §5.6.4 | Future |

**Legend:** P0=Critical, P1=High, P2=Medium, P3=Low | S=Small, M=Medium, L=Large

---

## Detailed Gap Analysis

### GAP 1: `deviation`/`deviate` — Parsed but NOT Applied ⚠️ CRITICAL

**RFC 7950 §7.20.3:** The `deviation` statement declares that a device does not fully comply with a module. The `deviate` sub-statement specifies how: `not-supported` (remove node), `add` (add properties), `replace` (replace properties), `delete` (delete properties).

**Current State:**
- `Deviation.cs` — Parses the statement, validates children (`description`, `deviate`, `reference`), but does **not** override `ToCode()` (inherits the empty-string default from `Statement`).
- `Deviate.cs` — Parses and validates arguments (`not-supported`, `add`, `replace`, `delete`) and sub-statements (`config`, `default`, `mandatory`, `max-elements`, `min-elements`, `must`, `type`, `unique`, `units`). But has no processing logic.
- `YangGenerator.cs` — The pipeline calls `IncludeSubmodules()`, `UnwrapUses()`, `InjectAugments()` but has **no** `ApplyDeviations()` step.
- `TopLevelStatement.cs` — The constructor collects `Augments`, `Uses`, `Groupings`, `Identities`, etc. but does **not** collect `Deviation` instances into any list.

**Impact:** Deviations are silently ignored. A `deviate not-supported` on a mandatory leaf will still generate that leaf in C#, potentially causing runtime failures when communicating with a device that actually deviates.

**Files to Change:**
- `TopLevelStatement.cs` — Add `List<Deviation> Deviations` collection
- `ITopLevelStatement.cs` — Add `Deviations` to interface  
- `Deviation.cs` — Add `Apply()` method with target resolution (similar to `Augment.Inject()`)
- `Deviate.cs` — Add `ApplyTo(IStatement target)` implementing the four deviate modes
- `YangGenerator.cs` — Add `ApplyDeviations()` step after `InjectAugments()`

**Implementation Approach:**
1. Collect `Deviation` instances during `TopLevelStatement` construction (same pattern as `Augments`)
2. Add a new pipeline step `ApplyDeviations()` in `YangGenerator.MakeClasses()`, after `InjectAugments()` but before `ToCode()`
3. `Deviation.Apply()` resolves the target node using the schema path (similar to `Augment.GetTarget()`)
4. For each `Deviate` child:
   - `not-supported`: Remove the target from its parent via `parent.Replace(target, [])`
   - `add`: `target.Insert(deviate.Children)` (add properties)
   - `replace`: For each sub-statement in deviate, find and replace the matching sub-statement in target
   - `delete`: For each sub-statement in deviate, find and remove the matching sub-statement from target

**Risk:** `Leaf` caches `Type`, `Default`, and `Required` in its constructor. If deviation replaces these after construction, the cached values will be stale. Mitigation: Make `Leaf.ToCode()` re-read these from `Children` instead of using cached fields, or apply deviations before `Uses` expansion creates the final `Leaf` instances.

**RFC Sections to Consult:** §7.20.3 (deviation), §7.20.3.2 (deviate not-supported), §7.20.3.3 (deviate add), §7.20.3.4 (deviate replace), §7.20.3.5 (deviate delete)

---

### GAP 2: `grouping` Missing `action`/`notification` in PermittedChildren

**RFC 7950 §7.12:** A `grouping` may contain: `action`, `anydata`, `anyxml`, `choice`, `container`, `description`, `grouping`, `leaf`, `leaf-list`, `list`, `notification`, `reference`, `status`, `typedef`, `uses`.

**Current State:** `Grouping.PermittedChildren` includes `anydata`, `anyxml`, `choice`, `container`, `description`, `grouping`, `leaf`, `leaf-list`, `list`, `reference`, `status`, `typedef`, `uses` — but is **missing** `Action.Keyword` and `Notification.Keyword`.

**Impact:** Valid YANG files that define `action` or `notification` inside a `grouping` will fail to parse with a validation error.

**Files to Change:**
- `Grouping.cs` — Add `new ChildRule(Action.Keyword, Cardinality.ZeroOrMore)` and `new ChildRule(Notification.Keyword, Cardinality.ZeroOrMore)` to `PermittedChildren`

**Complexity:** Trivial — 2 lines added.

---

### GAP 3: `refine` Uses Insert Instead of Replace

**RFC 7950 §7.13.2:** The `refine` statement refines nodes brought in by `uses`. For singleton sub-statements (`default`, `description`, `reference`, `config`, `mandatory`, `presence`, `min-elements`, `max-elements`), the refine value **replaces** the existing value. For `must`, the refine **adds** new constraints.

**Current State:** In `Grouping.WithUse()` (line ~150-167), refine is processed as:
```csharp
current?.Insert(refinement.Children);
```
This **always inserts** (adds) children. For singleton sub-statements like `default`, this means the node may end up with two `default` children — the original from the grouping and the refined one. When `Leaf.ToCode()` processes children, it takes the *first* `DefaultValue` child, meaning the refine is ignored.

**Impact:** Refine operations on `default`, `mandatory`, `description`, `reference`, `config`, `presence`, `min-elements`, `max-elements` silently fail — the original value persists.

**Files to Change:**
- `Grouping.cs` — Replace the simple `Insert(refinement.Children)` with logic that:
  1. For each child of the refinement, check if a singleton sub-statement of that type already exists on the target
  2. If yes, replace it via `target.Replace(existing, [refined])`
  3. If no, insert it via `target.Insert([refined])`

**Singleton sub-statements to replace:** `DefaultValue`, `Description`, `Reference`, `Config`, `Mandatory`, `Presence`, `MinElements`, `MaxElements`  
**Sub-statements to add (accumulate):** `Must`

**RFC Sections:** §7.13.2 (refine statement)

---

### GAP 4 & 5: `anyxml` and `anydata` — No XML Parsing Support

**RFC 7950 §7.10/§7.11:** `anyxml` represents arbitrary XML content; `anydata` represents arbitrary YANG-modeled data.

**Current State:**
- `AnyXml` generates a `string?` property and a write call (implements no read interface)
- `AnyData` implements `IXMLWriteValue` — generates a `string?` property and write call, but NOT `IXMLReadValue`
- Neither class participates in `CollectParsingChildren()` in `Statement.cs`, which only handles `IXMLParseable`, `IXMLReadValue`, and `IXMLAction`
- When the parser encounters an anyxml/anydata element, it throws: `"Unexpected element '{name}' under '{parent}'"`

**Impact:** Runtime parsing of XML containing anyxml/anydata nodes always fails.

**Files to Change:**
- `AnyXml.cs` — Implement `IXMLReadValue` interface, add `ClassName` and `ParseCall` properties
- `AnyData.cs` — Implement `IXMLReadValue` interface, add `ClassName` and `ParseCall` properties

**Implementation Approach:**
Both should use `reader.ReadInnerXml()` to capture the arbitrary XML content as a string:
```
ParseCall => """
    _TargetName = await Task.FromResult(reader.ReadInnerXml());
    """
ClassName => "string"
```

This preserves the existing `string?` property type while enabling parsing.

**RFC Sections:** §7.10 (anyxml), §7.11 (anydata)

---

### GAP 6 & 7: `when` and `must` XPath Not Evaluated

**RFC 7950 §7.21.5 / §7.5.3:** `when` makes a node conditional; `must` defines a data constraint. Both use XPath 1.0 expressions.

**Current State:** Both are stored as C# attributes (`[When("...")]`, `[Must("...")]`). The XPath is not evaluated at compile time or runtime.

**Assessment:** This is an **acceptable design limitation**. Evaluating XPath 1.0 over a YANG data tree at runtime would require:
1. An XPath engine targeting the YANG data model
2. A DOM-like in-memory representation of the entire data tree
3. Significant runtime overhead

**Recommendation:** Document this as a known limitation. The attributes preserve the XPath expressions for tooling and documentation purposes. Future enhancement could add optional runtime validation via an XPath library.

**No code changes required** — add documentation only.

---

### GAP 8: `if-feature` Not Used for Conditional Code Generation

**RFC 7950 §7.20.2:** `if-feature` makes a schema node conditional on feature support.

**Current State:** Features are listed in `YangNode.Features = [...]`. `if-feature` generates `[IfFeature("...")]` attributes. But no mechanism exists to conditionally exclude nodes from code generation based on a project-specified feature set.

**Recommendation:** This could be implemented as an optional MSBuild property (`<YangFeatures>feature1;feature2</YangFeatures>`) that the generator reads to prune nodes. However, this is a significant design decision. For now, document as a known limitation.

**Files to Change (future):**
- `YangGenerator.cs` — Read feature configuration from `AnalyzerConfigOptions`
- `Statement.cs` / `TopLevelStatement.cs` — Prune nodes with unsupported if-feature during compilation

---

### GAP 9: Submodule Prefix/Namespace Propagation

**RFC 7950 §7.2.2:** A submodule uses `belongs-to` to declare its parent module, inheriting that module's namespace.

**Current State:** `Submodule` doesn't set `XmlNamespace`. During `IncludeSubmodules()`, children are moved to the parent module and `Usings`/`ImportedModules` are propagated. However, `PrefixToNamespaceTable` is only partially propagated (the submodule's own imports may not have their namespace mappings carried over).

**Files to Change:**
- `YangGenerator.cs` `IncludeSubmodules()` — Ensure `PrefixToNamespaceTable` entries from submodule imports are propagated to the parent module

---

### GAP 10-15: Low-Priority Gaps (Attribute-Only / Convenience)

These gaps represent features that are parsed and annotated but not enforced at runtime:

| Gap | Current | Ideal | Effort |
|-----|---------|-------|--------|
| 10. `ordered-by user` | `[OrderedBy("user")]` attribute | Document; no change needed for basic use | — |
| 11. `unique` constraint | `[Unique("...")]` attribute | Generate validation in setter/constructor | S |
| 12. `mandatory` on choice | Parsed, not enforced | Generate non-nullable property when mandatory | S |
| 13. `min/max-elements` | Attributes | Generate bounds-checking in setter | S |
| 14. `key`-based equality | `[Key("...")]` attribute | Generate `Equals`/`GetHashCode` on list entries | M |
| 15. `instance-identifier` | String wrapper | Document as limitation | — |

---

### GAP 16: YANG 1.0 Backward Compatibility

**RFC 6020** defines YANG 1.0, which RFC 7950 (1.1) supersedes. Key differences:
- 1.0: No `action`, no `anydata`, single `base` per identity, no `modifier` in pattern
- 1.0: `leaf-list` doesn't support `default`

**Current State:** `yang-version` is parsed but not checked. YANG 1.0 modules can use 1.1-only features without error.

**Recommendation:** Add validation in `StatementFactory` or `TopLevelStatement` that checks `yang-version` and rejects 1.1-only constructs when version is "1" (or absent, which implies 1.0).

---

### GAP 17: `error-app-tag` in `must` Not Propagated

**Current State:** For `pattern`, `error-app-tag` IS used in the constructor validation message. For `must`, since must is just an attribute, the error-app-tag and error-message children are silently dropped.

**Files to Change:**
- `Must.cs` — Include error-app-tag and error-message values in the `[Must("...")]` attribute string

---

### GAP 18: Multiple Module Revisions

**Current State:** `Dictionary<string, Module> modules` in `YangGenerator.cs` keys by `module.Argument` (name). Two revisions of the same module overwrite each other.

**Files to Change:**
- `YangGenerator.cs` — Key modules by name+revision, or at minimum detect conflicts and report a diagnostic
- `Import.cs` / `RevisionDate.cs` — Use revision-date in import to select the correct module

---

## Implementation Phases

### Phase 1: Critical Correctness (P0-P1) — Estimated: 2-3 days

**Goal:** Fix issues that cause incorrect code generation or parsing failures for valid YANG.

| Step | Gap | Task | Files |
|------|-----|------|-------|
| 1.1 | #2 | Add `action`/`notification` to `Grouping.PermittedChildren` | `Grouping.cs` |
| 1.2 | #1 | Add `Deviations` collection to `TopLevelStatement` | `TopLevelStatement.cs`, `ITopLevelStatement.cs` |
| 1.3 | #1 | Implement `Deviation.Apply()` with target resolution | `Deviation.cs` |
| 1.4 | #1 | Implement `Deviate.ApplyTo()` for all four modes | `Deviate.cs` |
| 1.5 | #1 | Add `ApplyDeviations()` pipeline step | `YangGenerator.cs` |
| 1.6 | #1 | Add tests for all deviation modes | `dotnetYang.Tests/ParsingTests.cs` |

**Dependencies:** Step 1.1 is independent. Steps 1.2-1.6 are sequential.

### Phase 2: Data Integrity (P1-P2) — Estimated: 1-2 days

**Goal:** Fix refine semantics and enable anyxml/anydata parsing.

| Step | Gap | Task | Files |
|------|-----|------|-------|
| 2.1 | #3 | Implement replace-vs-insert logic for `refine` | `Grouping.cs` |
| 2.2 | #3 | Add tests for refine replacing default, mandatory, etc. | `dotnetYang.Tests/ParsingTests.cs` |
| 2.3 | #4 | Add `IXMLReadValue` to `AnyXml` with `ReadInnerXml()` | `AnyXml.cs` |
| 2.4 | #5 | Add `IXMLReadValue` to `AnyData` with `ReadInnerXml()` | `AnyData.cs` |
| 2.5 | #4,5 | Add tests for anyxml/anydata round-trip | `dotnetYang.Tests/ParsingTests.cs` |

**Dependencies:** Steps 2.1-2.2 independent from 2.3-2.5.

### Phase 3: Documentation & Annotations (P3) — Estimated: 1 day

**Goal:** Improve attribute annotations and document design decisions.

| Step | Gap | Task | Files |
|------|-----|------|-------|
| 3.1 | #6,7 | Document when/must as attribute-only (design decision) | `README.md` |
| 3.2 | #17 | Include error-app-tag/error-message in `[Must]` attribute | `Must.cs` |
| 3.3 | #9 | Propagate PrefixToNamespaceTable from submodule imports | `YangGenerator.cs` |

### Phase 4: Completeness & Polish (P3) — Estimated: 3-5 days (can be done incrementally)

**Goal:** Address remaining gaps for fuller RFC compliance.

| Step | Gap | Task | Files |
|------|-----|------|-------|
| 4.1 | #12 | Generate non-nullable Choice property when mandatory=true | `Choice.cs` |
| 4.2 | #14 | Generate Equals/GetHashCode on list entries based on key | `List.cs` |
| 4.3 | #13 | Generate min/max-elements validation in list/leaf-list | `List.cs`, `LeafList.cs` |
| 4.4 | #11 | Generate unique-constraint validation | `List.cs` |
| 4.5 | #18 | Handle multiple module revisions in compilation pipeline | `YangGenerator.cs` |
| 4.6 | #16 | Add YANG 1.0 compatibility validation | `TopLevelStatement.cs`, `StatementFactory.cs` |
| 4.7 | #8 | (Future) Feature-gated code generation via MSBuild config | `YangGenerator.cs` |

---

## Design Decisions & Notes

### Deviation Ordering
Deviations MUST be applied **after** augment injection, because augments may introduce nodes that are then deviated. The pipeline order should be:
1. Include submodules
2. Expand uses/grouping
3. Inject augments
4. **Apply deviations** ← new step
5. Generate code (ToCode)

### Refine Singleton Detection
The set of singleton (replace) vs. accumulating (add) sub-statements in refine:
- **Replace:** `default`, `description`, `reference`, `config`, `mandatory`, `presence`, `min-elements`, `max-elements`
- **Add:** `must`

This is defined in RFC 7950 §7.13.2 Table.

### anyxml/anydata Type Choice
Keep `string?` as the C# type for both. Using `XElement` or `XmlDocument` would be more type-safe but would add a dependency and break backward compatibility. The raw XML string approach is consistent with the project's existing streaming-XML philosophy.

### When/Must: Why Not Evaluate?
XPath 1.0 evaluation over a YANG instance data tree requires:
- Building a full data tree DOM at runtime
- An XPath engine with YANG-specific functions (`current()`, `deref()`, etc.)
- This is essentially a YANG validation engine — out of scope for a code generator

The attribute-only approach is the right design for a code generator. Runtime validation should be a separate library concern.



