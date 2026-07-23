using Rfc7950.Compliance.Test;
using YangSupport;

namespace YangSourceTests;

/// <summary>
/// Tests verifying RFC 7950 compliance for the remaining gaps:
/// - Gap 10: ordered-by user (positional insert/move)
/// - Gap 11: unique constraint enforcement
/// - Gap 13: min-elements / max-elements enforcement
/// - Gap 15: instance-identifier resolution
/// </summary>
public class Rfc7950ComplianceTests
{
    // ========================================================================
    // Gap 13: min-elements / max-elements
    // ========================================================================

    [Fact]
    public void MinElements_PassesWhenSatisfied()
    {
        var root = new YangNode.ConstrainedListsContainer
        {
            BoundedList = new YangList<string, YangNode.ConstrainedListsContainer.BoundedListEntry>(
                e => e.Name)
            {
                new() { Name = "item1", Value = 1 }
            },
            BoundedTagsList = new[] { "tag1" }
        };
        root.YangValidate();
    }

    [Fact]
    public void MinElements_FailsWhenListEmpty()
    {
        var root = new YangNode.ConstrainedListsContainer
        {
            BoundedList = new YangList<string, YangNode.ConstrainedListsContainer.BoundedListEntry>(
                e => e.Name),
            BoundedTagsList = new[] { "tag1" }
        };
        var ex = Assert.Throws<YangValidationException>(() => root.YangValidate());
        Assert.Contains("min-elements", ex.Message);
        Assert.Contains("BoundedList", ex.Message);
    }

    [Fact]
    public void MinElements_FailsWhenListNull()
    {
        var root = new YangNode.ConstrainedListsContainer
        {
            BoundedTagsList = new[] { "tag1" }
        };
        var ex = Assert.Throws<YangValidationException>(() => root.YangValidate());
        Assert.Contains("min-elements", ex.Message);
    }

    [Fact]
    public void MaxElements_FailsWhenExceeded()
    {
        var root = new YangNode.ConstrainedListsContainer
        {
            BoundedList = new YangList<string, YangNode.ConstrainedListsContainer.BoundedListEntry>(
                e => e.Name)
            {
                new() { Name = "a", Value = 1 },
                new() { Name = "b", Value = 2 },
                new() { Name = "c", Value = 3 },
                new() { Name = "d", Value = 4 },
                new() { Name = "e", Value = 5 },
                new() { Name = "f", Value = 6 }, // 6 > max-elements 5
            },
            BoundedTagsList = new[] { "tag1" }
        };
        var ex = Assert.Throws<YangValidationException>(() => root.YangValidate());
        Assert.Contains("max-elements", ex.Message);
        Assert.Contains("BoundedList", ex.Message);
    }

    [Fact]
    public void MaxElements_PassesAtLimit()
    {
        var root = new YangNode.ConstrainedListsContainer
        {
            BoundedList = new YangList<string, YangNode.ConstrainedListsContainer.BoundedListEntry>(
                e => e.Name)
            {
                new() { Name = "a", Value = 1 },
                new() { Name = "b", Value = 2 },
                new() { Name = "c", Value = 3 },
                new() { Name = "d", Value = 4 },
                new() { Name = "e", Value = 5 },
            },
            BoundedTagsList = new[] { "tag1" }
        };
        root.YangValidate(); // exactly 5 = max-elements 5
    }

    [Fact]
    public void MinElements_LeafList_FailsWhenNull()
    {
        var root = new YangNode.ConstrainedListsContainer
        {
            BoundedList = new YangList<string, YangNode.ConstrainedListsContainer.BoundedListEntry>(
                e => e.Name)
            {
                new() { Name = "x", Value = 1 }
            }
            // BoundedTagsList is null — violates min-elements 1
        };
        var ex = Assert.Throws<YangValidationException>(() => root.YangValidate());
        Assert.Contains("min-elements", ex.Message);
    }

    [Fact]
    public void MaxElements_LeafList_FailsWhenExceeded()
    {
        var root = new YangNode.ConstrainedListsContainer
        {
            BoundedList = new YangList<string, YangNode.ConstrainedListsContainer.BoundedListEntry>(
                e => e.Name)
            {
                new() { Name = "x", Value = 1 }
            },
            BoundedTagsList = new[] { "a", "b", "c", "d" } // 4 > max-elements 3
        };
        var ex = Assert.Throws<YangValidationException>(() => root.YangValidate());
        Assert.Contains("max-elements", ex.Message);
    }

    // ========================================================================
    // Gap 11: unique constraint
    // ========================================================================

    [Fact]
    public void Unique_PassesWhenAllDistinct()
    {
        var root = new YangNode.UniqueTestContainer
        {
            Employees = new YangList<uint, YangNode.UniqueTestContainer.EmployeesEntry>(
                e => e.Id)
            {
                new() { Id = 1, Email = "alice@co.com", Department = "eng", BadgeNumber = 100 },
                new() { Id = 2, Email = "bob@co.com", Department = "eng", BadgeNumber = 101 },
                new() { Id = 3, Email = "carol@co.com", Department = "sales", BadgeNumber = 100 },
            }
        };
        root.YangValidate();
    }

