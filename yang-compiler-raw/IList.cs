namespace Yang.Compiler;

public interface IList : IToken, IYangList, IMayBeConfiguration, IDescribable, ISupportsMusts,
    ISupportsFeatureFlags, ISupportsReference, ISupportsStatus, ISupportsWhen
{
    /// <summary>
    /// Space-seperated list of leaf identifiers of this list.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Space-seperated list of schema-node identifiers
    /// </summary>
    string Unique { get; }
}