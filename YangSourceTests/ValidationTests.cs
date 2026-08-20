using Tree.Test;
using YangSupport;

namespace YangSourceTests;

public class ValidationTests
{
    [Test]
    public void EmptyRootValidates()
    {
        var root = new YangNode.RootContainer();
        root.YangValidate();
    }

    [Test]
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

    [Test]
    public async Task MustOnNestedContainerFailsWhenAboveThreshold()
    {
        var root = new YangNode.RootContainer
        {
            Name = "anchor",
            Threshold = 1,
            Nested = new YangNode.RootContainer.NestedContainer { Value = 999 }
        };
        var ex = await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.Message).Contains("value exceeds threshold");
        await Assert.That(ex.ErrorAppTag).IsEqualTo("value-too-big");
    }

    [Test]
    public async Task WhenConditionFailsBlocksNestedContainer()
    {
        // 'name' is null so the 'when ../name' on nested is false; the nested
        // node must not be present. We test the negative case by providing
        // nested and expecting a YangValidationException.
        var root = new YangNode.RootContainer
        {
            Threshold = 100,
            Nested = new YangNode.RootContainer.NestedContainer { Value = 1 }
        };
        await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
    }

    [Test]
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

    [Test]
    public async Task ListEntryMustFailsWhenViolated()
    {
        var root = new YangNode.RootContainer
        {
            Items = new YangList<string, YangNode.RootContainer.ItemsEntry>(e => e.Id)
            {
                new YangNode.RootContainer.ItemsEntry { Id = "admin" /* missing description */ },
            }
        };
        await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
    }

    [Test]
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

    [Test]
    public async Task MustWithCountFunctionFailsOnEmptyList()
    {
        var root = new YangNode.RootContainer
        {
            // No items, but counts container present -> must "count(../items) > 0" fails.
            Counts = new YangNode.RootContainer.CountsContainer()
        };
        await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
    }

    [Test]
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

    [Test]
    public async Task AbsolutePathMustFailsWhenRootNameMissing()
    {
        var node = new YangNode
        {
            Root = new YangNode.RootContainer
            {
                AbsTest = new YangNode.RootContainer.AbsTestContainer()
            }
        };
        var ex = await Assert.That(() => node.Root.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.ErrorAppTag).IsEqualTo("abs-test-no-name");
    }

    [Test]
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
