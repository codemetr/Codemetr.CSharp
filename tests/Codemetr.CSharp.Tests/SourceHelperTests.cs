namespace Codemetr.CSharp.Tests;

public sealed class SourceHelperTests
{
    [Fact]
    public void RootMemberSelectionDoesNotMistakeNestedLocalForAnOverload()
    {
        var member = Source.Member("class A { void M() { void Local() { if (true) { } } } }", "A", "M");
        Assert.Equal("M()", member.Name);
        Assert.Equal(1, member.CyclomaticComplexity);
    }

    [Fact]
    public void ActualOverloadsStillRequireAnUnambiguousSelection()
    {
        Assert.Throws<InvalidOperationException>(() => Source.Member("class A { void M() { } void M(int value) { } }", "A", "M"));
    }
}
