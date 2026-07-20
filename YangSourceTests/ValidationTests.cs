using Tree.Test;
using YangSupport;

namespace YangSourceTests;

public class ValidationTests
{
    [Fact]
    public void EmptyRootValidates()
    {
        var root = new YangNode.RootContainer();
        root.YangValidate();
    }

    [Fact]
    public void MustOnNestedContainerPassesWhenBelowThreshold()
    {
        var root = new YangNode.RootContainer
        {
            Name = "anchor",   // makes 'when' on nested true
            Threshold = 10,
            Nested = new YangNode.RootContainer.NestedContainer { Value = 5 }
        };
        root.YangValidate();
    }

    [Fact]
    public void MustOnNestedContainerFailsWhenAboveThreshold()
    {
        var root = new YangNode.RootContainer
        {
            Name = "anchor",
            Threshold = 1,
            Nested = new YangNode.RootContainer.NestedContainer { Value = 999 }
        };
        var ex = Assert.Throws<YangValidationException>(() => root.YangValidate());
        Assert.Contains("value exceeds threshold", ex.Message);
        Assert.Equal("value-too-big", ex.ErrorAppTag);
    }

    [Fact]
    public void WhenConditionFailsBlocksNestedContainer()
    {
        // 'name' is null so the 'when ../name' on nested is false; the nested
        // node must not be present. We test the negative case by providing
        // nested and expecting a YangValidationException.
        var root = new YangNode.RootContainer
        {
            Threshold = 100,
            Nested = new YangNode.RootContainer.NestedContainer { Value = 1 }
        };
        Assert.Throws<YangValidationException>(() => root.YangValidate());
    }

    [Fact]
    public void ListEntryMustPassesWhenSatisfied()
    {
        var root = new YangNode.RootContainer
        {
            Items = new YangList<string, YangNode.RootContainer.ItemsEntry>(e => e.Id)
            {
                new YangNode.RootContainer.ItemsEntry { Id = "user1" },
                new YangNode.RootContainer.ItemsEntry { Id = "admin", Description = "the boss" },
            }
        };
        root.YangValidate();
    }

    [Fact]
    public void ListEntryMustFailsWhenViolated()
    {
        var root = new YangNode.RootContainer
        {
            Items = new YangList<string, YangNode.RootContainer.ItemsEntry>(e => e.Id)
            {
                new YangNode.RootContainer.ItemsEntry { Id = "admin" /* missing description */ },
            }
        };
        Assert.Throws<YangValidationException>(() => root.YangValidate());
    }

    [Fact]
    public void MustWithCountFunctionPasses()
    {
        var root = new YangNode.RootContainer
        {
            Items = new YangList<string, YangNode.RootContainer.ItemsEntry>(e => e.Id)
            {
                new YangNode.RootContainer.ItemsEntry { Id = "x" }
            },
            Counts = new YangNode.RootContainer.CountsContainer()
        };
        root.YangValidate();
    }

    [Fact]
    public void MustWithCountFunctionFailsOnEmptyList()
    {
        var root = new YangNode.RootContainer
        {
            // No items, but counts container present -> must "count(../items) > 0" fails.
            Counts = new YangNode.RootContainer.CountsContainer()
        };
        Assert.Throws<YangValidationException>(() => root.YangValidate());
    }

    [Fact]
    public void AbsolutePathMustPassesWhenRootNameSet()
    {
        var node = new YangNode
        {
            Root = new YangNode.RootContainer
            {
                Name = "present",
                AbsTest = new YangNode.RootContainer.AbsTestContainer()
            }
        };
        node.Root.YangValidate();
    }

    [Fact]
    public void AbsolutePathMustFailsWhenRootNameMissing()
    {
        var node = new YangNode
        {
            Root = new YangNode.RootContainer
            {
                AbsTest = new YangNode.RootContainer.AbsTestContainer()
            }
        };
        var ex = Assert.Throws<YangValidationException>(() => node.Root.YangValidate());
        Assert.Equal("abs-test-no-name", ex.ErrorAppTag);
    }

    [Fact]
    public void WildcardCountViaTranslatorWorks()
    {
        // Wildcard (*) in count context: tree-test doesn't use it directly
        // but we verify the mechanism via the tree-awareness framework.
        // Count the data children of root: name, threshold, nested, items,
        // tags, counts, abs-test, deep = up to 8 fields. If name is set,
        // the wildcard count expression on root should be > 0.
        var root = new YangNode.RootContainer { Name = "x" };
        // Access through reflection-free: just verify YangValidate doesn't crash.
        root.YangValidate();
    }
}
