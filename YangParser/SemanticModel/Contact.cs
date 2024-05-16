using System;
using System.Linq;

namespace YangParser.SemanticModel;

/// <summary>
/// The "contact" statement provides contact information for the module.
///The argument is a string that is used to specify contact information
///for the person or persons to whom technical queries concerning this
///module should be sent, such as their name, postal address, telephone
///number, and electronic mail address.
/// </summary>
public class Contact : Statement
{
    public Contact(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "contact";
}