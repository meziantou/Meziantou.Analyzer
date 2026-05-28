using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Internals;

internal sealed class TimeSpanOperation(Compilation compilation)
{
    private readonly ISymbol? _timeSpanSymbol = compilation.GetBestTypeByMetadataName("System.TimeSpan");
    private readonly ISymbol? _regexSymbol = compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.Regex");
    private readonly ISymbol? _timeoutSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Timeout");

    public long? GetMilliseconds(IOperation op)
    {
        if (op.SemanticModel is null)
            return null;

        if (!op.Type.IsEqualTo(_timeSpanSymbol))
            return null;

        return GetMilliseconds(op, 1d);
    }

    private long? GetMilliseconds(IOperation op, double factor)
    {
        const double TicksToMilliseconds = 1d / TimeSpan.TicksPerMillisecond;
        const double SecondsToMilliseconds = 1000;
        const double MinutesToMilliseconds = 60 * 1000;
        const double HoursToMilliseconds = 60 * 60 * 1000;
        const double DaysToMilliseconds = 24 * 60 * 60 * 1000;

        op = op.UnwrapImplicitConversionOperations();
        if (op.ConstantValue.HasValue)
        {
            if (op.ConstantValue.HasValue && op.ConstantValue.Value is long int64Value)
                return (long)(int64Value * factor);

            if (op.ConstantValue.HasValue && op.ConstantValue.Value is int int32Value)
                return (long)(int32Value * factor);

            if (op.ConstantValue.HasValue && op.ConstantValue.Value is double doubleValue)
                return (long)(doubleValue * factor);
        }

        if (op is IDefaultValueOperation)
            return 0L;

        if (op is IInvocationOperation invocationOperation)
        {
            var method = invocationOperation.TargetMethod;
            if (method.IsStatic && method.ContainingType.IsEqualTo(_timeSpanSymbol))
            {
                return method.Name switch
                {
                    "FromTicks" => GetMilliseconds(invocationOperation.Arguments[0].Value, TicksToMilliseconds),
                    "FromMilliseconds" => GetMilliseconds(invocationOperation.Arguments[0].Value, 1),
                    "FromSeconds" => GetMilliseconds(invocationOperation.Arguments[0].Value, SecondsToMilliseconds),
                    "FromMinutes" => GetMilliseconds(invocationOperation.Arguments[0].Value, MinutesToMilliseconds),
                    "FromHours" => GetMilliseconds(invocationOperation.Arguments[0].Value, HoursToMilliseconds),
                    "FromDays" => GetMilliseconds(invocationOperation.Arguments[0].Value, DaysToMilliseconds),
                    _ => null,
                };
            }

            return null;
        }

        if (op is IFieldReferenceOperation fieldReferenceOperation)
        {
            var member = fieldReferenceOperation.Member;
            if (member.IsStatic && member.ContainingType.IsEqualTo(_timeSpanSymbol))
            {
                return member.Name switch
                {
                    "Zero" => 0,
                    "MinValue" => (long)TimeSpan.MinValue.TotalMilliseconds,
                    "MaxValue" => (long)TimeSpan.MaxValue.TotalMilliseconds,
                    _ => null,
                };
            }

            if (member.IsStatic && member.ContainingType.IsEqualTo(_regexSymbol))
            {
                return member.Name switch
                {
                    "InfiniteMatchTimeout" => -1L,
                    _ => null,
                };
            }

            if (member.IsStatic && member.ContainingType.IsEqualTo(_timeoutSymbol))
            {
                return member.Name switch
                {
                    "InfiniteTimeSpan" => -1L,
                    "Infinite" => -1L,
                    _ => null,
                };
            }

            return null;
        }

        if (op is IObjectCreationOperation objectCreationOperation)
        {
            if (objectCreationOperation.Type.IsEqualTo(_timeSpanSymbol))
            {
                return objectCreationOperation.Arguments.Length switch
                {
                    // new TimeSpan(long ticks)
                    1 => GetMilliseconds(objectCreationOperation.Arguments[0].Value, 1d / TimeSpan.TicksPerMillisecond),

                    // new TimeSpan(int hours, int minutes, int seconds)
                    3 => AddValues(GetMilliseconds(objectCreationOperation.Arguments[0].Value, HoursToMilliseconds),
                                   GetMilliseconds(objectCreationOperation.Arguments[1].Value, MinutesToMilliseconds),
                                   GetMilliseconds(objectCreationOperation.Arguments[2].Value, SecondsToMilliseconds)),

                    // new TimeSpan(int days, int hours, int minutes, int seconds)
                    4 => AddValues(GetMilliseconds(objectCreationOperation.Arguments[0].Value, DaysToMilliseconds),
                                   GetMilliseconds(objectCreationOperation.Arguments[1].Value, HoursToMilliseconds),
                                   GetMilliseconds(objectCreationOperation.Arguments[2].Value, MinutesToMilliseconds),
                                   GetMilliseconds(objectCreationOperation.Arguments[3].Value, SecondsToMilliseconds)),

                    // new TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds)
                    5 => AddValues(GetMilliseconds(objectCreationOperation.Arguments[0].Value, DaysToMilliseconds),
                                   GetMilliseconds(objectCreationOperation.Arguments[1].Value, HoursToMilliseconds),
                                   GetMilliseconds(objectCreationOperation.Arguments[2].Value, MinutesToMilliseconds),
                                   GetMilliseconds(objectCreationOperation.Arguments[3].Value, SecondsToMilliseconds),
                                   GetMilliseconds(objectCreationOperation.Arguments[4].Value, 1)),
                    _ => null,
                };
            }
        }

        return null;

        static long? AddValues(params ReadOnlySpan<long?> values)
        {
            var result = 0L;
            foreach (var value in values)
            {
                if (!value.HasValue)
                    return null;

                result += value.GetValueOrDefault();
            }

            return result;
        }
    }
}
