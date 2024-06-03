using System.Text;
using System.Xml;
using Ietf.Interfaces;
using Xunit.Abstractions;
using Yang.Attributes;

namespace YangSourceTests;

public class IetfInterfacesTests(ITestOutputHelper output)
{
    private static readonly YangNode node = new()
    {
        Interfaces = new()
        {
            Interface = new List<YangNode.InterfacesContainer.InterfaceEntry>
            {
                new()
                {
                    TypeValue = "vlan",
                    ChannelSeparation = 0,
                    PowerMode = new YangNode.InterfacesContainer.InterfaceEntry.PowerModeChoice
                    {
                        Atpc =
                            new YangNode.InterfacesContainer.InterfaceEntry.PowerModeChoice.
                                AtpcContainer
                                {
                                    AtpcLowerThreshold = 0,
                                    AtpcUpperThreshold = 1,
                                    MaximumNominalPower = 2,
                                }
                    },
                    TxFrequency = 2,
                    CodingModulationMode =
                        new YangNode.InterfacesContainer.InterfaceEntry.CodingModulationModeChoice
                        {
                            Single =
                                new YangNode.InterfacesContainer.InterfaceEntry.
                                    CodingModulationModeChoice.SingleContainer()
                                    {
                                        SelectedCmValue = "cm"
                                    }
                        },
                    FreqOrDistance =
                        new YangNode.InterfacesContainer.InterfaceEntry.FreqOrDistanceChoice
                        {
                            RxFrequency = 402,
                        },
                    BridgePort = new YangNode.InterfacesContainer.InterfaceEntry.BridgePortContainer(),
                    ModeValue = "",
                }
            }
        }
    };

    [Fact]
    public async Task AugmentedSerializationTest()
    {
        var builder = new StringBuilder();
        await using var writer = XmlWriter.Create(builder, SerializationHelper.GetStandardWriterSettings());
        await node.WriteXMLAsync(writer);
        await writer.FlushAsync();
        output.WriteLine(builder.ToString());
    }
    // [Fact]
    // public async Task AugmentedDeserializationTest()
    // {
    //     var builder = new StringBuilder();
    //     await using var writer = XmlWriter.Create(builder, SerializationHelper.GetStandardWriterSettings());
    //     await node.WriteXMLAsync(writer);
    //     await writer.FlushAsync();
    //     output.WriteLine(builder.ToString());
    //     using var ms = new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));
    //     using var reader = XmlReader.Create(ms, SerializationHelper.GetStandardReaderSettings());
    //     await reader.ReadAsync();
    //     var nNode = await YangNode.ParseAsync(reader);
    // }
}