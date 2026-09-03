using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Infrastructure.Persistence;

internal sealed class MoneyConverter() : ValueConverter<Money, decimal>(
    money => money.Amount,
    value => new Money(value));

internal sealed class MembershipNumberConverter() : ValueConverter<MembershipNumber, string>(
    number => number.Value,
    value => new MembershipNumber(value));