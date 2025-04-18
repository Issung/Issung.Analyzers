using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Sog.Analyzers.Test;

[TestClass]
public class RestrictedToAnalyzerTests
{
    [TestMethod]
    public async Task Attribute_UsedOnAllowedType_NoDiagnostic()
    {
        var code = /* lang=c#-test */ """
            using System;
            using Sog.Analyzers;

            namespace Example
            {
                [RestrictedTo(typeof(MyClass))]
                public class MyAttribute : Attribute { }

                [MyAttribute]
                public class MyClass { }
            }
            """;

        var test = new CSharpAnalyzerTest<RestrictedToAnalyzer, DefaultVerifier>
        {
            TestCode = code,
        };

        test.TestState.AdditionalReferences.Add(
            MetadataReference.CreateFromFile(typeof(RestrictedToAttribute).Assembly.Location)
        );

        await test.RunAsync();

        //await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [TestMethod]
    public async Task Attribute_UsedOnDisallowedType_TriggersDiagnostic()
    {
        var code = /* lang=c#-test */ """
            using System;
            using Sog.Analyzers;

            namespace Example
            {
                [RestrictedTo(typeof(MyClass))]
                public class MyAttribute : Attribute { }

                public class MyClass { }

                [{|#0:MyAttribute|}]
                public class NotAllowedClass { }
            }
            """;

        var test = new CSharpAnalyzerTest<RestrictedToAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ExpectedDiagnostics = {
                DiagnosticResult.CompilerError("RTA001")
                    .WithLocation(0)
                    .WithArguments("MyAttribute", "NotAllowedClass")
            },
            TestState = {
                AdditionalReferences = {
                    MetadataReference.CreateFromFile(typeof(RestrictedToAttribute).Assembly.Location)
                }
            }
        };

        await test.RunAsync();
    }

    [TestMethod]
    public async Task Attribute_WithMultipleAllowedTypes_OnlyAllowsSpecified()
    {
        var code = /* lang=c#-test */ """
            using System;
            using Sog.Analyzers;

            namespace Example
            {
                [RestrictedTo(typeof(MyClass), typeof(AnotherAllowedClass))]
                public class MyAttribute : Attribute { }

                [MyAttribute]
                public class MyClass { }

                [MyAttribute]
                public class AnotherAllowedClass { }

                [{|#0:MyAttribute|}]
                public class BadClass { }
            }
            """;

        var test = new CSharpAnalyzerTest<RestrictedToAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ExpectedDiagnostics = {
                DiagnosticResult.CompilerError("RTA001")
                    .WithLocation(0)
                    .WithArguments("MyAttribute", "BadClass")
            },
            TestState = {
                AdditionalReferences = {
                    MetadataReference.CreateFromFile(typeof(RestrictedToAttribute).Assembly.Location)
                }
            }
        };

        //test.TestState.AdditionalReferences.Add(
        //    MetadataReference.CreateFromFile(typeof(RestrictedToAttribute).Assembly.Location)
        //);

        await test.RunAsync();

        //var expected = VerifyCS.Diagnostic("RTA001")
        //    .WithLocation(0)
        //    .WithArguments("MyAttribute", "BadClass");
        //
        //await VerifyCS.VerifyAnalyzerAsync(code, expected);
    }

    [TestMethod]
    public async Task Attribute_WithoutRestrictedTo_NoDiagnostic()
    {
        var code = /* lang=c#-test */ """
            using System;

            namespace Example
            {
                public class MyAttribute : Attribute { }

                [MyAttribute]
                public class WhateverClass { }
            }
            """;

        var test = new CSharpAnalyzerTest<RestrictedToAnalyzer, DefaultVerifier>
        {
            TestCode = code,
        };

        test.TestState.AdditionalReferences.Add(
            MetadataReference.CreateFromFile(typeof(RestrictedToAttribute).Assembly.Location)
        );

        await test.RunAsync();

        //await VerifyCS.VerifyAnalyzerAsync(code);
    }
}
