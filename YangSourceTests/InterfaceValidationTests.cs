using Tree.Test;
using YangSupport;

namespace YangSourceTests;

public class InterfaceValidationTests
{
    private static YangNode.RootContainer.ShapeContainer Shape(string kind,
        YangNode.RootContainer.ShapeContainer.FormChoice form) =>
        new() { Kind = kind, Form = form };

    private static YangNode.RootContainer.ShapeContainer.FormChoice Circle(uint radius, string? label = null) =>
        new()
        {
            CircleCaseValue = new YangNode.RootContainer.ShapeContainer.FormChoice.CircleCaseValueCase
            {
                Radius = radius,
                Label = label
            }
        };

    [Test]
    public async Task GeneratedContainersImplementIYangValidatable()
    {
        await Assert.That(new YangNode.RootContainer() is IYangValidatable).IsTrue();
        await Assert.That(new YangNode() is IYangValidatable).IsTrue();
    }

    [Test]
    public async Task ConfigurationImplementsRuntimeInterfaces()
    {
        var config = new YangSource.Configuration();
        await Assert.That(config is IYangValidatable).IsTrue();
        await Assert.That(config is IYangInstanceIdentifierRoot).IsTrue();
        await Assert.That(config is IYangNode).IsTrue();
    }

    [Test]
    public async Task ValidationThroughInterfaceThrowsUnwrappedException()
    {
        IYangValidatable node = new YangNode.RootContainer
        {
            Name = "anchor",
            Threshold = 1,
            Nested = new YangNode.RootContainer.NestedContainer { Value = 999 }
        };
        var ex = await Assert.That(() => node.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.ErrorAppTag).IsEqualTo("value-too-big");
    }

    [Test]
    public void WhenOnCasePassesWhenConditionHolds()
    {
        var root = new YangNode.RootContainer { Shape = Shape("round", Circle(5, "small")) };
        root.YangValidate();
    }

    [Test]
    public async Task WhenOnCaseFailsWhenConditionDoesNotHold()
    {
        var root = new YangNode.RootContainer { Shape = Shape("square", Circle(5)) };
        var ex = await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/shape/form/circle");
    }

    [Test]
    public void OtherCaseUnaffectedByCircleWhen()
    {
        var root = new YangNode.RootContainer
        {
            Shape = Shape("square", new YangNode.RootContainer.ShapeContainer.FormChoice
            {
                SquareCaseValue = new YangNode.RootContainer.ShapeContainer.FormChoice.SquareCaseValueCase
                {
                    Side = 3
                }
            })
        };
        root.YangValidate();
    }

