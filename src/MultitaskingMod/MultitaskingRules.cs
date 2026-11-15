using GamesByMalcs.Melvor2.Components.Actions;

namespace MultitaskingMod;

/// <summary>
/// Decides which already-running actions must stop when a new one starts.
/// </summary>
internal static class MultitaskingRules
{
    public static bool Conflicts(ActionComponent active, ActionComponent starting)
    {
        // Restarting the same action (e.g. picking a different recipe/tree in the same skill) still
        // goes through stop -> start, exactly like vanilla.
        if (ReferenceEquals(active, starting))
        {
            return true;
        }

        // One action per skill/entity. Everything else may run in parallel.
        // TODO: decide on combat vs. skills, shared resources (e.g. Thieving/Combat HP), etc.
        return active.Entity == starting.Entity;
    }
}
