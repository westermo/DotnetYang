using TUnit.Assertions.Enums;
using YangSupport;

namespace YangSourceTests;

public class YangListTests
{
    private record Entry(string Name, int Age);

    [Test]
    public async Task AddAndLookupByKey()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("alice", 30),
            new Entry("bob", 25),
        };

        await Assert.That(list.Count).IsEqualTo(2);
        await Assert.That(list["alice"].Age).IsEqualTo(30);
        await Assert.That(list["bob"].Age).IsEqualTo(25);
        await Assert.That(list.ContainsKey("alice")).IsTrue();
        await Assert.That(list.ContainsKey("carol")).IsFalse();
    }

    [Test]
    public async Task DuplicateKeyThrows()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("alice", 30)
        };
        await Assert.That(() => list.Add(new Entry("alice", 99))).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task PreservesInsertionOrder()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("c", 1),
            new Entry("a", 2),
            new Entry("b", 3),
        };
        await Assert.That(list.Select(e => e.Name).ToArray())
            .IsEquivalentTo(new[] { "c", "a", "b" }, CollectionOrdering.Matching);
        await Assert.That(list[0].Name).IsEqualTo("c");
        await Assert.That(list[1].Name).IsEqualTo("a");
    }

    [Test]
    public async Task RemoveByKey()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("a", 1),
            new Entry("b", 2),
        };
        await Assert.That(list.RemoveByKey("a")).IsTrue();
        await Assert.That(list.Count).IsEqualTo(1);
        await Assert.That(list.ContainsKey("a")).IsFalse();
        await Assert.That(list.ContainsKey("b")).IsTrue();
        await Assert.That(list.RemoveByKey("a")).IsFalse();
    }

    [Test]
    public async Task AddOrReplaceUpdatesExisting()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("a", 1)
        };
        list.AddOrReplace(new Entry("a", 42));
        await Assert.That(list.Count).IsEqualTo(1);
        await Assert.That(list["a"].Age).IsEqualTo(42);
    }

    [Test]
    public async Task CompositeKeyViaValueTuple()
    {
        var list = new YangList<(string, int), Entry>(e => (e.Name, e.Age))
        {
            new Entry("a", 1),
            new Entry("a", 2),
        };
        await Assert.That(list.Count).IsEqualTo(2);
        await Assert.That(list[("a", 1)].Age).IsEqualTo(1);
        await Assert.That(list[("a", 2)].Age).IsEqualTo(2);
    }

    [Test]
    public async Task TryGetValueWorks()
    {
        var list = new YangList<string, Entry>(e => e.Name)
        {
            new Entry("a", 1)
        };
        await Assert.That(list.TryGetValue("a", out var entry)).IsTrue();
        await Assert.That(entry!.Age).IsEqualTo(1);
        await Assert.That(list.TryGetValue("missing", out _)).IsFalse();
    }
}
