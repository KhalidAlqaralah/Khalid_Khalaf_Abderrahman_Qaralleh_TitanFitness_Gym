namespace TitanFitness.Domain.ValueObjects;

public sealed record OperatingHours
{
    public TimeOnly Opens { get; private set; }
    public TimeOnly Closes { get; private set; }

    private OperatingHours() { }

    public OperatingHours(TimeOnly opens, TimeOnly closes)
    {
        if (closes <= opens)
            throw new ArgumentException("Closing time must be after opening time.", nameof(closes));

        Opens = opens;
        Closes = closes;
    }

    public bool Covers(TimeOnly start, TimeOnly end) => start >= Opens && end <= Closes;

    public override string ToString() => $"{Opens:HH\\:mm}–{Closes:HH\\:mm}";
}