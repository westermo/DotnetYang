using System.Text;
using System.Xml;
using Xunit.Abstractions;
using YangSupport;

namespace IntegrationTests;

/// <summary>
/// Tests that validate DotnetYang-generated XML serialization against netopeer2.
/// These tests perform real NETCONF operations over SSH against a live server.
/// </summary>
[Trait("Category", "Integration")]
public class NetconfRoundtripTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private NetconfSshChannel? _channel;

    public NetconfRoundtripTests(ITestOutputHelper output) => _output = output;

    public async Task InitializeAsync()
    {
        try
        {
            _channel = await NetconfSshChannel.ConnectAsync(
                NetconfConfig.Host, NetconfConfig.Port,
                NetconfConfig.User, NetconfConfig.Password);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Could not connect to NETCONF server ({ex.GetType().Name}): {ex.Message}");
            _output.WriteLine("Ensure docker-compose is running: docker compose -f IntegrationTests/docker/docker-compose.yml up -d netopeer2");
        }
    }

    public async Task DisposeAsync()
    {
        if (_channel != null)
            await _channel.DisposeAsync();
    }

    [Fact]
    public async Task GetConfig_ReturnsValidXml()
    {
        if (_channel is null)
        {
            _output.WriteLine("Skipped: NETCONF server not available");
            return;
        }

        await using var writer = XmlWriter.Create(_channel!.WriteStream,
            SerializationHelper.GetStandardWriterSettings());
        await writer.WriteStartElementAsync(null, "rpc", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await writer.WriteAttributeStringAsync(null, "message-id", null, "1");
        await writer.WriteStartElementAsync(null, "get-config", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await writer.WriteStartElementAsync(null, "source", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await writer.WriteElementStringAsync(null, "running", "urn:ietf:params:xml:ns:netconf:base:1.0", null);
        await writer.WriteEndElementAsync(); // source
        await writer.WriteEndElementAsync(); // get-config
        await writer.WriteEndElementAsync(); // rpc
        await writer.FlushAsync();

        await _channel.Send();

        _channel.ReadStream.Position = 0;
        using var reader = new StreamReader(_channel.ReadStream, Encoding.UTF8, leaveOpen: true);
        var response = await reader.ReadToEndAsync();
        _output.WriteLine("Response:");
        _output.WriteLine(response);

        Assert.Contains("rpc-reply", response);
        Assert.DoesNotContain("rpc-error", response);
    }

    /// <summary>
    /// Full reconfiguration roundtrip using DotnetYang-generated types:
    /// 1. Serialize config using WriteConfigXMLAsync
    /// 2. Send edit-config to netopeer2
    /// 3. Read back with get-config
    /// 4. Verify the data matches what was sent
    /// </summary>
    [Fact]
    public async Task EditConfig_ThenGetConfig_RoundTrips()
    {
        if (_channel is null)
        {
            _output.WriteLine("Skipped: NETCONF server not available");
            return;
        }

        // Build config using DotnetYang-generated types
        var node = new Ietf.Interfaces.YangNode
        {
            Interfaces = new Ietf.Interfaces.YangNode.InterfacesContainer
            {
                Interface = new YangList<string, Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry>(
                    e => e.Name)
                {
                    new Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry
                    {
                        Name = "test-eth0",
                        Type = Ietf.Interfaces.YangNode.InterfaceTypeIdentity.EthernetCsmacd,
                        Description = "DotnetYang integration test",
                        // State fields are required by the type but excluded by WriteConfigXMLAsync
                        AdminStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.AdminStatus.Up,
                        OperStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.OperStatus.Up,
                        IfIndexValue = 1,
                    }
                }
            }
        };

        // Serialize using WriteConfigXMLAsync (config-only, no state data)
        var configXmlBuilder = new StringBuilder();
        await using (var configWriter = XmlWriter.Create(configXmlBuilder, SerializationHelper.GetStandardWriterSettings()))
        {
            await node.Interfaces!.WriteConfigXMLAsync(configWriter);
            await configWriter.FlushAsync();
        }
        var configXml = configXmlBuilder.ToString();
        _output.WriteLine("Config XML (WriteConfigXMLAsync):");
        _output.WriteLine(configXml);

        // Send edit-config with DotnetYang-serialized config
        await using (var writer = XmlWriter.Create(_channel!.WriteStream,
            SerializationHelper.GetStandardWriterSettings()))
        {
            await writer.WriteStartElementAsync(null, "rpc", "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteAttributeStringAsync(null, "message-id", null, "2");
            await writer.WriteStartElementAsync(null, "edit-config", "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteStartElementAsync(null, "target", "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteElementStringAsync(null, "running", "urn:ietf:params:xml:ns:netconf:base:1.0", null);
            await writer.WriteEndElementAsync(); // target
            await writer.WriteStartElementAsync(null, "config", "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteRawAsync(configXml);
            await writer.WriteEndElementAsync(); // config
            await writer.WriteEndElementAsync(); // edit-config
            await writer.WriteEndElementAsync(); // rpc
            await writer.FlushAsync();
        }

        await _channel.Send();

        _channel.ReadStream.Position = 0;
        using (var reader = new StreamReader(_channel.ReadStream, Encoding.UTF8, leaveOpen: true))
        {
            var editResponse = await reader.ReadToEndAsync();
            _output.WriteLine("Edit-config response:");
            _output.WriteLine(editResponse);
            Assert.DoesNotContain("rpc-error", editResponse);
        }

        // Read back with get-config and verify data round-trips
        await using (var writer = XmlWriter.Create(_channel.WriteStream,
            SerializationHelper.GetStandardWriterSettings()))
        {
            await writer.WriteStartElementAsync(null, "rpc", "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteAttributeStringAsync(null, "message-id", null, "3");
            await writer.WriteStartElementAsync(null, "get-config", "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteStartElementAsync(null, "source", "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteElementStringAsync(null, "running", "urn:ietf:params:xml:ns:netconf:base:1.0", null);
            await writer.WriteEndElementAsync(); // source
            await writer.WriteStartElementAsync(null, "filter", "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteAttributeStringAsync(null, "type", null, "subtree");
            await writer.WriteStartElementAsync(null, "interfaces", "urn:ietf:params:xml:ns:yang:ietf-interfaces");
            await writer.WriteEndElementAsync();
            await writer.WriteEndElementAsync(); // filter
            await writer.WriteEndElementAsync(); // get-config
            await writer.WriteEndElementAsync(); // rpc
            await writer.FlushAsync();
        }

        await _channel.Send();

        _channel.ReadStream.Position = 0;
        using (var reader = new StreamReader(_channel.ReadStream, Encoding.UTF8, leaveOpen: true))
        {
            var getResponse = await reader.ReadToEndAsync();
            _output.WriteLine("Get-config response:");
            _output.WriteLine(getResponse);
            Assert.Contains("test-eth0", getResponse);
            Assert.Contains("DotnetYang integration test", getResponse);
        }
    }
}
