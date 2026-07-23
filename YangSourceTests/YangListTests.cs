using YangSupport;

namespace YangSourceTests;

public class YangListTests
{
    private record Entry(string Name, int Age);

    [Fact]
    public void AddAndLookupByKey()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("alice", 30),
            new Entry("bob", 25),
        };

        Assert.Equal(2, list.Count);
        Assert.Equal(30, list["alice"].Age);
        Assert.Equal(25, list["bob"].Age);
        Assert.True(list.ContainsKey("alice"));
        Assert.False(list.ContainsKey("carol"));
    }

    [Fact]
    public void DuplicateKeyThrows()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("alice", 30)
        };
        Assert.Throws<ArgumentException>(() => list.Add(new Entry("alice", 99)));
    }

    [Fact]
    public void PreservesInsertionOrder()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("c", 1),
            new Entry("a", 2),
            new Entry("b", 3),
        };
        Assert.Equal(new[] { "c", "a", "b" }, list.Select(e => e.Name).ToArray());
        Assert.Equal("c", list[0].Name);
        Assert.Equal("a", list[1].Name);
    }

    [Fact]
    public void RemoveByKey()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("a", 1),
            new Entry("b", 2),
        };
        Assert.True(list.RemoveByKey("a"));
        Assert.Single(list);
        Assert.False(list.ContainsKey("a"));
        Assert.True(list.ContainsKey("b"));
        Assert.False(list.RemoveByKey("a"));
    }

    [Fact]
    public void AddOrReplaceUpdatesExisting()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("a", 1)
        };
        list.AddOrReplace(new Entry("a", 42));
        Assert.Single(list);
        Assert.Equal(42, list["a"].Age);
    }

    [Fact]
    public void CompositeKeyViaValueTuple()
    {
        var list = new YangList<(string, int), Entry>(e => (e.Name, e.Age))
        {
            new Entry("a", 1),
            new Entry("a", 2),
        };
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[("a", 1)].Age);
        Assert.Equal(2, list[("a", 2)].Age);
    }

    [Fact]
    public void TryGetValueWorks()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("a", 1)
        };
        Assert.True(list.TryGetValue("a", out var entry));
        Assert.Equal(1, entry.Age);
        Assert.False(list.TryGetValue("missing", out _));
    }
}