    [Fact]
    public void Unique_SingleField_FailsOnDuplicate()
    {
        var root = new YangNode.UniqueTestContainer
        {
            Employees = new YangList<uint, YangNode.UniqueTestContainer.EmployeesEntry>(
                e => e.Id)
            {
                new() { Id = 1, Email = "same@co.com", Department = "eng", BadgeNumber = 100 },
                new() { Id = 2, Email = "same@co.com", Department = "sales", BadgeNumber = 101 },
            }
        };
        var ex = Assert.Throws<YangValidationException>(() => root.YangValidate());
        Assert.Contains("unique", ex.Message);
        Assert.Contains("email", ex.Message);
    }

    [Fact]
    public void Unique_CompositeFields_FailsOnDuplicate()
    {
        var root = new YangNode.UniqueTestContainer
        {
            Employees = new YangList<uint, YangNode.UniqueTestContainer.EmployeesEntry>(
                e => e.Id)
            {
                new() { Id = 1, Email = "a@co.com", Department = "eng", BadgeNumber = 42 },
                new() { Id = 2, Email = "b@co.com", Department = "eng", BadgeNumber = 42 },
                // Same (department, badge-number) combination
            }
        };
        var ex = Assert.Throws<YangValidationException>(() => root.YangValidate());
        Assert.Contains("unique", ex.Message);
    }

    // ========================================================================
    // Gap 10: ordered-by user
    // ========================================================================

    [Fact]
    public void OrderedByUser_PreservesInsertionOrder()
    {
        var list = new YangList<string, YangNode.OrderedTestContainer.UserOrderedListEntry>(
            e => e.Name)
        {
            new() { Name = "c", Priority = 3 },
            new() { Name = "a", Priority = 1 },
            new() { Name = "b", Priority = 2 },
        };

        Assert.Equal(new[] { "c", "a", "b" }, list.Select(e => e.Name).ToArray());
    }

    [Fact]
    public void OrderedByUser_InsertBefore()
    {
        var list = new YangList<string, YangNode.OrderedTestContainer.UserOrderedListEntry>(
            e => e.Name)
        {
            new() { Name = "first", Priority = 1 },
            new() { Name = "last", Priority = 3 },
        };

        list.InsertBefore("last", new() { Name = "middle", Priority = 2 });

        Assert.Equal(new[] { "first", "middle", "last" }, list.Select(e => e.Name).ToArray());
    }

    [Fact]
    public void OrderedByUser_InsertAfter()
    {
        var list = new YangList<string, YangNode.OrderedTestContainer.UserOrderedListEntry>(
            e => e.Name)
        {
            new() { Name = "first", Priority = 1 },
            new() { Name = "last", Priority = 3 },
        };

        list.InsertAfter("first", new() { Name = "second", Priority = 2 });

        Assert.Equal(new[] { "first", "second", "last" }, list.Select(e => e.Name).ToArray());
    }

    [Fact]
    public void OrderedByUser_Move()
    {
        var list = new YangList<string, YangNode.OrderedTestContainer.UserOrderedListEntry>(
            e => e.Name)
        {
            new() { Name = "a", Priority = 1 },
            new() { Name = "b", Priority = 2 },
            new() { Name = "c", Priority = 3 },
        };

        // Move "c" before "a"
        list.Move("c", "a", before: true);
        Assert.Equal(new[] { "c", "a", "b" }, list.Select(e => e.Name).ToArray());
    }

    [Fact]
    public void OrderedByUser_MoveToFirst()
    {
        var list = new YangList<string, YangNode.OrderedTestContainer.UserOrderedListEntry>(
            e => e.Name)
        {
            new() { Name = "a", Priority = 1 },
            new() { Name = "b", Priority = 2 },
            new() { Name = "c", Priority = 3 },
        };

        list.Move("c", first: true);
        Assert.Equal(new[] { "c", "a", "b" }, list.Select(e => e.Name).ToArray());
    }

    [Fact]
    public void OrderedByUser_MoveToLast()
    {
        var list = new YangList<string, YangNode.OrderedTestContainer.UserOrderedListEntry>(
            e => e.Name)
        {
            new() { Name = "a", Priority = 1 },
            new() { Name = "b", Priority = 2 },
            new() { Name = "c", Priority = 3 },
        };

        list.Move("a", first: false);
        Assert.Equal(new[] { "b", "c", "a" }, list.Select(e => e.Name).ToArray());
    }

    [Fact]
    public void OrderedByUser_InsertBefore_InvalidKeyThrows()
    {
        var list = new YangList<string, YangNode.OrderedTestContainer.UserOrderedListEntry>(
            e => e.Name)
        {
            new() { Name = "a", Priority = 1 },
        };

        Assert.Throws<ArgumentException>(() =>
            list.InsertBefore("nonexistent", new() { Name = "b", Priority = 2 }));
    }

    // ========================================================================
    // Gap 15: instance-identifier (GetChild navigation)
    // ========================================================================

    [Fact]
    public void GetChild_ReturnsCorrectProperty()
    {
        var node = new YangNode
        {
            Refs = new YangNode.RefsContainer
            {
                TargetName = "hello"
            }
        };
        var child = node.GetChild("refs");
        Assert.NotNull(child);
        Assert.IsType<YangNode.RefsContainer>(child);
    }

    [Fact]
    public void GetChild_ReturnsNullForUnknown()
    {
        var node = new YangNode();
        Assert.Null(node.GetChild("nonexistent"));
    }

    [Fact]
    public void InstanceIdentifier_ParseAndToString()
    {
        var id = InstanceIdentifier.Parse("/rfc7950-compliance-test:refs/target-name");
        Assert.Equal("/rfc7950-compliance-test:refs/target-name", id.ToString());
    }
}
