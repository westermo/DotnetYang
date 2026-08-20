using System.Text;
using System.Xml;
using YangSupport;

namespace IntegrationTests;

/// <summary>
/// Tests that validate the XML output of DotnetYang-generated serializers
/// against well-known XML structures. These tests don't require a running
/// NETCONF server - they verify that DotnetYang produces correct XML
/// that other implementations would accept.
/// </summary>
[Category("Integration")]
public class XmlInteropTests
{
    [Test]
    public async Task IetfInterfaces_ProducesCorrectNamespaces()
    {
        var node = CreateTestNode("eth0", "Test interface");
        var xml = await SerializeToString(node);
        Console.WriteLine(xml);

        await Assert.That(xml).Contains("urn:ietf:params:xml:ns:yang:ietf-interfaces");

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("if", "urn:ietf:params:xml:ns:yang:ietf-interfaces");
        var nameNode = doc.SelectSingleNode("//if:interface/if:name", nsMgr);
        await Assert.That(nameNode).IsNotNull();
        await Assert.That(nameNode!.InnerText).IsEqualTo("eth0");
    }

    [Test]
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
        Console.WriteLine("First serialization:");
        Console.WriteLine(firstXml);

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(firstXml));
        using var reader = XmlReader.Create(ms, SerializationHelper.GetStandardReaderSettings());
        await reader.ReadAsync();
        var deserialized = await Ietf.Interfaces.YangNode.ParseAsync(reader);

        var secondXml = await SerializeToString(deserialized);
        Console.WriteLine("Second serialization:");
        Console.WriteLine(secondXml);

        await Assert.That(secondXml).IsEqualTo(firstXml);
    }

    [Test]
    public async Task ConfigPayload_HasCorrectIdentityAndBooleanSerialization()
    {
        var node = CreateTestNode("mgmt0", "Management interface");
        var xml = await SerializeToString(node);
        Console.WriteLine(xml);

        // Identity from iana-if-type must be namespace-prefixed
        await Assert.That(xml).Contains("urn:ietf:params:xml:ns:yang:iana-if-type");
        await Assert.That(xml).Contains("ethernetCsmacd");

        // Booleans must be lowercase per YANG/XML spec
        await Assert.That(xml).DoesNotContain(">True<");
        await Assert.That(xml).DoesNotContain(">False<");
    }

    [Test]
    public async Task WriteXMLAsync_ConfigOnly_ExcludesStateData()
    {
        var node = CreateTestNode("test0", "Config filter test");
        var sb = new StringBuilder();
        await using var writer = XmlWriter.Create(sb, SerializationHelper.GetStandardWriterSettings());
        await node.WriteXMLAsync(writer, configOnly: true);
        await writer.FlushAsync();

        var xml = sb.ToString();
        Console.WriteLine(xml);

        // Config leaves should be present
        await Assert.That(xml).Contains("test0");
        await Assert.That(xml).Contains("type");

        // State-only leaves (oper-status, if-index, etc.) should be excluded
        await Assert.That(xml).DoesNotContain("oper-status");
        await Assert.That(xml).DoesNotContain("if-index");
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
            // State fields are required by the generated type but will be excluded by configOnly: true
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
