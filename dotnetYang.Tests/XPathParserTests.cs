using YangParser.SemanticModel.XPath;

namespace Compiler.Tests;

public class XPathParserTests
{
    [Theory]
    [InlineData("true()")]
    [InlineData("false()")]
    [InlineData("1")]
    [InlineData("1.5")]
    [InlineData("'hello'")]
    [InlineData("\"hello\"")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("foo")]
    [InlineData("foo/bar")]
    [InlineData("../foo")]
    [InlineData("/foo")]
    [InlineData("/foo/bar")]
    [InlineData("//foo")]
    [InlineData("pfx:foo")]
    [InlineData("foo[bar='baz']")]
    [InlineData("foo[bar = 'baz' and qux > 3]")]
    [InlineData("count(foo)")]
    [InlineData("count(../foo) > 0")]
    [InlineData("not(foo)")]
    [InlineData("not(../enabled = 'true')")]
    [InlineData("../config/enabled = 'true' or ../disabled = 'false'")]
    [InlineData("string-length(name) > 0")]
    [InlineData("(a or b) and c")]
    [InlineData("child::foo/parent::bar")]
    [InlineData("@id = 'x'")]
    [InlineData("foo | bar")]
    [InlineData("position() = last()")]
    public void ParsesWithoutError(string expr)
    {
        var ast = XPathParser.Parse(expr);
        Assert.NotNull(ast);
    }

    [Fact]
    public void NumberLiteralParses()
    {
        var ast = (NumberLiteralExpr)XPathParser.Parse("42");
        Assert.Equal(42.0, ast.Value);
    }

    [Fact]
    public void StringLiteralPreservesContents()
    {
        var ast = (StringLiteralExpr)XPathParser.Parse("'hi there'");
        Assert.Equal("hi there", ast.Value);
    }

    [Fact]
    public void AndBindsTighterThanOr()
    {
        // 'a or b and c' parses as 'a or (b and c)'.
        var ast = (BinaryExpr)XPathParser.Parse("a or b and c");
        Assert.Equal(BinaryOp.Or, ast.Op);
        var right = (BinaryExpr)ast.Right;
        Assert.Equal(BinaryOp.And, right.Op);
    }

    [Fact]
    public void DoubleDotIsParentStep()
    {
        var path = (PathExpr)XPathParser.Parse("..");
        Assert.Single(path.Steps);
        Assert.Equal(XPathAxis.Parent, path.Steps[0].Axis);
    }

    [Fact]
    public void AbsolutePathFlagged()
    {
        var path = (PathExpr)XPathParser.Parse("/foo/bar");
        Assert.True(path.IsAbsolute);
        Assert.Equal(2, path.Steps.Count);
    }

    [Fact]
    public void PredicateIsParsedAsExpression()
    {
        var path = (PathExpr)XPathParser.Parse("foo[bar='baz']");
        Assert.Single(path.Steps);
        Assert.Single(path.Steps[0].Predicates);
        var pred = (BinaryExpr)path.Steps[0].Predicates[0];
        Assert.Equal(BinaryOp.Eq, pred.Op);
    }

    [Fact]
    public void FunctionCallWithMultipleArgs()
    {
        var ast = (FunctionCallExpr)XPathParser.Parse("substring('abc', 1, 2)");
        Assert.Equal("substring", ast.Name);
        Assert.Equal(3, ast.Arguments.Count);
    }

    [Fact]
    public void DoubleSlashParsesAsDescendantOrSelf()
    {
        var path = (PathExpr)XPathParser.Parse("//foo");
        Assert.True(path.IsAbsolute);
        // Parser inserts: descendant-or-self::node(), child::foo
        Assert.Equal(2, path.Steps.Count);
        Assert.Equal(XPathAxis.DescendantOrSelf, path.Steps[0].Axis);
        Assert.Equal(XPathAxis.Child, path.Steps[1].Axis);
    }

    [Fact]
    public void RelativeDoubleSlashParsesCorrectly()
    {
        var path = (PathExpr)XPathParser.Parse("a//b");
        Assert.False(path.IsAbsolute);
        // Steps: child::a, descendant-or-self::node(), child::b
        Assert.Equal(3, path.Steps.Count);
        Assert.Equal(XPathAxis.Child, path.Steps[0].Axis);
        Assert.Equal(XPathAxis.DescendantOrSelf, path.Steps[1].Axis);
        Assert.Equal(XPathAxis.Child, path.Steps[2].Axis);
    }

    [Fact]
    public void WildcardStepParsesAsNameTestStar()
    {
        var path = (PathExpr)XPathParser.Parse("foo/*");
        Assert.Equal(2, path.Steps.Count);
        var star = path.Steps[1].Test as NameTest;
        Assert.NotNull(star);
        Assert.Equal("*", star!.LocalName);
    }

    [Fact]
    public void CountStarParsesCorrectly()
    {
        var fc = (FunctionCallExpr)XPathParser.Parse("count(*)");
        Assert.Equal("count", fc.Name);
        Assert.Single(fc.Arguments);
        var arg = (PathExpr)fc.Arguments[0];
        Assert.Single(arg.Steps);
        Assert.Equal("*", ((NameTest)arg.Steps[0].Test).LocalName);
    }
}
