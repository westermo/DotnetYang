using System.Collections.Generic;

namespace Yang.Compiler;

public interface IContainer : IToken
{
    IEnumerable<IToken> Children { get; }
}