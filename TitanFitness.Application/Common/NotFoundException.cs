namespace TitanFitness.Application.Common;

public sealed class NotFoundException(string entity, object key)
    : Exception($"{entity} '{key}' was not found.");