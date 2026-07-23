using System.Text;
using System.Xml;
using YangSupport;
using YangSupport.Netconf;

namespace IntegrationTests;

/// <summary>
/// Tests that validate DotnetYang-generated XML against netopeer2
/// using the high-level NetconfClient API.
/// </summary>
[Category("Integration")]
public class NetconfRoundtripTests
{
    private SshNetconfClient? _client;

    [Before(Test)]
    public async Task InitializeAsync()
    {
        try
        {
            _client = await SshNetconfClient.ConnectAsync(
                NetconfConfig.Host, NetconfConfig.Port,
                NetconfConfig.User, NetconfConfig.Password);
            Console.WriteLine($"Connected. Base 1.1: {_client.Session.Base11}, " +
                $"Candidate: {_client.Session.Candidate}, Validate: {_client.Session.Validate}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not connect ({ex.GetType().Name}): {ex.Message}");
        }
    }

    [After(Test)]
    public void CleanUp()
    {
        _client?.Dispose();
    }

    [Test]
    public async Task GetConfig_ReturnsData()
    {
        if (_client is null) { Console.WriteLine("Skipped"); return; }

        var data = await _client.GetConfigAsync();
        await Assert.That(data).IsNotNull();
        Console.WriteLine($"GetConfig returned {data!.ChildNodes.Count} top-level elements");
    }

    /// <summary>
    /// Full roundtrip: edit-config with DotnetYang-generated types → get-config → verify.
    /// </summary>
    [Test]
    public async Task EditConfig_ThenGetConfig_RoundTrips()
    {
        if (_client is null) { Console.WriteLine("Skipped"); return; }

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
        Console.WriteLine("edit-config succeeded");

        // Read back and verify
        var filter = "<interfaces xmlns=\"urn:ietf:params:xml:ns:yang:ietf-interfaces\"/>";
        var data = await _client.GetConfigAsync(filter: filter);
        await Assert.That(data).IsNotNull();
        var xml = data!.OuterXml;
        Console.WriteLine($"get-config: {xml}");

        await Assert.That(xml).Contains("test-eth0");
        await Assert.That(xml).Contains("DotnetYang roundtrip test");
        await Assert.That(xml).Contains("ethernetCsmacd");
    }

    [Test]
    public async Task Lock_Unlock_Works()
    {
        if (_client is null) { Console.WriteLine("Skipped"); return; }

        await _client.LockAsync(Datastore.Running);
        Console.WriteLine("Lock acquired");

        await _client.UnlockAsync(Datastore.Running);
        Console.WriteLine("Lock released");
    }

    [Test]
    public async Task Validate_Works()
    {
        if (_client is null || !_client.Session.Validate)
        {
            Console.WriteLine("Skipped: validate not supported");
            return;
        }

        await _client.ValidateAsync(Datastore.Running);
        Console.WriteLine("Validate succeeded");
    }

    [Test]
    public async Task Candidate_Commit_Works()
    {
        if (_client is null || !_client.Session.Candidate)
        {
            Console.WriteLine("Skipped: candidate not supported");
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
        Console.WriteLine("edit-config to candidate succeeded");

        await _client.CommitAsync();
        Console.WriteLine("commit succeeded");

        // Verify in running
        var filter = "<interfaces xmlns=\"urn:ietf:params:xml:ns:yang:ietf-interfaces\"/>";
        var data = await _client.GetConfigAsync(filter: filter);
        await Assert.That(data!.OuterXml).Contains("candidate-if0");
        Console.WriteLine("Verified in running after commit");
    }

    /// <summary>
    /// Test typed deserialization: get-config → ParseAsync → typed C# object.
    /// </summary>
    [Test]
    public async Task GetConfigTyped_DeserializesIntoGeneratedType()
    {
        if (_client is null) { Console.WriteLine("Skipped"); return; }

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
        await Assert.That(data).IsNotNull();

        // Parse the <interfaces> element directly using the container's ParseAsync
        var xml = data!.InnerXml;
        using var stringReader = new System.IO.StringReader(xml);
        using var reader = XmlReader.Create(stringReader, SerializationHelper.GetStandardReaderSettings());
        await reader.ReadAsync();
        var parsedInterfaces = await Ietf.Interfaces.YangNode.InterfacesContainer.ParseAsync(reader);

        await Assert.That(parsedInterfaces).IsNotNull();
        await Assert.That(parsedInterfaces!.Interface).IsNotNull();

        var entry = parsedInterfaces.Interface!["typed-if0"];
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.Description).IsEqualTo("Typed deserialization test");
        await Assert.That(entry.Type).IsEqualTo(Ietf.Interfaces.YangNode.InterfaceTypeIdentity.EthernetCsmacd);
        Console.WriteLine($"Deserialized: name={entry.Name}, type={entry.Type}, desc={entry.Description}");
    }

    /// <summary>
    /// Test client-side validation before edit-config.
    /// </summary>
    [Test]
    public async Task EditConfigValidated_EnforcesConstraints()
    {
        if (_client is null) { Console.WriteLine("Skipped"); return; }

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
        Console.WriteLine("edit-config with validation succeeded");

        // Verify it was applied
        var filter = "<interfaces xmlns=\"urn:ietf:params:xml:ns:yang:ietf-interfaces\"/>";
        var data = await _client.GetConfigAsync(filter: filter);
        await Assert.That(data!.OuterXml).Contains("validated-if0");
    }
}
