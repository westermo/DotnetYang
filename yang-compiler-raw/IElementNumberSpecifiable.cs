namespace Yang.Compiler;

public interface IElementNumberSpecifiable
{
    /// <summary>
    /// Null refers to the default value of 0
    /// </summary>
    uint? MinElements { get; }

    /// <summary>
    /// Null refers to the default value of unbounded
    /// </summary>
    uint? MaxElements { get; }
}