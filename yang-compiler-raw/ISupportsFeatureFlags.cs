using System.Collections.Generic;

namespace Yang.Compiler;

public interface ISupportsFeatureFlags
{
    IEnumerable<IFeatureFlag>? FeatureFlags { get; }
}