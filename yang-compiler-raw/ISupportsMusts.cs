using System.Collections.Generic;

namespace Yang.Compiler;

public interface ISupportsMusts
{
    IEnumerable<IMust>? Must { get; }
}