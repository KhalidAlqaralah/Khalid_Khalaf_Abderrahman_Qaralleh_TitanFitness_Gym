using TitanFitness.Application.Common;

namespace TitanFitness.Infrastructure.Services;

/// <summary>Local time of the machine running the API: the gym's own clock.</summary>
internal sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTime Now => timeProvider.GetLocalNow().DateTime;
}
