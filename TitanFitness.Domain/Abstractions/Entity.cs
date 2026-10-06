namespace TitanFitness.Domain.Abstractions;

/// <summary>Marker: this type is an entity with an identity.</summary>
public interface IEntity
{
    Guid Id { get; }
}

/// <summary>Marker: this entity is the root of an aggregate, the only way in for writes.</summary>
public interface IAggregateRoot : IEntity;

/// <summary>
/// Base class for entities. Two entities are equal when they are the same type with the same Id.
/// </summary>
public abstract class Entity : IEntity
{
    public Guid Id { get; protected set; }

    protected Entity()
    {
    }

    protected Entity(Guid id) => Id = id;

    public override bool Equals(object? obj) =>
        obj is Entity other
        && other.GetType() == GetType()
        && Id != Guid.Empty
        && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>Base class for aggregate roots.</summary>
public abstract class AggregateRoot : Entity, IAggregateRoot
{
    protected AggregateRoot()
    {
    }

    protected AggregateRoot(Guid id) : base(id)
    {
    }
}
