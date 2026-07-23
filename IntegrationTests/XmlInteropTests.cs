using System.Text;
using System.Xml;
using Xunit.Abstractions;
using YangSupport;

namespace IntegrationTests;

/// <summary>
/// Tests that validate the XML output of DotnetYang-generated serializers
/// against well-known XML structures. These tests don't require a running
/// NETCONF server - they verify that DotnetYang produces correct XML
/// that other implementations would accept.
/// </summary>
[Trait("Category", "Integration")]
public class XmlInteropTests
{
    private readonly ITestOutputHelper _output;

    public XmlInteropTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task IetfInterfaces_ProducesCorrectNamespaces()
    {
        var node = CreateTestNode("eth0", "Test interface");
        var xml = await SerializeToString(node);
        _output.WriteLine(xml);

        Assert.Contains("urn:ietf:params:xml:ns:yang:ietf-interfaces", xml);

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("if", "urn:ietf:params:xml:ns:yang:ietf-interfaces");
        var nameNode = doc.SelectSingleNode("//if:interface/if:name", nsMgr);
        Assert.NotNull(nameNode);
        Assert.Equal("eth0", nameNode!.InnerText);
    }

    [Fact]
    public async Task IetfInterfaces_SerializationRoundTrip()
    {
        var original = new Ietf.Interfaces.YangNode
        {
            Interfaces = new Ietf.Interfaces.YangNode.InterfacesContainer
            {
                Interface = new YangList<string,
                    Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry>(e => e.Name)
                {
                    CreateTestEntry("eth0", "First interface"),
                    CreateTestEntry("eth1", "Second interface")
                }
            }
        };

        var firstXml = await SerializeToString(original);
        _output.WriteLine("First serialization:");
        _output.WriteLine(firstXml);

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(firstXml));
        using var reader = XmlReader.Create(ms, SerializationHelper.GetStandardReaderSettings());
        await reader.ReadAsync();
        var deserialized = await Ietf.Interfaces.YangNode.ParseAsync(reader);

        var secondXml = await SerializeToString(deserialized);
        _output.WriteLine("Second serialization:");
        _output.WriteLine(secondXml);

        Assert.Equal(firstXml, secondXml);
    }

    [Fact]
    public async Task ConfigPayload_HasCorrectIdentityAndBooleanSerialization()
    {
        var node = CreateTestNode("mgmt0", "Management interface");
        var xml = await SerializeToString(node);
        _output.WriteLine(xml);

        // Identity from iana-if-type must be namespace-prefixed
        Assert.Contains("urn:ietf:params:xml:ns:yang:iana-if-type", xml);
        Assert.Contains("ethernetCsmacd", xml);

        // Booleans must be lowercase per YANG/XML spec
        Assert.DoesNotContain(">True<", xml);
        Assert.DoesNotContain(">False<", xml);
    }

    [Fact]
    public async Task WriteConfigXMLAsync_ExcludesStateData()
    {
        var node = CreateTestNode("test0", "Config filter test");
        var sb = new StringBuilder();
        await using var writer = XmlWriter.Create(sb, SerializationHelper.GetStandardWriterSettings());
        await node.WriteConfigXMLAsync(writer);
        await writer.FlushAsync();

        var xml = sb.ToString();
        _output.WriteLine(xml);

        // Config leaves should be present
        Assert.Contains("test0", xml);
        Assert.Contains("type", xml);

        // State-only leaves (oper-status, if-index, etc.) should be excluded
        Assert.DoesNotContain("oper-status", xml);
        Assert.DoesNotContain("if-index", xml);
    }

    private static Ietf.Interfaces.YangNode CreateTestNode(string name, string description)
    {
        return new Ietf.Interfaces.YangNode
        {
            Interfaces = new Ietf.Interfaces.YangNode.InterfacesContainer
            {
                Interface = new YangList<string,
                    Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry>(e => e.Name)
                {
                    CreateTestEntry(name, description)
                }
            }
        };
    }

    private static Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry CreateTestEntry(
        string name, string description)
    {
        return new Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry
        {
            Name = name,
            Type = Ietf.Interfaces.YangNode.InterfaceTypeIdentity.EthernetCsmacd,
            Description = description,
            // State fields are required by the generated type but will be excluded by WriteConfigXMLAsync
            AdminStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.AdminStatus.Up,
            OperStatusValue = Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.OperStatus.Up,
            IfIndexValue = 1,
        };
    }

    private static async Task<string> SerializeToString(Ietf.Interfaces.YangNode node)
    {
        var sb = new StringBuilder();
        await using var writer = XmlWriter.Create(sb, SerializationHelper.GetStandardWriterSettings());
        await node.WriteXMLAsync(writer);
        await writer.FlushAsync();
        return sb.ToString();
    }
}
