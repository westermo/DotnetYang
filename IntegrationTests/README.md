# Integration Testing with netopeer2/sysrepo

This directory contains integration tests that validate DotnetYang's generated
C# code against the [netopeer2](https://github.com/CESNET/netopeer2) /
[sysrepo](https://github.com/sysrepo/sysrepo) NETCONF stack running in Docker.

## Architecture

```
┌─────────────────────────────┐     NETCONF/SSH     ┌─────────────────────────┐
│  .NET Integration Tests     │────── :830 ────────▶│  netopeer2 + sysrepo    │
│                             │                      │  (Docker container)     │
│  - XmlInteropTests          │                      │                         │
│  - NetconfRoundtripTests    │                      │  YANG modules loaded    │
│                             │                      │  from yang-modules/     │
│  Uses DotnetYang-generated  │                      │                         │
│  types + NetconfSshChannel  │                      │  Reference NETCONF      │
│  (SSH.NET NetConfClient)    │                      │  implementation         │
└─────────────────────────────┘                      └─────────────────────────┘
```

## What Gets Tested

The integration tests exercise the full DotnetYang pipeline end-to-end:

1. **YANG compilation** — `YangModels/` contains `ietf-interfaces`, `iana-if-type`,
   and dependencies. DotnetYang's source generator compiles these into C# types.
2. **XML serialization** — Generated types are serialized using `WriteXMLAsync` and
   `WriteConfigXMLAsync`, validating correct namespaces, identity prefixes, and
   boolean casing.
3. **NETCONF round-trip** — Serialized config is sent via `edit-config` to netopeer2
   and read back via `get-config`, verifying the data survives a full write/read cycle
   through a reference NETCONF implementation.
4. **Config-only serialization** — `WriteXMLAsync(writer, configOnly: true)` correctly
   excludes `config false` state data (e.g., `admin-status`, `oper-status`, `if-index`),
   producing XML suitable for `edit-config` operations.

### Interop issues caught by these tests

- **Identityref namespace prefixing** — identities from other modules must be
  serialized with an `xmlns` prefix (e.g., `<type xmlns:idr="...">idr:ethernetCsmacd</type>`).
- **Boolean casing** — YANG/XML requires lowercase `true`/`false`, not C#'s `True`/`False`.
- **State vs config data** — `edit-config` must not include `config false` leaves.

## Test Categories

### 1. XML Interop Tests (no Docker required)
Validate that DotnetYang's XML serialization produces correct namespaces,
structure, identity prefixes, boolean casing, and round-trips correctly.

### 2. NETCONF Roundtrip Tests (requires Docker)
Connect to netopeer2 over SSH and perform real NETCONF operations:
- `get-config` returns valid XML
- `edit-config` with DotnetYang-serialized config → `get-config` → verify round-trip

## Quick Start

### Run all tests in Docker (CI-friendly)
```bash
./run-integration-tests.sh
```

### Run netopeer2 locally for development
```bash
# Start netopeer2 in background
./run-integration-tests.sh --up-only

# Run tests locally (faster iteration)
dotnet test IntegrationTests/ --filter "Category=Integration"

# Tear down when done
./run-integration-tests.sh --down
```

### Run only the offline XML tests (no Docker needed)
```bash
dotnet test IntegrationTests/ --filter "FullyQualifiedName~XmlInteropTests"
```

## Adding New Tests

### Testing a new YANG module against netopeer2

1. Add the `.yang` file to both `YangModels/yang/` (for C# generation)
   and `yang-modules/` (for netopeer2 installation)
2. Write a test that:
   - Creates instances using DotnetYang-generated types
   - Serializes with `WriteXMLAsync(writer, configOnly: true)`
   - Sends via `edit-config` to netopeer2
   - Reads back with `get-config`
   - Verifies the round-trip result

### Testing against additional NETCONF servers

The `NetconfSshChannel` class (backed by SSH.NET's `NetConfClient`) can connect
to any NETCONF-over-SSH server. To test against other implementations:

1. Add a new service to `docker/docker-compose.yml`
2. Set different env vars (e.g., `NETCONF2_HOST`)
3. Write tests that connect to both servers and compare behavior

## Files

| File | Description |
|------|-------------|
| `YangModels/` | Minimal YANG module set for integration tests (compiled by DotnetYang) |
| `yang-modules/` | Same YANG files installed into the netopeer2 container |
| `docker/docker-compose.yml` | Docker Compose with netopeer2 + test runner |
| `docker/Dockerfile.netopeer2` | netopeer2/sysrepo container with YANG modules and NACM config |
| `docker/Dockerfile.tests` | .NET SDK container for running tests |
| `NetconfSshChannel.cs` | `IChannel` implementation using SSH.NET's `NetConfClient` |
| `NetconfConfig.cs` | Environment-based connection config |
| `NetconfRoundtripTests.cs` | Live NETCONF round-trip tests (edit-config → get-config) |
| `XmlInteropTests.cs` | Offline XML validation (namespaces, identities, booleans, config filtering) |
| `run-integration-tests.sh` | All-in-one test runner script |
