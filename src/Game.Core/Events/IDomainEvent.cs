namespace Game.Core.Events;

/// <summary>
/// Маркер доменного события. События описывают уже произошедший факт,
/// не являются командами и не изменяют состояние.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Момент возникновения события в UTC.</summary>
    DateTimeOffset OccurredAtUtc { get; }
}
