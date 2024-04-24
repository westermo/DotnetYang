namespace Yang.Compiler;

public interface ITypeDefinition : IType, ISupportsUnitSpecification, IDescribable, ITyped, ISupportsDefaultValue,
    ISupportsReference, ISupportsStatus
{
}