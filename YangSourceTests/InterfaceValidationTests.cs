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
}