    [Test]
    public async Task MustNavigatesThroughChoiceAndCase()
    {
        var root = new YangNode.RootContainer { Shape = Shape("round", Circle(200)) };
        var ex = await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.ErrorAppTag).IsEqualTo("radius-too-large");
    }

    [Test]
    public async Task UsesWhenRejectsLeafWhenConditionFalse()
    {
        var root = new YangNode.RootContainer
        {
            Gated = new YangNode.RootContainer.GatedContainer { Flag = false, ExtraVal = "x" }
        };
        var ex = await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/gated/extra-val");
    }

    [Test]
    public async Task UsesWhenRejectsContainerWhenConditionFalse()
    {
        var root = new YangNode.RootContainer
        {
            Gated = new YangNode.RootContainer.GatedContainer
            {
                Flag = false,
                ExtraC = new YangNode.RootContainer.GatedContainer.ExtraCContainer { V = "y" }
            }
        };
        var ex = await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/gated/extra-c");
    }

    [Test]
    public void UsesWhenAcceptsNodesWhenConditionTrue()
    {
        var root = new YangNode.RootContainer
        {
            Gated = new YangNode.RootContainer.GatedContainer
            {
                Flag = true,
                ExtraVal = "x",
                ExtraC = new YangNode.RootContainer.GatedContainer.ExtraCContainer { V = "y" }
            }
        };
        root.YangValidate();
    }

    [Test]
    public void UsesWhenIgnoresAbsentNodes()
    {
        var root = new YangNode.RootContainer
        {
            Gated = new YangNode.RootContainer.GatedContainer { Flag = false }
        };
        root.YangValidate();
    }

    private static YangNode.RootContainer Wrapped(bool? enabled, uint? boost) => new()
    {
        Wrapped = new YangNode.RootContainer.WrappedContainer
        {
            Enabled = enabled,
            Settings = new YangNode.RootContainer.WrappedContainer.SettingsContainer { Mode = "m", Boost = boost }
        }
    };

    [Test]
    public async Task AugmentWhenOnGroupingTargetRejectsWhenConditionFalse()
    {
        var root = Wrapped(false, 3);
        var ex = await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/wrapped/settings/boost");
    }

    [Test]
    public void AugmentWhenOnGroupingTargetAcceptsWhenConditionTrue()
    {
        Wrapped(true, 3).YangValidate();
    }

    [Test]
    public void AugmentWhenOnGroupingTargetIgnoresAbsentNode()
    {
        Wrapped(false, null).YangValidate();
    }

    private static YangNode.RootContainer Deep(uint level, string? bonus) => new()
    {
        Deep = new YangNode.RootContainer.DeepContainer
        {
            Inner = new YangNode.RootContainer.DeepContainer.InnerContainer { Level = level, Bonus = bonus }
        }
    };

    [Test]
    public async Task AugmentWhenRejectsWhenConditionFalse()
    {
        var root = Deep(2, "b");
        var ex = await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/deep/inner/bonus");
    }

    [Test]
    public void AugmentWhenAcceptsWhenConditionTrue()
    {
        Deep(9, "b").YangValidate();
    }

    [Test]
    public void AugmentWhenIgnoresAbsentNode()
    {
        Deep(2, null).YangValidate();
    }

    private static (YangSource.Configuration Config, YangNode Module) ResolverFixture()
    {
        var module = new YangNode
        {
            Root = new YangNode.RootContainer
            {
                Items = new YangList<string, YangNode.RootContainer.ItemsEntry>(e => e.Id)
                {
                    new() { Id = "first" },
                    new() { Id = "a]b/c=d" }
                },
                Pairs = new YangList<(string, uint), YangNode.RootContainer.PairsEntry>(e => (e.A, e.B))
                {
                    new() { A = "x", B = 1, Note = "x1" },
                    new() { A = "x", B = 2, Note = "x2" },
                    new() { A = "y", B = 2, Note = "y2" }
                },
                TagsList = ["red", "green"]
            }
        };
        return (new YangSource.Configuration { TreeTest = module }, module);
    }

    [Test]
    public async Task KeyedListEntriesImplementIYangListEntry()
    {
        await Assert.That(new YangNode.RootContainer.ItemsEntry { Id = "a" } is IYangListEntry).IsTrue();
        await Assert.That(new YangNode.RootContainer.PairsEntry { A = "a", B = 1 } is IYangListEntry).IsTrue();
    }

    [Test]
    public async Task GeneratedKeyMatchComparesLexicalValues()
    {
        IYangListEntry entry = new YangNode.RootContainer.PairsEntry { A = "x", B = 2 };
        await Assert.That(entry.YangMatchesKeys(new Dictionary<string, string> { ["a"] = "x", ["b"] = "2" })).IsTrue();
        await Assert.That(entry.YangMatchesKeys(new Dictionary<string, string> { ["a"] = "x", ["b"] = "3" })).IsFalse();
        await Assert.That(entry.YangMatchesKeys(new Dictionary<string, string> { ["a"] = "x" })).IsFalse();
    }

    [Test]
    public async Task ResolvesMultiKeyListEntry()
    {
        var (config, module) = ResolverFixture();
        var resolved = config.ResolveInstanceIdentifier("/tree-test:root/tree-test:pairs[tree-test:a='x'][tree-test:b='2']");
        await Assert.That(resolved).IsSameReferenceAs(module.Root!.Pairs!.Single(p => p.Note == "x2"));
    }

    [Test]
    public async Task ResolvesKeyContainingDelimiters()
    {
        var (config, module) = ResolverFixture();
        var resolved = config.ResolveInstanceIdentifier("/tree-test:root/items[id=\"a]b/c=d\"]");
        await Assert.That(resolved).IsSameReferenceAs(module.Root!.Items!.Single(i => i.Id == "a]b/c=d"));
    }

    [Test]
    public async Task ResolvesLeafBelowListEntry()
    {
        var (config, _) = ResolverFixture();
        var resolved = config.ResolveInstanceIdentifier("/tree-test:root/pairs[a='y'][b='2']/note");
        await Assert.That(resolved).IsEqualTo("y2");
    }

    [Test]
    public async Task ResolvesPositionalAndLeafListPredicates()
    {
        var (config, module) = ResolverFixture();
        await Assert.That(config.ResolveInstanceIdentifier("/tree-test:root/items[2]"))
            .IsSameReferenceAs(module.Root!.Items!.ElementAt(1));
        await Assert.That(config.ResolveInstanceIdentifier("/tree-test:root/tags[.='green']")).IsEqualTo("green");
    }

    [Test]
    public async Task ResolvesFromModuleNode()
    {
        var (_, module) = ResolverFixture();
        var resolved = InstanceIdentifierResolver.Resolve(module, "/root/items[id='first']");
        await Assert.That(resolved).IsSameReferenceAs(module.Root!.Items!.First());
    }

    [Test]
    public async Task UnmatchedOrMalformedPathsResolveToNull()
    {
        var (config, _) = ResolverFixture();
        await Assert.That(config.ResolveInstanceIdentifier("/tree-test:root/pairs[a='x'][b='9']")).IsNull();
        await Assert.That(config.ResolveInstanceIdentifier("/tree-test:root/items[id='missing']")).IsNull();
        await Assert.That(config.ResolveInstanceIdentifier("/tree-test:root/items[id='unterminated]")).IsNull();
        await Assert.That(config.ResolveInstanceIdentifier("/tree-test:root/items[5]")).IsNull();
        await Assert.That(config.ResolveInstanceIdentifier("tree-test:root")).IsNull();
    }

    [Test]
    public async Task CurrentFilterPathWhenRejectsFalseCondition()
    {
        var root = new YangNode.RootContainer { Xp = new YangNode.RootContainer.XpContainer { Kind = "off", CurRef = "x" } };
        var ex = await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/xp/cur-ref");
    }

    [Test]
    public void CurrentFilterPathWhenAcceptsTrueCondition()
    {
        new YangNode.RootContainer { Xp = new YangNode.RootContainer.XpContainer { Kind = "on", CurRef = "x" } }.YangValidate();
    }

    [Test]
    public async Task LeafStepPredicateWhenRejectsNonMatchingValue()
    {
        var root = new YangNode.RootContainer { Lp = new YangNode.RootContainer.LpContainer { Mode = "c", Gated = "x" } };
        var ex = await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/lp/gated");
    }

    [Test]
    public void LeafStepPredicateWhenAcceptsMatchingValue()
    {
        new YangNode.RootContainer { Lp = new YangNode.RootContainer.LpContainer { Mode = "a", Gated = "x" } }.YangValidate();
        new YangNode.RootContainer { Lp = new YangNode.RootContainer.LpContainer { Mode = "b", Gated = "x" } }.YangValidate();
    }

    [Test]
    public async Task EnumLeafStepPredicateWhenRejectsNonMatchingMember()
    {
        var root = new YangNode.RootContainer { Lp = new YangNode.RootContainer.LpContainer { EmodeValue = YangNode.RootContainer.LpContainer.Emode.Other, Egated = "x" } };
        var ex = await Assert.That(() => root.YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/lp/egated");
    }

    [Test]
    public void EnumLeafStepPredicateWhenAcceptsMatchingMember()
    {
        new YangNode.RootContainer { Lp = new YangNode.RootContainer.LpContainer { EmodeValue = YangNode.RootContainer.LpContainer.Emode.PreShared, Egated = "x" } }.YangValidate();
        new YangNode.RootContainer { Lp = new YangNode.RootContainer.LpContainer { EmodeValue = YangNode.RootContainer.LpContainer.Emode.Eap, Egated = "x" } }.YangValidate();
    }

    private static YangNode LkRoot(YangNode.ComponentKindIdentity? type, string bridge = "br0", string component = "c0", string? vlanOnly = "x", params uint[] weights)
    {
        var components = new YangList<string, YangNode.RootContainer.LkContainer.BridgeEntry.ComponentEntry>(e => e.Name)
        {
            new YangNode.RootContainer.LkContainer.BridgeEntry.ComponentEntry { Name = "c0", Type = type },
        };
        for (var i = 0; i < weights.Length; i++)
        {
            components.Add(new YangNode.RootContainer.LkContainer.BridgeEntry.ComponentEntry { Name = $"w{i}", Weight = weights[i] });
        }
        return new YangNode
        {
            Root = new YangNode.RootContainer
            {
                Lk = new YangNode.RootContainer.LkContainer
                {
                    Bridge = new YangList<YangNode.BridgeName, YangNode.RootContainer.LkContainer.BridgeEntry>(e => e.Name)
                    {
                        new YangNode.RootContainer.LkContainer.BridgeEntry { Name = "br0", Component = components },
                    },
                    BridgeName = bridge,
                    ComponentName = component,
                    VlanOnly = vlanOnly,
                    HeavyLimit = "x",
                },
            },
        };
    }

    [Test]
    public void ListKeyPredicateWhenAcceptsMatchingEntryWithOtherIdentity()
    {
        LkRoot(YangNode.ComponentKindIdentity.CVlan).YangValidate();
    }

    [Test]
    public async Task ListKeyPredicateWhenRejectsMatchingEntryWithExcludedIdentity()
    {
        var ex = await Assert.That(() => LkRoot(YangNode.ComponentKindIdentity.DBridge).YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/lk/vlan-only");
    }

    [Test]
    public async Task ListKeyPredicateWhenRejectsWhenNoEntryMatches()
    {
        // An empty node-set compared with '!=' is false (RFC 7950 / XPath 1.0 3.4).
        await Assert.That(() => LkRoot(YangNode.ComponentKindIdentity.CVlan, bridge: "missing").YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(() => LkRoot(YangNode.ComponentKindIdentity.CVlan, component: "missing").YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(() => LkRoot(null).YangValidate()).ThrowsExactly<YangValidationException>();
    }

    [Test]
    public void ListKeyPredicateWhenIgnoredWhenLeafAbsent()
    {
        LkRoot(YangNode.ComponentKindIdentity.DBridge, bridge: "missing", vlanOnly: null).YangValidate();
    }

    [Test]
    public async Task NonKeyListPredicateFiltersEntries()
    {
        LkRoot(YangNode.ComponentKindIdentity.CVlan, "br0", "c0", "x", 5, 20, 7).YangValidate();
        var ex = await Assert.That(() => LkRoot(YangNode.ComponentKindIdentity.CVlan, "br0", "c0", "x", 11, 20).YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/lk/heavy-limit");
    }

    private static YangNode.RootContainer BtRoot(YangNode.RootContainer.BtContainer.Flags? flags, params YangNode.ServerKind?[] servers)
    {
        var list = new YangList<string, YangNode.RootContainer.BtContainer.ServerEntry>(e => e.Name);
        for (var i = 0; i < servers.Length; i++)
        {
            list.Add(new YangNode.RootContainer.BtContainer.ServerEntry { Name = $"s{i}", ServerType = servers[i] });
        }
        return new YangNode.RootContainer
        {
            Bt = new YangNode.RootContainer.BtContainer
            {
                Server = list,
                AuthRequired = "x",
                FlagsValue = flags,
                HighOnly = "x",
            },
        };
    }

    [Test]
    public void BitIsSetAcceptsWhenFirstEntryHasBit()
    {
        BtRoot(YangNode.RootContainer.BtContainer.Flags.HighBit, YangNode.ServerKind.AuthN).YangValidate();
        BtRoot(YangNode.RootContainer.BtContainer.Flags.LowBit | YangNode.RootContainer.BtContainer.Flags.HighBit,
            YangNode.ServerKind.AuthZ | YangNode.ServerKind.AuthN, YangNode.ServerKind.Accounting).YangValidate();
    }

    [Test]
    public async Task BitIsSetMustUsesFirstNodeOnly()
    {
        // RFC 7950 10.6.1: only the first node in document order is tested.
        var ex = await Assert.That(() => BtRoot(YangNode.RootContainer.BtContainer.Flags.HighBit, YangNode.ServerKind.AuthZ, YangNode.ServerKind.AuthN).YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/bt/auth-required");
        await Assert.That(ex.Message).Contains("first server must do authentication");
    }

    [Test]
    public async Task BitIsSetMustRejectsEmptyNodeSet()
    {
        await Assert.That(() => BtRoot(YangNode.RootContainer.BtContainer.Flags.HighBit).YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(() => BtRoot(YangNode.RootContainer.BtContainer.Flags.HighBit, (YangNode.ServerKind?)null).YangValidate()).ThrowsExactly<YangValidationException>();
    }

    [Test]
    public async Task BitIsSetWhenGatesOnInlineBits()
    {
        var ex = await Assert.That(() => BtRoot(YangNode.RootContainer.BtContainer.Flags.LowBit, YangNode.ServerKind.AuthN).YangValidate()).ThrowsExactly<YangValidationException>();
        await Assert.That(ex.SchemaPath).IsEqualTo("/tree-test/root/bt/high-only");
        await Assert.That(() => BtRoot(null, YangNode.ServerKind.AuthN).YangValidate()).ThrowsExactly<YangValidationException>();
    }
}
