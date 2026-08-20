# DotnetYang — Agent Onboarding & General Instructions

## What This Project Is

**dotnetYang** is a Roslyn source generator that compiles YANG (RFC 7950) data models into typed C# code, with a complete NETCONF client/server runtime. Users add `.yang` files to a C# project and immediately get typed data models, async RPC/notification calls, and NETCONF operations — no manual XML parsing needed.

**Published as:** NuGet package `dotnetYang` (generator + runtime).

## Quick Orientation

```
DotnetYang/
├── dotnetYang/              ← Source generator (compiles .yang → C# at build time)
│   ├── Generator/           ← IIncrementalGenerator entry point
│   ├── Parser/              ← YANG lexer + parser → YangStatement tree
│   └── SemanticModel/       ← Statement classes, code emission, XPath, builtins
├── YangSupport/             ← Runtime library (ships with NuGet)
│   ├── Netconf/             ← Client: NetconfClient, Subscriptions, Framing
│   ├── Datastore/           ← Server: DatastoreManager, YangDatastore, ServerSession
│   └── (root)              ← IChannel, IYangNode, SerializationHelper, attributes
├── IntegrationTests/        ← Docker-based tests against netopeer2/sysrepo
│   ├── docker/              ← Compose + Dockerfiles
│   ├── yang-modules/        ← Raw YANG files loaded by netopeer2 inside Docker
│   └── YangModels/          ← Minimal YANG set matching netopeer2 (AdditionalFiles for generator)
├── TestData/YangSource/     ← Full IETF/IEEE/IANA module test set
├── YangSourceTests/         ← Integration tests for generated code
├── dotnetYang.Tests/        ← Unit tests for parser/semantic model
└── benchmarks/              ← Performance benchmarks
```

## Critical Constraints

| Constraint | Reason |
|---|---|
| `dotnetYang` and `YangSupport` target **`netstandard2.0`** | They ship as NuGet packages consumed by any .NET version |
| No `ToHashSet()`, no `IAsyncEnumerable` without polyfill | `netstandard2.0` API surface. Use `new HashSet<T>(...)` and the `Microsoft.Bcl.AsyncInterfaces` package. |
| Central package versioning (`Directory.Packages.props`) | No inline `<Version>` in `.csproj` files |
| Generated code must compile without warnings | Users see the generated output in their projects |
| Use `global::` qualified namespaces in generated code | Avoids collisions with user namespaces |
| `IYangServer` is a **partial interface** | Each YANG module contributes to it; changes are breaking |
| `IYangXmlSerializable` is the runtime serialization interface | Generated classes implement it; keep stable |

## How The Generator Works

```
.yang files → Lexer → Parser → Semantic Model → ToCode() → C# source
```

1. YANG files listed as `<AdditionalFiles>` are read by `YangGenerator.cs`
2. Each file is tokenized and parsed into a `YangStatement` tree
3. `StatementFactory.Create()` maps keywords to typed C# classes
4. `CompilationUnit` links modules, expands groupings, injects augments, resolves identities
5. Each `Statement.ToCode()` emits the corresponding C# source text
6. `SourceProductionContext.AddSource()` writes the generated files

**To add a new YANG keyword:** Create a class in `SemanticModel/`, inherit `Statement`, define `Keyword` + `PermittedChildren`, implement `ToCode()`, register in `StatementFactory`.

## How The Runtime Works

### Client Side
```csharp
var client = new SshNetconfClient(...);           // Connect
var interfaces = new InterfacesContainer { ... }; // Build typed config
await client.EditConfigAsync(interfaces, configOnly: true);  // Send (state excluded)
await client.EditConfigValidatedAsync(interfaces, configOnly: true); // Same + client-side YangValidate()
var config = await client.GetConfigAsync<T>(T.ParseAsync);   // Read back typed (config only)
var oper  = await client.GetAsync<T>(T.ParseAsync);          // Read operational + config data

// NetconfSessionInfo captures negotiated capabilities after hello exchange
// Enums: Datastore { Running, Candidate, Startup }
//        DefaultOperation { Merge, Replace, None }
//        ErrorOption { StopOnError, ContinueOnError, RollbackOnError }
```

### Server Side
```csharp
var datastores = new DatastoreManager<Config>(Config.ParseAsync);
var session = new NetconfServerSession<Config>(datastores);
await session.HandleRpcAsync(input, output);  // Dispatches get/edit/lock/commit/etc.
```

## Key Generated Code Patterns

