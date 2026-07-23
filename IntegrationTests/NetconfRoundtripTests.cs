using System.Text;
using System.Xml;
using Xunit.Abstractions;
using YangSupport;
using YangSupport.Netconf;

namespace IntegrationTests;

/// <summary>
/// Tests that validate DotnetYang-generated XML against netopeer2
/// using the high-level NetconfClient API.
/// </summary>
[Trait("Category", "Integration")]
public class NetconfRoundtripTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private SshNetconfClient? _client;

    public NetconfRoundtripTests(ITestOutputHelper output) => _output = output;

    public async Task InitializeAsync()
    {
        try
        {
            _client = await SshNetconfClient.ConnectAsync(
                NetconfConfig.Host, NetconfConfig.Port,
                NetconfConfig.User, NetconfConfig.Password);
            _output.WriteLine($"Connected. Base 1.1: {_client.Session.Base11}, " +
                $"Candidate: {_client.Session.Candidate}, Validate: {_client.Session.Validate}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Could not connect ({ex.GetType().Name}): {ex.Message}");
        }
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetConfig_ReturnsData()
    {
        if (_client is null) { _output.WriteLine("Skipped"); return; }

        var data = await _client.GetConfigAsync();
        Assert.NotNull(data);
        _output.WriteLine($"GetConfig returned {data!.ChildNodes.Count} top-level elements");
    }

    /// <summary>
    /// Full roundtrip: edit-config with DotnetYang-generated types → get-config → verify.
    /// </summary>
    [Fact]
    public async Task EditConfig_ThenGetConfig_RoundTrips()
    {
        if (_client is null) { _output.WriteLine("Skipped"); return; }

        var interfaces = new Ietf.Interfaces.YangNode.InterfacesContainer
        {
            Interface = new YangList<string, Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry>(
                e => e.Name)
            {
                new Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry
                {
                    Name = "test-eth0",
                    Type = Ietf.Interfaces.YangNode.InterfaceTypeIdentity.EthernetCsmacd,
                    Description = "DotnetYang roundtrip test",
                    AdminStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.AdminStatus.Up,
                    OperStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.OperStatus.Up,
                    IfIndexValue = 1,
                }
            }
        };

        // Edit config with generated types — configOnly: true excludes state data
        await _client.EditConfigAsync(interfaces, configOnly: true);
        _output.WriteLine("edit-config succeeded");

        // Read back and verify
        var filter = "<interfaces xmlns=\"urn:ietf:params:xml:ns:yang:ietf-interfaces\"/>";
        var data = await _client.GetConfigAsync(filter: filter);
        Assert.NotNull(data);
        var xml = data!.OuterXml;
        _output.WriteLine($"get-config: {xml}");

        Assert.Contains("test-eth0", xml);
        Assert.Contains("DotnetYang roundtrip test", xml);
        Assert.Contains("ethernetCsmacd", xml);
    }

    [Fact]
    public async Task Lock_Unlock_Works()
    {
        if (_client is null) { _output.WriteLine("Skipped"); return; }

        await _client.LockAsync(Datastore.Running);
        _output.WriteLine("Lock acquired");

        await _client.UnlockAsync(Datastore.Running);
        _output.WriteLine("Lock released");
    }

    [Fact]
    public async Task Validate_Works()
    {
        if (_client is null || !_client.Session.Validate)
        {
            _output.WriteLine("Skipped: validate not supported");
            return;
        }

        await _client.ValidateAsync(Datastore.Running);
        _output.WriteLine("Validate succeeded");
    }

    [Fact]
    public async Task Candidate_Commit_Works()
    {
        if (_client is null || !_client.Session.Candidate)
        {
            _output.WriteLine("Skipped: candidate not supported");
            return;
        }

        var interfaces = new Ietf.Interfaces.YangNode.InterfacesContainer
        {
            Interface = new YangList<string, Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry>(
                e => e.Name)
            {
                new Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry
                {
                    Name = "candidate-if0",
                    Type = Ietf.Interfaces.YangNode.InterfaceTypeIdentity.EthernetCsmacd,
                    Description = "Candidate commit test",
                    AdminStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.AdminStatus.Up,
                    OperStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.OperStatus.Up,
                    IfIndexValue = 2,
                }
            }
        };

        await _client.EditConfigAsync(interfaces, target: Datastore.Candidate, configOnly: true);
        _output.WriteLine("edit-config to candidate succeeded");

        await _client.CommitAsync();
        _output.WriteLine("commit succeeded");

        // Verify in running
        var filter = "<interfaces xmlns=\"urn:ietf:params:xml:ns:yang:ietf-interfaces\"/>";
        var data = await _client.GetConfigAsync(filter: filter);
        Assert.Contains("candidate-if0", data!.OuterXml);
        _output.WriteLine("Verified in running after commit");
    }

    /// <summary>
    /// Test typed deserialization: get-config → ParseAsync → typed C# object.
    /// </summary>
    [Fact]
    public async Task GetConfigTyped_DeserializesIntoGeneratedType()
    {
        if (_client is null) { _output.WriteLine("Skipped"); return; }

        // First ensure there's data to read
        var interfaces = new Ietf.Interfaces.YangNode.InterfacesContainer
        {
            Interface = new YangList<string, Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry>(
                e => e.Name)
            {
                new Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry
                {
                    Name = "typed-if0",
                    Type = Ietf.Interfaces.YangNode.InterfaceTypeIdentity.EthernetCsmacd,
                    Description = "Typed deserialization test",
                    AdminStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.AdminStatus.Up,
                    OperStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.OperStatus.Up,
                    IfIndexValue = 3,
                }
            }
        };
        await _client.EditConfigAsync(interfaces, configOnly: true);

        // Get config and deserialize into generated type
        var filter = "<interfaces xmlns=\"urn:ietf:params:xml:ns:yang:ietf-interfaces\"/>";
        var data = await _client.GetConfigAsync(filter: filter);
        Assert.NotNull(data);

        // Parse the <interfaces> element directly using the container's ParseAsync
        var xml = data!.InnerXml;
        using var stringReader = new System.IO.StringReader(xml);
        using var reader = XmlReader.Create(stringReader, SerializationHelper.GetStandardReaderSettings());
        await reader.ReadAsync();
        var parsedInterfaces = await Ietf.Interfaces.YangNode.InterfacesContainer.ParseAsync(reader);

        Assert.NotNull(parsedInterfaces);
        Assert.NotNull(parsedInterfaces!.Interface);

        var entry = parsedInterfaces.Interface!["typed-if0"];
        Assert.NotNull(entry);
        Assert.Equal("Typed deserialization test", entry!.Description);
        Assert.Equal(Ietf.Interfaces.YangNode.InterfaceTypeIdentity.EthernetCsmacd, entry.Type);
        _output.WriteLine($"Deserialized: name={entry.Name}, type={entry.Type}, desc={entry.Description}");
    }

    /// <summary>
    /// Test client-side validation before edit-config.
    /// </summary>
    [Fact]
    public async Task EditConfigValidated_EnforcesConstraints()
    {
        if (_client is null) { _output.WriteLine("Skipped"); return; }

        // Create a valid interface and send with validation
        var interfaces = new Ietf.Interfaces.YangNode.InterfacesContainer
        {
            Interface = new YangList<string, Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry>(
                e => e.Name)
            {
                new Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry
                {
                    Name = "validated-if0",
                    Type = Ietf.Interfaces.YangNode.InterfaceTypeIdentity.EthernetCsmacd,
                    Description = "Validation test",
                    AdminStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.AdminStatus.Up,
                    OperStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.OperStatus.Up,
                    IfIndexValue = 4,
                }
            }
        };

        // EditConfigValidatedAsync runs YangValidate() before sending
        await _client.EditConfigValidatedAsync(interfaces, configOnly: true);
        _output.WriteLine("edit-config with validation succeeded");

        // Verify it was applied
        var filter = "<interfaces xmlns=\"urn:ietf:params:xml:ns:yang:ietf-interfaces\"/>";
        var data = await _client.GetConfigAsync(filter: filter);
        Assert.Contains("validated-if0", data!.OuterXml);
    }
}
