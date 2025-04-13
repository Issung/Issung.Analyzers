using System;

namespace Issung.Analyzers
{
    [RestrictedTo(typeof(Attribute))]   // The RestrictedToAttribute can only be applied to classes that extend Attribute.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RestrictedToAttribute(params Type[] allowedTypes) : Attribute
    {
        public Type[] AllowedTypes { get; } = allowedTypes;
    }
}
