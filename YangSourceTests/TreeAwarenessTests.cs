using YangSupport;
using Tree.Test;

namespace YangSourceTests;

public class TreeAwarenessTests
{
    [Fact]
    public void ContainerSetsYangParentOnAssignment()
    {
        var node = new YangNode
        {
            Root = new YangNode.RootContainer()
        };
        Assert.Same(node, node.Root!.YangParent);
    }

    [Fact]
    public void NestedContainerGetsYangParent()
    {
        var node = new YangNode
        {
            Root = new YangNode.RootContainer
            {
                Nested = new YangNode.RootContainer.NestedContainer { Value = 42 }
            }
        };
        Assert.Same(node.Root, node.Root!.Nested!.YangParent);
    }

    [Fact]
    public void NestedListEntryGetsYangParentFromContainerSetter()
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
        Assert.Same(node.Root, first.YangParent);
        Assert.Same(node.Root, second.YangParent);
    }

    [Fact]
    public void YangListAddWiresYangParentAfterAssignment()
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
        Assert.Same(node.Root, entry.YangParent);
    }

    [Fact]
    public void YangListRemoveByKeyClearsYangParent()
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
        Assert.Same(node.Root, entry.YangParent);
        Assert.True(node.Root!.Items!.RemoveByKey("x"));
        Assert.Null(entry.YangParent);
    }

    [Fact]
    public void ReassigningContainerClearsOldParentAndSetsNew()
    {
        var shared = new YangNode.RootContainer();
        var nodeA = new YangNode { Root = shared };
        Assert.Same(nodeA, shared.YangParent);

        var nodeB = new YangNode();
        nodeA.Root = null;
        Assert.Null(shared.YangParent);
        nodeB.Root = shared;
        Assert.Same(nodeB, shared.YangParent);
    }

    [Fact]
    public void ParentWiringSurvivesAddAfterAssignment()
    {
        var list = new YangList<string, YangNode.RootContainer.ItemsEntry>(e => e.Id);
        var entryBefore = new YangNode.RootContainer.ItemsEntry { Id = "before" };
        list.Add(entryBefore);

        var root = new YangNode.RootContainer { Items = list };
        Assert.Same(root, entryBefore.YangParent);

        var entryAfter = new YangNode.RootContainer.ItemsEntry { Id = "after" };
        list.Add(entryAfter);
        Assert.Same(root, entryAfter.YangParent);
    }

    [Fact]
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

        Assert.NotNull(root.Nested);
        Assert.Same(root, root.Nested!.YangParent);

        Assert.NotNull(root.Items);
        Assert.Equal(2, root.Items!.Count);
        foreach (var item in root.Items)
        {
            Assert.Same(root, item.YangParent);
        }
    }
}

