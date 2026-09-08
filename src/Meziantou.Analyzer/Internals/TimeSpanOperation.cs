namespace Meziantou.Analyzer.Internals;

internal sealed class TimeSpanOperation(Compilation compilation)
{
    private const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

    private readonly ISymbol? _timeSpanSymbol = compilation.GetBestTypeByMetadataName("System.TimeSpan");
    private readonly ISymbol? _regexSymbol = compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.Regex");
    private readonly ISymbol? _timeoutSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Timeout");

    /// <summary>
    /// Gets the duration of a <see cref="TimeSpan"/> expression, or <see langword="null"/> when the expression is not
    /// supported or when its duration is not an exact number of milliseconds.
    /// </summary>
    public long? GetMilliseconds(IOperation op)
    {
        if (op.SemanticModel is null)
            return null;

        if (!op.Type.IsEqualTo(_timeSpanSymbol))
            return null;

        long ticks;
        try
        {
            if (GetTicks(op) is not long value)
                return null;

            ticks = value;
        }
        catch (OverflowException)
        {
            // The arithmetic of the components is checked, so a duration that cannot be represented by a
            // TimeSpan, which the code creating it would report at runtime, is unknown
            return null;
        }

        // The callers replace the expression with a number of milliseconds, so a duration that is not an exact
        // number of milliseconds cannot be reported without changing the behavior of the code
        if (ticks % TimeSpan.TicksPerMillisecond is not 0)
            return null;

        return ticks / TimeSpan.TicksPerMillisecond;
    }

    private long? GetTicks(IOperation op)
    {
        op = op.UnwrapImplicitConversions();

        if (op is IDefaultValueOperation)
            return 0L;

        if (op is IFieldReferenceOperation fieldReferenceOperation)
            return GetFieldTicks(fieldReferenceOperation.Member);

        // TimeSpan.FromSeconds(1, milliseconds: 500)
        if (op is IInvocationOperation invocationOperation)
        {
            var method = invocationOperation.TargetMethod;
            if (!method.IsStatic || !method.ContainingType.IsEqualTo(_timeSpanSymbol))
                return null;

            return GetComponentsTicks(invocationOperation.Arguments, GetTicksPerUnit(method.Name));
        }

        // new TimeSpan(hours: 1, minutes: 2, seconds: 3)
        if (op is IObjectCreationOperation objectCreationOperation && objectCreationOperation.Type.IsEqualTo(_timeSpanSymbol))
            return GetComponentsTicks(objectCreationOperation.Arguments, valueParameterTicksPerUnit: null);

        return null;
    }

    private long? GetFieldTicks(ISymbol member)
    {
        if (!member.IsStatic)
            return null;

        if (member.ContainingType.IsEqualTo(_timeSpanSymbol))
        {
            return member.Name switch
            {
                "Zero" => 0L,
                "MinValue" => TimeSpan.MinValue.Ticks,
                "MaxValue" => TimeSpan.MaxValue.Ticks,
                _ => null,
            };
        }

        if (member.ContainingType.IsEqualTo(_regexSymbol))
        {
            return member.Name switch
            {
                "InfiniteMatchTimeout" => -TimeSpan.TicksPerMillisecond,
                _ => null,
            };
        }

        if (member.ContainingType.IsEqualTo(_timeoutSymbol))
        {
            return member.Name switch
            {
                "InfiniteTimeSpan" => -TimeSpan.TicksPerMillisecond,
                _ => null,
            };
        }

        return null;
    }

    /// <summary>
    /// Sums the components of a <c>TimeSpan.FromXXX</c> overload or of a <see cref="TimeSpan"/> constructor. The unit
    /// of a component comes from the name of its parameter, so the optional components and the named arguments given
    /// in any order are evaluated too, and an overload with an unknown component is unknown.
    /// </summary>
    /// <param name="valueParameterTicksPerUnit">
    /// The unit of the <c>value</c> parameter of the overloads taking a single value, such as
    /// <see cref="TimeSpan.FromSeconds(double)"/>, which is named after the method instead of the unit.
    /// </param>
    private static long? GetComponentsTicks(ImmutableArray<IArgumentOperation> arguments, long? valueParameterTicksPerUnit)
    {
        var ticks = 0L;
        foreach (var argument in arguments)
        {
            var parameter = argument.Parameter;
            if (parameter is null)
                return null;

            var ticksPerUnit = parameter.Name is "value" ? valueParameterTicksPerUnit : GetTicksPerUnit(parameter.Name);
            if (ticksPerUnit is null)
                return null;

            if (GetComponentTicks(argument.Value, ticksPerUnit.GetValueOrDefault()) is not long componentTicks)
                return null;

            checked
            {
                ticks += componentTicks;
            }
        }

        return ticks;
    }

    private static long? GetComponentTicks(IOperation operation, long ticksPerUnit)
    {
        var constantValue = operation.ConstantValue;
        if (!constantValue.HasValue)
            return null;

        checked
        {
            return constantValue.Value switch
            {
                int int32Value => int32Value * ticksPerUnit,
                long int64Value => int64Value * ticksPerUnit,

                // The overloads taking a double multiply the value by the number of ticks of the unit and truncate the
                // result. The conversion is checked, so a value that is out of range or NaN is unknown.
                double doubleValue => (long)(doubleValue * ticksPerUnit),
                _ => null,
            };
        }
    }

    /// <summary>
    /// Gets the number of ticks of a unit named by the parameter of a <see cref="TimeSpan"/> component, such as
    /// <c>seconds</c>, or by a <c>TimeSpan.FromXXX</c> method, such as <c>FromSeconds</c>.
    /// </summary>
    private static long? GetTicksPerUnit(string name) => name switch
    {
        "ticks" or "FromTicks" => 1L,
        "microseconds" or "FromMicroseconds" => TicksPerMicrosecond,
        "milliseconds" or "FromMilliseconds" => TimeSpan.TicksPerMillisecond,
        "seconds" or "FromSeconds" => TimeSpan.TicksPerSecond,
        "minutes" or "FromMinutes" => TimeSpan.TicksPerMinute,
        "hours" or "FromHours" => TimeSpan.TicksPerHour,
        "days" or "FromDays" => TimeSpan.TicksPerDay,
        _ => null,
    };
}
