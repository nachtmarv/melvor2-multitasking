using GamesByMalcs.Melvor2.Components.Actions;
using GamesByMalcs.Melvor2.Entities;

namespace MultitaskingMod;

internal static class GameEntityExtensions
{
    /// <summary>The entity's (single) action component, e.g. WoodcuttingEntity.Action. Null for Bank, Shop, etc.</summary>
    public static ActionComponent? FindAction(this IGameEntity? entity) =>
        entity is GameEntity gameEntity
            ? gameEntity.Components.Values.OfType<ActionComponent>().FirstOrDefault()
            : null;
}
