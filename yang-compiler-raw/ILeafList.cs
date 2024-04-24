namespace Yang.Compiler;

public interface ILeafList : IToken, IYangList, IMayBeConfiguration, IDescribable,
    ISupportsUnitSpecification, ITyped, ISupportsMusts, ISupportsFeatureFlags, ISupportsReference, ISupportsStatus,
    ISupportsWhen;