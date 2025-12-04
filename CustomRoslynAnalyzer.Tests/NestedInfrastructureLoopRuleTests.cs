using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using CustomRoslynAnalyzer.Rules;
using VerifyCS = CustomRoslynAnalyzer.Tests.Helpers.CSharpAnalyzerVerifier<CustomRoslynAnalyzer.CustomUsageAnalyzer>;

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
    }
}