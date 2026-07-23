using YangSupport;
using Tree.Test;

namespace YangSourceTests;

public class TreeAwarenessTests
{
    [Test]
    public async Task ContainerSetsYangParentOnAssignment()
    {
        var node = new YangNode
        {
            Root = new YangNode.RootContainer()
        };
        await Assert.That(node.Root!.YangParent).IsSameReferenceAs(node);
    }

    [Test]
    public async Task NestedContainerGetsYangParent()
    {
        var node = new YangNode
        {
            Root = new YangNode.RootContainer
            {
                Nested = new YangNode.RootContainer.NestedContainer { Value = 42 }
            }
        };
        await Assert.That(node.Root!.Nested!.YangParent).IsSameReferenceAs(node.Root);
    }

    [Test]
    public async Task NestedListEntryGetsYangParentFromContainerSetter()
    {
        var node = new YangNode
        {
            Root = new YangNode.RootContainer
            {
                Items = new YangList<string, YangNode.RootContainer.ItemsEntry>(e => e.Id)
                {
                    new YangNode.RootContainer.ItemsEntry { Id = "a" },
                    new YangNode.RootContainer.ItemsEntry { Id = "b" },
                }
            }
        };

        var first = node.Root!.Items![0];
        var second = node.Root.Items[1];
        await Assert.That(first.YangParent).IsSameReferenceAs(node.Root);
        await Assert.That(second.YangParent).IsSameReferenceAs(node.Root);
    }

    [Test]
    public async Task YangListAddWiresYangParentAfterAssignment()
    {
        var node = new YangNode
        {
            Root = new YangNode.RootContainer
            {
                Items = new YangList<string, YangNode.RootContainer.ItemsEntry>(e => e.Id)
            }
        };

        var entry = new YangNode.RootContainer.ItemsEntry { Id = "x" };
        node.Root!.Items!.Add(entry);
        await Assert.That(entry.YangParent).IsSameReferenceAs(node.Root);
    }

    [Test]
    public async Task YangListRemoveByKeyClearsYangParent()
    {
        var entry = new YangNode.RootContainer.ItemsEntry { Id = "x" };
        var node = new YangNode
        {
            Root = new YangNode.RootContainer
            {
                Items = new YangList<string, YangNode.RootContainer.ItemsEntry>(e => e.Id)
                {
                    entry
                }
            }
        };
        await Assert.That(entry.YangParent).IsSameReferenceAs(node.Root);
        await Assert.That(node.Root!.Items!.RemoveByKey("x")).IsTrue();
        await Assert.That(entry.YangParent).IsNull();
    }

    [Test]
    public async Task ReassigningContainerClearsOldParentAndSetsNew()
    {
        var shared = new YangNode.RootContainer();
        var nodeA = new YangNode { Root = shared };
        await Assert.That(shared.YangParent).IsSameReferenceAs(nodeA);

        var nodeB = new YangNode();
        nodeA.Root = null;
        await Assert.That(shared.YangParent).IsNull();
        nodeB.Root = shared;
        await Assert.That(shared.YangParent).IsSameReferenceAs(nodeB);
    }

    [Test]
    public async Task ParentWiringSurvivesAddAfterAssignment()
    {
        var list = new YangList<string, YangNode.RootContainer.ItemsEntry>(e => e.Id);
        var entryBefore = new YangNode.RootContainer.ItemsEntry { Id = "before" };
        list.Add(entryBefore);

        var root = new YangNode.RootContainer { Items = list };
        await Assert.That(entryBefore.YangParent).IsSameReferenceAs(root);

        var entryAfter = new YangNode.RootContainer.ItemsEntry { Id = "after" };
        list.Add(entryAfter);
        await Assert.That(entryAfter.YangParent).IsSameReferenceAs(root);
    }

    [Test]
    public async Task ParseAsyncWiresYangParent()
    {
        const string xml = """
                           <root xmlns="urn:dotnet:yang:tree-test">
                             <name>r1</name>
                             <nested><value>7</value></nested>
                             <items><id>a</id><description>first</description></items>
                             <items><id>b</id><description>second</description></items>
                           </root>
                           """;
        using var reader = System.Xml.XmlReader.Create(new System.IO.StringReader(xml),
            new System.Xml.XmlReaderSettings { Async = true, IgnoreWhitespace = true });
        await reader.ReadAsync();
        var root = await YangNode.RootContainer.ParseAsync(reader);

        await Assert.That(root.Nested).IsNotNull();
        await Assert.That(root.Nested!.YangParent).IsSameReferenceAs(root);

        await Assert.That(root.Items).IsNotNull();
        await Assert.That(root.Items!.Count).IsEqualTo(2);
        foreach (var item in root.Items)
        {
            await Assert.That(item.YangParent).IsSameReferenceAs(root);
        }
    }
}
