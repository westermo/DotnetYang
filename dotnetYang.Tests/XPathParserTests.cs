using YangParser.SemanticModel.XPath;

namespace Compiler.Tests;

public class XPathParserTests
{
    [Test]
    [Arguments("true()")]
    [Arguments("false()")]
    [Arguments("1")]
    [Arguments("1.5")]
    [Arguments("'hello'")]
    [Arguments("\"hello\"")]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("foo")]
    [Arguments("foo/bar")]
    [Arguments("../foo")]
    [Arguments("/foo")]
    [Arguments("/foo/bar")]
    [Arguments("//foo")]
    [Arguments("pfx:foo")]
    [Arguments("foo[bar='baz']")]
    [Arguments("foo[bar = 'baz' and qux > 3]")]
    [Arguments("count(foo)")]
    [Arguments("count(../foo) > 0")]
    [Arguments("not(foo)")]
    [Arguments("not(../enabled = 'true')")]
    [Arguments("../config/enabled = 'true' or ../disabled = 'false'")]
    [Arguments("string-length(name) > 0")]
    [Arguments("(a or b) and c")]
    [Arguments("child::foo/parent::bar")]
    [Arguments("@id = 'x'")]
    [Arguments("foo | bar")]
    [Arguments("position() = last()")]
    public async Task ParsesWithoutError(string expr)
    {
        var ast = XPathParser.Parse(expr);
        await Assert.That(ast).IsNotNull();
    }

    [Test]
    public async Task NumberLiteralParses()
    {
        var ast = (NumberLiteralExpr)XPathParser.Parse("42");
        await Assert.That(ast.Value).IsEqualTo(42.0);
    }

    [Test]
    public async Task StringLiteralPreservesContents()
    {
        var ast = (StringLiteralExpr)XPathParser.Parse("'hi there'");
        await Assert.That(ast.Value).IsEqualTo("hi there");
    }

    [Test]
    public async Task AndBindsTighterThanOr()
    {
        // 'a or b and c' parses as 'a or (b and c)'.
        var ast = (BinaryExpr)XPathParser.Parse("a or b and c");
        await Assert.That(ast.Op).IsEqualTo(BinaryOp.Or);
        var right = (BinaryExpr)ast.Right;
        await Assert.That(right.Op).IsEqualTo(BinaryOp.And);
    }

    [Test]
    public async Task DoubleDotIsParentStep()
    {
        var path = (PathExpr)XPathParser.Parse("..");
        await Assert.That(path.Steps.Count).IsEqualTo(1);
        await Assert.That(path.Steps[0].Axis).IsEqualTo(XPathAxis.Parent);
    }

    [Test]
    public async Task AbsolutePathFlagged()
    {
        var path = (PathExpr)XPathParser.Parse("/foo/bar");
        await Assert.That(path.IsAbsolute).IsTrue();
        await Assert.That(path.Steps.Count).IsEqualTo(2);
    }

    [Test]
    public async Task PredicateIsParsedAsExpression()
    {
        var path = (PathExpr)XPathParser.Parse("foo[bar='baz']");
        await Assert.That(path.Steps.Count).IsEqualTo(1);
        await Assert.That(path.Steps[0].Predicates.Count).IsEqualTo(1);
        var pred = (BinaryExpr)path.Steps[0].Predicates[0];
        await Assert.That(pred.Op).IsEqualTo(BinaryOp.Eq);
    }

    [Test]
    public async Task FunctionCallWithMultipleArgs()
    {
        var ast = (FunctionCallExpr)XPathParser.Parse("substring('abc', 1, 2)");
        await Assert.That(ast.Name).IsEqualTo("substring");
        await Assert.That(ast.Arguments.Count).IsEqualTo(3);
    }

    [Test]
    public async Task DoubleSlashParsesAsDescendantOrSelf()
    {
        var path = (PathExpr)XPathParser.Parse("//foo");
        await Assert.That(path.IsAbsolute).IsTrue();
        // Parser inserts: descendant-or-self::node(), child::foo
        await Assert.That(path.Steps.Count).IsEqualTo(2);
        await Assert.That(path.Steps[0].Axis).IsEqualTo(XPathAxis.DescendantOrSelf);
        await Assert.That(path.Steps[1].Axis).IsEqualTo(XPathAxis.Child);
    }

    [Test]
    public async Task RelativeDoubleSlashParsesCorrectly()
    {
        var path = (PathExpr)XPathParser.Parse("a//b");
        await Assert.That(path.IsAbsolute).IsFalse();
        // Steps: child::a, descendant-or-self::node(), child::b
        await Assert.That(path.Steps.Count).IsEqualTo(3);
        await Assert.That(path.Steps[0].Axis).IsEqualTo(XPathAxis.Child);
        await Assert.That(path.Steps[1].Axis).IsEqualTo(XPathAxis.DescendantOrSelf);
        await Assert.That(path.Steps[2].Axis).IsEqualTo(XPathAxis.Child);
    }

    [Test]
    public async Task WildcardStepParsesAsNameTestStar()
    {
        var path = (PathExpr)XPathParser.Parse("foo/*");
        await Assert.That(path.Steps.Count).IsEqualTo(2);
        var star = path.Steps[1].Test as NameTest;
        await Assert.That(star).IsNotNull();
        await Assert.That(star!.LocalName).IsEqualTo("*");
    }

    [Test]
    public async Task CountStarParsesCorrectly()
    {
        var fc = (FunctionCallExpr)XPathParser.Parse("count(*)");
        await Assert.That(fc.Name).IsEqualTo("count");
        await Assert.That(fc.Arguments.Count).IsEqualTo(1);
        var arg = (PathExpr)fc.Arguments[0];
        await Assert.That(arg.Steps.Count).IsEqualTo(1);
        await Assert.That(((NameTest)arg.Steps[0].Test).LocalName).IsEqualTo("*");
    }
}
