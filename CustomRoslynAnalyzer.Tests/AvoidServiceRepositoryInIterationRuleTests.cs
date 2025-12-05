using CustomRoslynAnalyzer.Rules;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = CustomRoslynAnalyzer.Tests.Helpers.CSharpAnalyzerVerifier<CustomRoslynAnalyzer.Rules.AvoidServiceRepositoryInIterationRule>;

namespace CustomRoslynAnalyzer.Tests;

public sealed class AvoidServiceRepositoryInIterationRuleTests
{
    [Fact]
    public async Task ReportsServiceInvocationInsideForeach()
    {
        const string testCode = @"
using System.Collections.Generic;

public class Processor
{
    private readonly OrderService _service = new();

    public void Process(IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            _service.{|#0:Save|}(value);
        }
    }
}

public sealed class OrderService
{
    public void Save(int value) { }
}";

        var expected = VerifyCS.Diagnostic(AvoidServiceRepositoryInIterationRule.DefaultDescriptor)
            .WithLocation(0)
            .WithArguments("OrderService");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ReportsServiceCreationInsideForeach()
    {
        const string testCode = @"
using System.Collections.Generic;

public class Processor
{
    public void Process(IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            var repo = {|#0:new OrderRepository()|};
        }
    }
}

public sealed class OrderRepository { }";

        var expected = VerifyCS.Diagnostic(AvoidServiceRepositoryInIterationRule.DefaultDescriptor)
            .WithLocation(0)
            .WithArguments("OrderRepository");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ReportsObjectCreationWithExplicitTypeInsideForeach()
    {
        const string testCode = @"
using System.Collections.Generic;

public class Processor
{
    public void Process(IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            OrderRepository repo = {|#0:new OrderRepository()|};
        }
    }
}

public class OrderRepository { }";

        var expected = VerifyCS.Diagnostic(AvoidServiceRepositoryInIterationRule.DefaultDescriptor)
            .WithLocation(0)
            .WithArguments("OrderRepository");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ReportsTargetTypedObjectCreationInsideForeach()
    {
        const string testCode = @"
using System.Collections.Generic;

public class Processor
{
    public void Process(IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            OrderRepository repo = {|#0:new()|};
        }
    }
}

public class OrderRepository { }";

        var expected = VerifyCS.Diagnostic(AvoidServiceRepositoryInIterationRule.DefaultDescriptor)
            .WithLocation(0)
            .WithArguments("OrderRepository");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ReportsWhenOverloadResolutionFindsServiceDependency()
    {
        const string testCode = @"
using System.Collections.Generic;

public class Processor
{
    private readonly Helper _helper = new();

    public void Process(IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            _helper.{|#0:Handle|}(value);
        }
    }
}

public sealed class Helper
{
    public void Handle(object value)
    {
    }

    public void Handle(int value)
    {
        new OrderService().Save(value);
    }
}

public sealed class OrderService
{
    public void Save(int value) { }
}";

        var expected = VerifyCS.Diagnostic(AvoidServiceRepositoryInIterationRule.DefaultDescriptor)
            .WithLocation(0)
            .WithArguments("OrderService");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ReportsLambdaInvocationInsideLoopLikeCall()
    {
        const string testCode = @"
using System.Collections.Generic;

public class Processor
{
    private readonly OrderService _service = new();

    public void Process(List<int> values)
    {
        values.ForEach(v => _service.{|#0:Save|}(v));
    }
}

public sealed class OrderService
{
    public void Save(int value) { }
}";

        var expected = VerifyCS.Diagnostic(AvoidServiceRepositoryInIterationRule.DefaultDescriptor)
            .WithLocation(0)
            .WithArguments("OrderService");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task DoesNotReportOutsideIteration()
    {
        const string testCode = @"
public class Processor
{
    private readonly OrderService _service = new();

    public void Process()
    {
        _service.Save();
    }
}

public sealed class OrderService
{
    public void Save() { }
}";

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ReportsIndirectServiceInvocation()
    {
        const string testCode = @"
using System.Collections.Generic;

public class Processor
{
    public void Process(IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            {|#0:Persist|}(value);
        }
    }

    private void Persist(int value)
    {
        new OrderService().Save(value);
    }
}

public sealed class OrderService
{
    public void Save(int value) { }
}";

        var expected = VerifyCS.Diagnostic(AvoidServiceRepositoryInIterationRule.DefaultDescriptor)
            .WithLocation(0)
            .WithArguments("OrderService");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ReportsInjectedRepositoryUsageInsideForeach()
    {
        const string testCode = @"
using System.Collections.Generic;

public class Processor
{
    private readonly IOrderRepository _repository;

    public Processor(IOrderRepository repository)
    {
        _repository = repository;
    }

    public void Process(IEnumerable<int> orders)
    {
        foreach (var order in orders)
        {
            _repository.{|#0:Save|}(order);
        }
    }
}

public interface IOrderRepository
{
    void Save(int value);
}";

        var expected = VerifyCS.Diagnostic(AvoidServiceRepositoryInIterationRule.DefaultDescriptor)
            .WithLocation(0)
            .WithArguments("IOrderRepository");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }
}
