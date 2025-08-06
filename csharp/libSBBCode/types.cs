using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace libSBBCode;

public interface ISBBElement;

public interface ISBBTagAttribute
{
    public string Name { get; }
    public object Value { get; }
}

public interface SBBTagAttribute<out TVal> : ISBBTagAttribute where TVal : notnull
{
    public new TVal Value { get; }

    object ISBBTagAttribute.Value => Value;
}

public record SBBTagStringAttribute(string Name, string Value) : SBBTagAttribute<string>;

public record SBBTagIntAttribute(string Name, int Value) : SBBTagAttribute<int>;

public record SBBTagFloatAttribute(string Name, double Value) : SBBTagAttribute<double>;

public record SBBTagBoolAttribute(string Name, bool Value) : SBBTagAttribute<bool>;

[DebuggerDisplay("SBBTag \\{ Name = {Name} \\}")]
public class SBBTag(string name, List<ISBBTagAttribute> attributes, List<ISBBElement> elements) : ISBBElement
{
    internal static readonly List<ISBBTagAttribute> EmptyAttr = [];
    internal static readonly List<ISBBElement> EmptyElements = [];

    public string Name { get; } = name;
    public List<ISBBTagAttribute> Attributes { get; } = attributes;
    public List<ISBBElement> Elements { get; } = elements;

    public override bool Equals(object? otherObj)
    {
        // ensure that empty collections not modified
        Debug.Assert(EmptyAttr.Count == 0);
        Debug.Assert(EmptyElements.Count == 0);

        if (otherObj is null) return false;
        if (ReferenceEquals(this, otherObj)) return true;
        if (otherObj is not SBBTag other) return false;

        return Name == other.Name
               && (Attributes == other.Attributes || Attributes.SequenceEqual(other.Attributes))
               && (Elements == other.Elements || Elements.SequenceEqual(other.Elements));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Attributes, Elements);
    }
}

public record SBBContent(string Value) : ISBBElement;

public record AllowedTagAttribute(
    string Name,
    bool Required,
    ISet<Type> ValueTypes
);

public record AllowedTag(
    string Name,
    IEnumerable<AllowedTagAttribute> Attributes,
    bool ExtraAttributes
);