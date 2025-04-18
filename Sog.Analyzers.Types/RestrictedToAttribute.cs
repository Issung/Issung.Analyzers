using System;

namespace Sog.Analyzers;

/// <summary>
/// Apply this attribute to an attribute to specify a list of types it may be applied to.
/// Inheritance is allowed, for example if you wish for someone to use your attibute on any collection type of ints, you can use
/// <code>
/// [RestrictedTo(typeof(IEnumerable&lt;int&gt;))]
/// </code>
/// </summary>
[RestrictedTo(typeof(Attribute))]   // The RestrictedToAttribute can only be applied to classes that extend Attribute.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RestrictedToAttribute(params Type[] allowedTypes) : Attribute
{
    public Type[] AllowedTypes { get; } = allowedTypes;
}
