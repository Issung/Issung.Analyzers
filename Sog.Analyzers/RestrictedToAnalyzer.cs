using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Sog.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RestrictedToAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "SOG001",
        title: "Attribute applied to disallowed type",
        messageFormat: "Attribute '{0}' cannot be applied to type '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Property);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        // This analyzer runs on class definitions/references.
        if (context.Symbol is not ITypeSymbol)
        {
            return;
        }

        var symbol = context.Symbol;

        foreach (var attr in symbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass == null)
            {
                continue;
            }

            // Look for [RestrictedTo] on the attribute definition
            var restriction = attrClass
                .GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "RestrictedToAttribute");

            if (restriction == null)
            {
                continue;
            }

            var allowedTypes = restriction.ConstructorArguments.FirstOrDefault();
            if (allowedTypes.Values is not { Length: > 0 }) continue;

            var allowed = allowedTypes.Values.Select(v => v.Value as INamedTypeSymbol).OfType<ISymbol>().ToImmutableHashSet(SymbolEqualityComparer.Default);

            // Check if current symbol's type matches
            var targetType = symbol switch
            {
                INamedTypeSymbol typeSym => typeSym,
                IPropertySymbol propSym => propSym.ContainingType,
                IFieldSymbol fieldSym => fieldSym.ContainingType,
                _ => null
            };

            if (targetType == null || !allowed.Any(a => IsCompatible(targetType, a)))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    attr.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation(),
                    attrClass.Name,
                    targetType?.Name ?? "unknown");

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsCompatible(ITypeSymbol target, ISymbol allowedType)
    {
        return SymbolEqualityComparer.Default.Equals(target, allowedType) ||
               target.AllInterfaces.Contains(allowedType, SymbolEqualityComparer.Default) ||
               InheritsFrom(target, allowedType);
    }

    private static bool InheritsFrom(ITypeSymbol type, ISymbol baseType)
    {
        while (type?.BaseType != null)
        {
            if (SymbolEqualityComparer.Default.Equals(type.BaseType, baseType))
                return true;

            type = type.BaseType;
        }
        return false;
    }
}
