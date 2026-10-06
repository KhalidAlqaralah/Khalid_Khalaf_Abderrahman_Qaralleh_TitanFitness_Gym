using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Domain.ValueObjects;

/// <summary>When a branch opens and closes each day.</summary>
public sealed record OperatingHours
{
    public TimeOnly Opens { get; private set; }
    public TimeOnly Closes { get; private set; }

    private OperatingHours()
    {
    }

    private OperatingHours(TimeOnly opens, TimeOnly closes)
    {
        Opens = opens;
        Closes = closes;
    }

    public static Result<OperatingHours> Create(TimeOnly opens, TimeOnly closes)
    {
        if (closes <= opens)
            return Error.Validation("OperatingHours.Order", "Closing time must be after opening time.", "closes");

        return new OperatingHours(opens, closes);
    }

    public bool Covers(TimeOnly start, TimeOnly end) => start >= Opens && end <= Closes;

    public override string ToString() => $"{Opens:HH\\:mm}-{Closes:HH\\:mm}";
}
