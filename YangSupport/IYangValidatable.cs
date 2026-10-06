namespace YangSupport;

/// <summary>
/// Implemented by generated YANG node classes that carry runtime constraint checks
/// (must/when, min/max-elements, unique, mandatory, instance-identifier require-instance).
/// </summary>
public interface IYangValidatable
{
    /// <summary>
    /// Validates this node and all of its descendants.
    /// Throws <see cref="YangValidationException"/> for a single violation or
    /// <see cref="YangValidationAggregateException"/> when several constraints fail.
    /// </summary>
    void YangValidate();
}

/// <summary>
/// Implemented by generated root classes (e.g. <c>Configuration</c>) that can resolve
/// instance-identifier paths against the whole data tree.
/// </summary>
public interface IYangInstanceIdentifierRoot
{
    /// <summary>
    /// Resolves an instance-identifier path (e.g. <c>/module:container/list[key='value']/leaf</c>)
    /// to the target object, or returns null if it does not exist.
    /// </summary>
    object? ResolveInstanceIdentifier(string path);
}
