using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using CustomRoslynAnalyzer.Rules;
using VerifyCS = CustomRoslynAnalyzer.Tests.Helpers.CSharpAnalyzerVerifier<CustomRoslynAnalyzer.Rules.NestedInfrastructureLoopRule>;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace CustomRoslynAnalyzer.Tests
{

    public sealed class NestedInfrastructureLoopRuleTests
    {
        [Fact]
        public async Task ReportsInfrastructureCallInsideLoop()
        {
            const string testCode = @"
namespace Demo
{
    public class Processor
    {
        private readonly Sample.Infrastructure.Repository _repository = new();

        public void Process()
        {
            for (var i = 0; i < 3; i++)
            {
                _repository.{|#0:Save|}();
            }
        }
    }
}
namespace Sample.Infrastructure
{
    public sealed class Repository
    {
        public void Save()
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic(NestedInfrastructureLoopRule.DefaultDescriptor)
                .WithLocation(0);

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task DoesNotReportInfrastructureCallOutsideLoop()
        {
            const string testCode = @"
namespace Demo
{
    public class Processor
    {
        private readonly Sample.Infrastructure.Repository _repository = new();

        public void Process()
        {
            var snapshots = _repository.GetAll();
            foreach (var item in snapshots)
            {
                _ = item;
            }
        }
    }
}

namespace Sample.Infrastructure
{
    public sealed class Repository
    {
        public int[] GetAll() => new int[0];
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ReportsInfrastructureCallInsideWhileLoop()
        {
            const string testCode = @"
namespace Demo
{
    public class Processor
    {
        private readonly Sample.Infrastructure.Repository _repository = new();

        public void Process()
        {
            var i = 0;
            while (i < 3)
            {
                _repository.{|#0:Save|}();
                i++;
            }
        }
    }
}

namespace Sample.Infrastructure
{
    public sealed class Repository
    {
        public void Save()
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic(NestedInfrastructureLoopRule.DefaultDescriptor)
                .WithLocation(0);

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ReportsInfrastructureCallInsideDoLoop()
        {
            const string testCode = @"
namespace Demo
{
    public class Processor
    {
        private readonly Sample.Infrastructure.Repository _repository = new();

        public void Process()
        {
            var i = 0;
            do
            {
                _repository.{|#0:Save|}();
                i++;
            } while (i < 3);
        }
    }
}

namespace Sample.Infrastructure
{
    public sealed class Repository
    {
        public void Save()
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic(NestedInfrastructureLoopRule.DefaultDescriptor)
                .WithLocation(0);

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ReportsInfrastructureObjectCreationInsideLoop()
        {
            const string testCode = @"
namespace Demo
{
    public class Processor
    {
        public void Process()
        {
            for (var i = 0; i < 3; i++)
            {
                var repo = {|#0:new Sample.Infrastructure.Repository()|};
            }
        }
    }
}

namespace Sample.Infrastructure
{
    public sealed class Repository
    {
    }
}";

            var expected = VerifyCS.Diagnostic(NestedInfrastructureLoopRule.DefaultDescriptor)
                .WithLocation(0);

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ReportsInfrastructureCallInsideNestedLoops()
        {
            const string testCode = @"
namespace Demo
{
    public class Processor
    {
        private readonly Sample.Infrastructure.Repository _repository = new();

        public void Process()
        {
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    _repository.{|#0:Save|}();
                }
            }
        }
    }
}

namespace Sample.Infrastructure
{
    public sealed class Repository
    {
        public void Save()
        {
        }
    }
}";
            var expected = VerifyCS.Diagnostic(NestedInfrastructureLoopRule.DefaultDescriptor)
                .WithLocation(0);

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ReportsInfrastructureCallInsideForeachLoop()
        {
            const string testCode = @"
using System.Collections.Generic;

namespace Demo
{
    public class Processor
    {
        private readonly Sample.Infrastructure.Repository _repository = new();

        public void Process()
        {
            var items = new List<int> { 1, 2, 3 };
            foreach (var item in items)
            {
                _repository.{|#0:Save|}();
            }
        }
    }
}

namespace Sample.Infrastructure
{
    public sealed class Repository
    {
        public void Save()
        {
        }
    }
}";
            var expected = VerifyCS.Diagnostic(NestedInfrastructureLoopRule.DefaultDescriptor)
                .WithLocation(0);

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ReportsStaticInfrastructureMethodCallInsideLoop()
        {
            const string testCode = @"
namespace Demo
{
    public class Processor
    {
        public void Process()
        {
            for (var i = 0; i < 3; i++)
            {
                Sample.Infrastructure.Repository.{|#0:SaveStatic|}();
            }
        }
    }
}

namespace Sample.Infrastructure
{
    public sealed class Repository
    {
        public static void SaveStatic()
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic(NestedInfrastructureLoopRule.DefaultDescriptor)
                .WithLocation(0);

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ReportsIndirectInfrastructureCallInsideLoop()
        {
            const string testCode = @"
namespace Demo
{
    public class Processor
    {
        private readonly Sample.Infrastructure.Repository _repository = new();

        public void Process()
        {
            for (var i = 0; i < 3; i++)
            {
                {|#0:HelperMethod()|};
            }
        }

        private void HelperMethod()
        {
            _repository.Save();
        }
    }
}

namespace Sample.Infrastructure
{
    public sealed class Repository
    {
        public void Save()
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic(NestedInfrastructureLoopRule.DefaultDescriptor)
                .WithLocation(0);

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public void ExtractBodyNode_ReturnsBody_ForMethodDeclaration()
        {
            const string code = @"
class C
{
    void M()
    {
        int x = 1;
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var method = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();
            var extractMethod = typeof(NestedInfrastructureLoopRule).GetMethod("ExtractBodyNode", BindingFlags.NonPublic | BindingFlags.Static);
            var result = extractMethod?.Invoke(null, new object[] { method });
            var body = result as SyntaxNode;
            Assert.NotNull(body);
            Assert.IsType<Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax>(body);
        }

        [Fact]
        public void ExtractBodyNode_ReturnsExpressionBody_ForExpressionMethod()
        {
            const string code = @"
class C
{
    int M() => 1;
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var method = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();
            var extractMethod = typeof(NestedInfrastructureLoopRule).GetMethod("ExtractBodyNode", BindingFlags.NonPublic | BindingFlags.Static);
            var result = extractMethod?.Invoke(null, new object[] { method });
            var body = result as SyntaxNode;
            Assert.NotNull(body);
            Assert.IsType<Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax>(body);
        }

        [Fact]
        public void ExtractBodyNode_ReturnsBody_ForAccessor()
        {
            const string code = @"
class C
{
    int P { get { return 1; } }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var accessor = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AccessorDeclarationSyntax>().First();
            var extractMethod = typeof(NestedInfrastructureLoopRule).GetMethod("ExtractBodyNode", BindingFlags.NonPublic | BindingFlags.Static);
            var result = extractMethod?.Invoke(null, new object[] { accessor });
            var body = result as SyntaxNode;
            Assert.NotNull(body);
            Assert.IsType<Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax>(body);
        }

        [Fact]
        public void ExtractBodyNode_ReturnsExpressionBody_ForExpressionAccessor()
        {
            const string code = @"
class C
{
    int P { get => 1; }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var accessor = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AccessorDeclarationSyntax>().First();
            var extractMethod = typeof(NestedInfrastructureLoopRule).GetMethod("ExtractBodyNode", BindingFlags.NonPublic | BindingFlags.Static);
            var result = extractMethod?.Invoke(null, new object[] { accessor });
            var body = result as SyntaxNode;
            Assert.NotNull(body);
            Assert.IsType<Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax>(body);
        }

        [Fact]
        public void ExtractBodyNode_ReturnsBody_ForLocalFunction()
        {
            const string code = @"
class C
{
    void M()
    {
        void Local()
        {
            int x = 1;
        }
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var localFunction = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LocalFunctionStatementSyntax>().First();
            var extractMethod = typeof(NestedInfrastructureLoopRule).GetMethod("ExtractBodyNode", BindingFlags.NonPublic | BindingFlags.Static);
            var result = extractMethod?.Invoke(null, new object[] { localFunction });
            var body = result as SyntaxNode;
            Assert.NotNull(body);
            Assert.IsType<Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax>(body);
        }

        [Fact]
        public void ExtractBodyNode_ReturnsExpressionBody_ForExpressionLocalFunction()
        {
            const string code = @"
class C
{
    void M()
    {
        int Local() => 1;
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var localFunction = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LocalFunctionStatementSyntax>().First();
            var extractMethod = typeof(NestedInfrastructureLoopRule).GetMethod("ExtractBodyNode", BindingFlags.NonPublic | BindingFlags.Static);
            var result = extractMethod?.Invoke(null, new object[] { localFunction });
            var body = result as SyntaxNode;
            Assert.NotNull(body);
            Assert.IsType<Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax>(body);
        }

        [Fact]
        public void ContainsInfrastructureUsage_ReturnsTrue_WhenInvocationIsInfrastructure()
        {
            const string code = @"
using Sample.Infrastructure;

class C
{
    void M()
    {
        Repository.SaveStatic();
    }
}

namespace Sample.Infrastructure
{
    public class Repository
    {
        public static void SaveStatic() { }
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var body = method.Body;

            var containsMethod = typeof(NestedInfrastructureLoopRule).GetMethod("ContainsInfrastructureUsage", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool?)containsMethod.Invoke(null, new object?[] { body, semanticModel });

            Assert.True(result);
        }

        [Fact]
        public void ContainsInfrastructureUsage_ReturnsTrue_WhenCreationIsInfrastructure()
        {
            const string code = @"
using Sample.Infrastructure;

class C
{
    void M()
    {
        var repo = new Repository();
    }
}

namespace Sample.Infrastructure
{
    public class Repository
    {
        public void Save() { }
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var body = method.Body;

            var containsMethod = typeof(NestedInfrastructureLoopRule).GetMethod("ContainsInfrastructureUsage", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool?)containsMethod.Invoke(null, new object?[] { body, semanticModel });

            Assert.True(result);
        }

        [Fact]
        public void ContainsInfrastructureUsage_ReturnsFalse_WhenInvocationIsNotInfrastructure()
        {
            const string code = @"
class C
{
    void M()
    {
        Console.WriteLine();
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location), Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Console).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var body = method.Body;

            var containsMethod = typeof(NestedInfrastructureLoopRule).GetMethod("ContainsInfrastructureUsage", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool?)containsMethod.Invoke(null, new object?[] { body, semanticModel });

            Assert.False(result);
        }

        [Fact]
        public void IsInfrastructureSymbol_ReturnsFalse_WhenSymbolIsNull()
        {
            var method = typeof(NestedInfrastructureLoopRule).GetMethod("IsInfrastructureSymbol", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool)method.Invoke(null, new object[] { null! })!;

            Assert.False(result);
        }

        [Fact]
        public void IsInfrastructureSymbol_ReturnsTrue_WhenTypeSymbolIsInfrastructure()
        {
            const string code = @"
namespace Sample.Infrastructure
{
    public class Repository { }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var classDecl = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().First();
            var symbol = semanticModel.GetDeclaredSymbol(classDecl) as Microsoft.CodeAnalysis.ITypeSymbol;

            var method = typeof(NestedInfrastructureLoopRule).GetMethod("IsInfrastructureSymbol", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool?)method.Invoke(null, new object?[] { symbol });

            Assert.True(result);
        }

        [Fact]
        public void IsInfrastructureSymbol_ReturnsFalse_WhenTypeSymbolIsNotInfrastructure()
        {
            const string code = @"
namespace Sample
{
    public class Repository { }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var classDecl = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().First();
            var symbol = semanticModel.GetDeclaredSymbol(classDecl) as Microsoft.CodeAnalysis.ITypeSymbol;

            var method = typeof(NestedInfrastructureLoopRule).GetMethod("IsInfrastructureSymbol", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool?)method.Invoke(null, new object?[] { symbol });

            Assert.False(result);
        }

        [Fact]
        public void IsInfrastructureSymbol_ReturnsTrue_WhenMethodSymbolIsInfrastructure()
        {
            const string code = @"
namespace Sample.Infrastructure
{
    public class Repository
    {
        public void Save() { }
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();
            var symbol = semanticModel.GetDeclaredSymbol(methodDecl) as Microsoft.CodeAnalysis.IMethodSymbol;

            var method = typeof(NestedInfrastructureLoopRule).GetMethod("IsInfrastructureSymbol", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool?)method.Invoke(null, new object?[] { symbol });

            Assert.True(result);
        }

        [Fact]
        public void IsInfrastructureSymbol_ReturnsTrue_WhenPropertySymbolIsInfrastructure()
        {
            const string code = @"
namespace Sample.Infrastructure
{
    public class Repository
    {
        public int Count { get; set; }
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var propertyDecl = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>().First();
            var symbol = semanticModel.GetDeclaredSymbol(propertyDecl) as Microsoft.CodeAnalysis.IPropertySymbol;

            var method = typeof(NestedInfrastructureLoopRule).GetMethod("IsInfrastructureSymbol", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool?)method.Invoke(null, new object?[] { symbol });

            Assert.True(result);
        }

        [Fact]
        public void IsInfrastructureSymbol_ReturnsTrue_WhenFieldSymbolIsInfrastructure()
        {
            const string code = @"
namespace Sample.Infrastructure
{
    public class Repository
    {
        public int count;
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var fieldDecl = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax>().First();
            var variable = fieldDecl.Declaration.Variables.First();
            var symbol = semanticModel.GetDeclaredSymbol(variable) as Microsoft.CodeAnalysis.IFieldSymbol;

            var method = typeof(NestedInfrastructureLoopRule).GetMethod("IsInfrastructureSymbol", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool?)method.Invoke(null, new object?[] { symbol });

            Assert.True(result);
        }

        [Fact]
        public void IsInfrastructureSymbol_ReturnsTrue_WhenEventSymbolIsInfrastructure()
        {
            const string code = @"
using System;
namespace Sample.Infrastructure
{
    public class Repository
    {
        public event Action MyEvent;
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location), Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Action).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var eventDecl = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.EventFieldDeclarationSyntax>().First();
            var variable = eventDecl.Declaration.Variables.First();
            var symbol = semanticModel.GetDeclaredSymbol(variable) as Microsoft.CodeAnalysis.IEventSymbol;

            var method = typeof(NestedInfrastructureLoopRule).GetMethod("IsInfrastructureSymbol", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool?)method.Invoke(null, new object?[] { symbol });

            Assert.True(result);
        }

        [Fact]
        public void IsInfrastructureSymbol_ReturnsTrue_WhenParameterSymbolIsInfrastructure()
        {
            const string code = @"
namespace Sample
{
    using Sample.Infrastructure;
    public class C
    {
        public void M(Repository r) { }
    }
}
namespace Sample.Infrastructure
{
    public class Repository { }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();
            var param = methodDecl.ParameterList.Parameters.First();
            var symbol = semanticModel.GetDeclaredSymbol(param) as Microsoft.CodeAnalysis.IParameterSymbol;

            var method = typeof(NestedInfrastructureLoopRule).GetMethod("IsInfrastructureSymbol", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool?)method.Invoke(null, new object?[] { symbol });

            Assert.True(result);
        }

        [Fact]
        public void IsInfrastructureSymbol_ReturnsTrue_WhenLocalSymbolIsInfrastructure()
        {
            const string code = @"
namespace Sample
{
    public class C
    {
        public void M()
        {
            Sample.Infrastructure.Repository r = null;
        }
    }
}
namespace Sample.Infrastructure
{
    public class Repository { }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();
            var body = methodDecl.Body;
            Assert.NotNull(body);
            var localDecl = body.Statements.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LocalDeclarationStatementSyntax>().First();
            var variable = localDecl.Declaration.Variables.First();
            var symbol = semanticModel.GetDeclaredSymbol(variable) as Microsoft.CodeAnalysis.ILocalSymbol;

            var method = typeof(NestedInfrastructureLoopRule).GetMethod("IsInfrastructureSymbol", BindingFlags.NonPublic | BindingFlags.Static)!;
            var result = (bool?)method.Invoke(null, new object?[] { symbol });

            Assert.True(result);
        }

        [Fact]
        public void TryFindInfrastructureUsage_ReturnsTrue_WhenInvocationIsInfrastructure()
        {
            const string code = @"
using Sample.Infrastructure;

class C
{
    void M()
    {
        Repository.SaveStatic();
    }
}

namespace Sample.Infrastructure
{
    public class Repository
    {
        public static void SaveStatic() { }
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var loopBody = methodDecl.Body;

            var tryFindMethod = typeof(NestedInfrastructureLoopRule).GetMethod("TryFindInfrastructureUsage", BindingFlags.NonPublic | BindingFlags.Static)!;
            var args = new object?[] { loopBody, semanticModel, null, null };
            var invocationResult = tryFindMethod.Invoke(null, args);
            var result = invocationResult is bool boolResult && boolResult;
            var offendingNode = (SyntaxNode?)args[2];
            var infrastructureSymbol = (ISymbol?)args[3];

            Assert.True(result);
            Assert.IsType<InvocationExpressionSyntax>(offendingNode);
            Assert.NotNull(infrastructureSymbol);
        }

        [Fact]
        public void TryFindInfrastructureUsage_ReturnsTrue_WhenCreationIsInfrastructure()
        {
            const string code = @"
using Sample.Infrastructure;

class C
{
    void M()
    {
        var repo = new Repository();
    }
}

namespace Sample.Infrastructure
{
    public class Repository
    {
        public void Save() { }
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var loopBody = methodDecl.Body;

            var tryFindMethod = typeof(NestedInfrastructureLoopRule).GetMethod("TryFindInfrastructureUsage", BindingFlags.NonPublic | BindingFlags.Static)!;
            var args = new object?[] { loopBody, semanticModel, null, null };
            var result = (bool?)tryFindMethod.Invoke(null, args);
            var offendingNode = (SyntaxNode?)args[2];
            var infrastructureSymbol = (ISymbol?)args[3];

            Assert.True(result);
            Assert.IsType<ObjectCreationExpressionSyntax>(offendingNode);
            Assert.NotNull(infrastructureSymbol);
        }

        [Fact]
        public void TryFindInfrastructureUsage_ReturnsTrue_WhenIndirectInvocationIsInfrastructure()
        {
            const string code = @"
using Sample.Infrastructure;

class C
{
    void M()
    {
        Helper();
    }

    void Helper()
    {
        Repository.SaveStatic();
    }
}

namespace Sample.Infrastructure
{
    public class Repository
    {
        public static void SaveStatic() { }
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var loopBody = methodDecl.Body;

            var tryFindMethod = typeof(NestedInfrastructureLoopRule).GetMethod("TryFindInfrastructureUsage", BindingFlags.NonPublic | BindingFlags.Static)!;
            var args = new object?[] { loopBody, semanticModel, null, null };
            var invocationResult = tryFindMethod.Invoke(null, args);
            var result = invocationResult is bool invocationBool && invocationBool;
            var offendingNode = (SyntaxNode?)args[2];
            var infrastructureSymbol = (ISymbol?)args[3];

            Assert.True(result);
            Assert.IsType<InvocationExpressionSyntax>(offendingNode);
            Assert.NotNull(infrastructureSymbol);
        }

        [Fact]
        public void TryFindInfrastructureUsage_ReturnsFalse_WhenNoInfrastructureUsage()
        {
            const string code = @"
class C
{
    void M()
    {
        Console.WriteLine();
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location), Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Console).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var methodDecl = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var loopBody = methodDecl.Body;

            var tryFindMethod = typeof(NestedInfrastructureLoopRule).GetMethod("TryFindInfrastructureUsage", BindingFlags.NonPublic | BindingFlags.Static)!;
            var args = new object?[] { loopBody, semanticModel, null, null };
            var result = (bool)tryFindMethod.Invoke(null, args)!;
            var offendingNode = (SyntaxNode?)args[2]!;
            var infrastructureSymbol = (ISymbol?)args[3]!;

            Assert.False(result);
            Assert.Null(offendingNode);
            Assert.Null(infrastructureSymbol);
        }

        [Fact]
        public void FindInfrastructureMethod_ReturnsCandidateMethod_WhenPrimaryIsNotInfrastructure()
        {
            const string code = @"
class C
{
    void Helper(object o) { }
    void Helper(string s) { Sample.Infrastructure.Repository.SaveStatic(); }

    void M()
    {
        Helper(null);
    }
}

namespace Sample.Infrastructure
{
    public class Repository
    {
        public static void SaveStatic() { }
    }
}";
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test", new[] { tree }, new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
            var semanticModel = compilation.GetSemanticModel(tree);
            var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Last();
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);

            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

            var findMethod = typeof(NestedInfrastructureLoopRule).GetMethod(
                "FindInfrastructureMethod",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(SymbolInfo), typeof(Compilation) },
                modifiers: null)!;
            var result = (IMethodSymbol?)findMethod.Invoke(null, new object[] { symbolInfo, compilation })!;

            Assert.NotNull(result);
            Assert.Equal("Helper", result.Name);
            // The one with string parameter calls infrastructure
        }
    }
}
