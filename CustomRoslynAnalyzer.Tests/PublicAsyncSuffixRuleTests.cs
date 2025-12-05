using System.Threading.Tasks;
using CustomRoslynAnalyzer.Rules;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = CustomRoslynAnalyzer.Tests.Helpers.CSharpAnalyzerVerifier<CustomRoslynAnalyzer.Rules.PublicAsyncSuffixRule>;

namespace CustomRoslynAnalyzer.Tests;

public sealed class PublicAsyncSuffixRuleTests
{
    [Fact]
    public async Task ReportsMissingAsyncSuffix()
    {
        const string testCode = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task {|#0:Fetch|}()
    {
        await Task.Delay(10);
    }
}";

        var expected = VerifyCS.Diagnostic(PublicAsyncSuffixRule.DefaultDescriptor)
            .WithLocation(0)
            .WithArguments("Fetch");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DoesNotReportWhenMethodEndsWithAsync()
    {
        const string testCode = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task FetchAsync()
    {
        await Task.Delay(10);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task DoesNotReportForInternalAsyncMethod()
    {
        const string testCode = @"
using System.Threading.Tasks;

public class TestClass
{
    internal async Task Fetch()
    {
        await Task.Delay(10);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ReportsForAsyncValueTask()
    {
        const string testCode = @"
using System.Threading.Tasks;

public class TestClass
{
    public async System.Threading.Tasks.ValueTask {|#0:Fetch|}()
    {
        await Task.Delay(10);
    }
}";

        var expected = VerifyCS.Diagnostic(PublicAsyncSuffixRule.DefaultDescriptor)
            .WithLocation(0)
            .WithArguments("Fetch");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DoesNotReportForAsyncValueTaskWithAsyncSuffix()
    {
        const string testCode = @"
using System.Threading.Tasks;

public class TestClass
{
    public async System.Threading.Tasks.ValueTask FetchAsync()
    {
        await Task.Delay(10);
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task DoesNotReportPropertyGetterReturningTask()
    {
        const string testCode = @"
using System.Threading.Tasks;

public class TestClass
{
    public Task<int> Value
    {
        get => Task.FromResult(42);
        set => _ = value;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }
}
