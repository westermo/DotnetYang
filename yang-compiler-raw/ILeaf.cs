namespace Yang.Compiler;

public interface ILeaf : IToken, ISupportsUnitSpecification, IMayBeConfiguration,
    ISupportsDefaultValue, IDescribable, ITyped,
    ISupportsFeatureFlags, ISupportsMandatory, ISupportsMusts,
    ISupportsReference, ISupportsStatus, ISupportsWhen
{
}