Every container/list-entry class gets:
- `WriteXMLAsync(XmlWriter writer, bool configOnly = false)` — serializes to NETCONF XML; when `configOnly: true`, skips `[NotConfigurationData]` nodes
- `ParseAsync(XmlReader reader)` — deserializes from XML into typed object
- `YangValidate()` — evaluates must/when constraints, min/max-elements, unique; throws `YangValidationException` (single failure) or `YangValidationAggregateException` (multiple failures); both carry `SchemaPath`, `Expression`, `ErrorAppTag`, `ErrorMessage`

Identity enums get:
- `GetEncodedValue(EnumType)` → YANG identity string
- `GetIdentityNamespace(EnumType)` → XML namespace for cross-module identities
- `GetXxxValue(string)` → parses (strips namespace prefix before matching)

## Testing Strategy

| Layer | Location | What | Requires |
|---|---|---|---|
| Unit | `dotnetYang.Tests/` | Parser, semantic model, ToCode() | Nothing |
| Integration | `YangSourceTests/` | Full generated code, RPC round-trips | Build |
| NETCONF interop | `IntegrationTests/` | Live netopeer2: edit/get/lock/commit/subscribe | Docker |

Run NETCONF tests: `cd IntegrationTests && ./run-integration-tests.sh`

## Standards This Project Implements

| RFC | Area | Key Sections |
|---|---|---|
| **7950** | YANG 1.1 language | All of it — data nodes, types, grouping, augment, identity, must/when |
| **6241** | NETCONF protocol | Operations: get-config, edit-config, copy-config, lock, validate, commit |
| **6242** | NETCONF over SSH | Base:1.0 EOM framing (`]]>]]>`), Base:1.1 chunked framing |
| **5277** | Notifications | create-subscription, notification XML structure, eventTime |

## Common Pitfalls

1. **Identity serialization** — When an identityref value is from a different module than the leaf, the XML value MUST be namespace-prefixed: `<type xmlns:idr="...">idr:ethernetCsmacd</type>`

2. **Boolean casing** — YANG/XML requires `"true"/"false"`, not C#'s `"True"/"False"`. Use `value == true ? "true" : "false"` (works for both `bool` and `bool?`).

3. **Config vs state** — `config false` nodes get `[NotConfigurationData]` attribute. `WriteXMLAsync(configOnly: true)` wraps them in `if(!configOnly)` guards. State leaves can still be `required` in C# (mandatory in YANG) — users set them but they're excluded from edit-config.

4. **IYangServerExtensions** — Only generated when at least one module has RPCs/actions/notifications. Projects with no RPCs won't generate it.

5. **HashSet in netstandard2.0** — `ToHashSet()` doesn't exist. Use `new HashSet<T>(enumerable)`.

6. **Diamond identity inheritance** — `GetInheritanceList()` can yield duplicates. Use `Dictionary<argName, value>` for namespace mapping to avoid duplicate switch cases.

7. **XmlWriter ConformanceLevel** — When writing XML fragments (not full documents), use `ConformanceLevel.Fragment` in settings.

8. **EditConfigValidatedAsync uses reflection** — It calls `node.GetType().GetMethod("YangValidate")` then `Invoke()`. If you rename the generated `YangValidate()` method, this client-side validation will silently stop working.

9. **NetconfSubscription** — RFC 5277 notification subscriptions live in `YangSupport/Netconf/NetconfSubscription.cs`. Use `CreateSubscriptionAsync(stream, filter, startTime, stopTime)` then `ReadNotificationsAsync<T>(parseFunc)` to get a typed `IAsyncEnumerable<T>` stream. The subscription must be disposed to release the underlying channel.

## What's Implemented vs. What's Not

### ✅ Done
- Full YANG 1.1 compilation (all statement types)
- NETCONF client: GetConfig, EditConfig, Lock, Unlock, Validate, Commit, CopyConfig, DeleteConfig, KillSession
- Typed deserialization (`GetConfigAsync<T>(parseFunc)`)
- Config-only serialization (`configOnly: true`)
- RFC 5277 notifications with typed `IAsyncEnumerable<T>` parsing
- Base:1.0 + 1.1 framing
- Server datastores (running/candidate/startup) with full lifecycle
- NETCONF server session handler (all standard RPCs)
- Identity namespace prefixing + prefix-stripping on parse
- must/when/cardinality runtime validation
- Integration tests against netopeer2 (12 tests, all passing)

### 🔮 Not Yet Implemented
- RFC 8639/8641 dynamic subscriptions (subscription-id multiplexing)
- RFC 8342 NMDA (`<get-data>`, `<edit-data>`, origin)
- NETCONF call-home (RFC 8071)
- YANG schema mount (RFC 8528)
- JSON encoding (RFC 7951)
- Full RESTCONF server (RFC 8040)
