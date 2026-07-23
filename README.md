[![Nuget (Generator)](https://img.shields.io/nuget/v/dotnetYang?style=flat-square)](https://www.nuget.org/packages/dotnetYang/)
[![Build](https://img.shields.io/github/actions/workflow/status/westermo/dotnetYang/build.yml?branch=main&style=flat-square)](https://github.com/westermo/dotnetYang/actions)
[![License](https://img.shields.io/github/license/westermo/dotnetYang?style=flat-square)](https://github.com/westermo/dotnetYang/blob/develop/LICENSE)

dotnetYang is a [Roslyn](https://github.com/dotnet/roslyn) source generator that compiles YANG models into C# code, providing typed data models, NETCONF client/server operations, and async RPC/Action/Notification support — all without manually parsing XML.

## Features

- **Drop-and-go:** Add `.yang` files to a C# project as additional files — RPCs, containers, lists, and notifications become typed C# classes immediately.
- **NETCONF Client:** High-level `NetconfClient` with typed `GetConfig`, `EditConfig`, `Lock`, `Validate`, `Commit`, and notification subscriptions.
- **NETCONF Server:** `DatastoreManager` with running/candidate/startup datastores, locking, copy-config, and `NetconfServerSession` for handling incoming RPCs.
- **Config-only serialization:** `WriteXMLAsync(writer, configOnly: true)` filters out `config false` state data for `edit-config` operations.
- **Interop-correct XML:** Namespace-prefixed identity values, lowercase booleans, proper NETCONF framing (base:1.0 and 1.1).
- **Server interface:** Generated `IYangServer` with extension method `Receive(input, output)` for implementing YANG-based servers.

## Getting Started

Add the NuGet package:
```bash
dotnet add package dotnetYang
```

Add a `.yang` file to your project:
```xml
<ItemGroup>
    <AdditionalFiles Include="ietf-interfaces@2018-02-20.yang" />
    <AdditionalFiles Include="iana-if-type@2023-01-26.yang" />
</ItemGroup>
```

The generated types are immediately available in your code.

---

## NETCONF Client Usage

### Connecting to a device

```csharp
using YangSupport.Netconf;
using Renci.SshNet;

// Connect via SSH.NET
var ssh = new NetConfClient("192.168.1.1", 830, "admin", "password");
ssh.Connect();

// Or use the stream-based client with base:1.1 chunked framing
var client = await NetconfClient.ConnectAsync(inputStream, outputStream);
Console.WriteLine($"Session {client.Session.SessionId}, Base 1.1: {client.Session.Base11}");
```

### Reading configuration (typed)

```csharp
// Get raw XML
var data = await client.GetConfigAsync(filter: "<interfaces xmlns=\"urn:ietf:params:xml:ns:yang:ietf-interfaces\"/>");

// Or deserialize directly into generated types
var config = await client.GetConfigAsync(
    Ietf.Interfaces.YangNode.InterfacesContainer.ParseAsync,
    filter: "<interfaces xmlns=\"urn:ietf:params:xml:ns:yang:ietf-interfaces\"/>");

// Access typed properties
foreach (var iface in config.Interface!)
{
    Console.WriteLine($"{iface.Name}: {iface.Type} - {iface.Description}");
}
```

### Writing configuration

```csharp
// Build config using generated types
var interfaces = new Ietf.Interfaces.YangNode.InterfacesContainer
{
    Interface = new YangList<string, Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry>(
        e => e.Name)
    {
        new()
        {
            Name = "eth0",
            Type = Ietf.Interfaces.YangNode.InterfaceTypeIdentity.EthernetCsmacd,
            Description = "Management interface",
        }
    }
};

// Send to device — configOnly: true automatically excludes state data
await client.EditConfigAsync(interfaces, configOnly: true);
```

### Candidate datastore workflow

```csharp
await client.LockAsync(Datastore.Candidate);

await client.EditConfigAsync(interfaces, target: Datastore.Candidate, configOnly: true);
await client.ValidateAsync(Datastore.Candidate);
await client.CommitAsync();

await client.UnlockAsync(Datastore.Candidate);
```

### With client-side YANG validation

```csharp
// Validates must/when constraints before sending to the server
await client.EditConfigValidatedAsync(interfaces, configOnly: true);
```

### Subscribing to notifications (RFC 5277)

```csharp
// Stream-based subscription with typed parsing
var sub = await NetconfSubscription.CreateAsync(inputStream, outputStream);
await sub.CreateSubscriptionAsync(stream: "NETCONF");

await foreach (var (eventTime, notification) in
    sub.ReadNotificationsAsync(MyModule.YangNode.MyNotification.ParseAsync))
{
    Console.WriteLine($"[{eventTime}] {notification.SomeField}");
}
```

### Calling RPCs

```csharp
IChannel channel = // your channel implementation
int messageId = 1;

var input = new Some.Module.YangNode.DoSomethingInput { TheBigLeaf = 123 };
var output = await Some.Module.YangNode.DoSomething(channel, messageId, input);
Console.WriteLine(output.Response);
```

---

## NETCONF Server Usage

### Setting up datastores

```csharp
using YangSupport.Datastore;
using YangSupport.Netconf;

// Create the datastore manager with your generated Configuration type
var datastores = new DatastoreManager<MyNamespace.Configuration>(
    MyNamespace.Configuration.ParseAsync);

// Load saved startup configuration
await datastores.LoadFromFileAsync("/etc/netconf/startup.xml", copyToRunning: true);
```

### Handling NETCONF sessions

```csharp
// For each incoming NETCONF connection:
var session = new NetconfServerSession<MyNamespace.Configuration>(datastores);

// Send hello
var hello = session.GetHello();
// ... send hello over transport ...

// Process RPCs in a loop
bool keepAlive = true;
while (keepAlive)
{
    // Read framed message from transport into inputStream
    keepAlive = await session.HandleRpcAsync(inputStream, outputStream);
    // Send outputStream content back over transport
}
```

The server automatically handles: `get-config`, `edit-config`, `copy-config`, `delete-config`, `lock`, `unlock`, `validate`, `commit`, `discard-changes`, `close-session`, `kill-session`.

### Programmatic datastore access

```csharp
// Modify running config programmatically
datastores.Running.Modify(cfg =>
{
    cfg.IetfInterfaces!.Interfaces!.Interface!.Add(new() { Name = "lo0", ... });
});

// Copy running → startup (persist)
await datastores.CopyConfigAsync(Datastore.Running, Datastore.Startup);
await datastores.SaveToFileAsync("/etc/netconf/startup.xml");

// Validate before commit
datastores.Validate(Datastore.Candidate); // throws YangValidationException on failure
await datastores.CommitAsync(); // candidate → running
```

### Implementing module-specific RPCs

```csharp
public class MyServer : IYangServer
{
    public async Task<YangNode.DoSomethingOutput> OnDoSomething(YangNode.DoSomethingInput input)
    {
        // Your business logic here
        return new YangNode.DoSomethingOutput { Response = SomeIdentity.SomeValue };
    }
}
```

For large projects with many YANG modules, split `IYangServer` into partial classes for readability.

---

## Project Structure

```
dotnetYang/          — Roslyn source generator (compiles .yang → C#)
YangSupport/         — Runtime library (ships with NuGet package)
├── Netconf/         — NETCONF client: NetconfClient, NetconfSubscription, framing
├── Datastore/       — Server runtime: DatastoreManager, YangDatastore, NetconfServerSession
├── IChannel.cs      — Transport abstraction for RPCs
├── SerializationHelper.cs — XML read/write utilities
└── YangList.cs      — Typed YANG list collection
IntegrationTests/    — End-to-end tests against netopeer2/sysrepo in Docker
```

## Config-Only Serialization

YANG distinguishes `config true` (writable) from `config false` (read-only state) data. When sending `edit-config`, you must exclude state data:

```csharp
// Serializes everything (config + state)
await node.WriteXMLAsync(writer);

// Serializes only config data (for edit-config operations)
await node.WriteXMLAsync(writer, configOnly: true);
```

Properties marked `[NotConfigurationData]` are automatically excluded when `configOnly: true`.

## Integration Testing

See [IntegrationTests/README.md](IntegrationTests/README.md) for running tests against netopeer2/sysrepo in Docker. The tests validate the full pipeline: YANG compilation → C# types → XML serialization → NETCONF round-trip → typed deserialization.

```bash
cd IntegrationTests && ./run-integration-tests.sh
```

## Documentation

- [Integration Testing Guide](IntegrationTests/README.md)
- [YANG RFC 7950](https://tools.ietf.org/html/rfc7950)
- [NETCONF RFC 6241](https://tools.ietf.org/html/rfc6241)
- [NETCONF over SSH RFC 6242](https://tools.ietf.org/html/rfc6242)
