using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using GamesByMalcs.Melvor2.Components.Actions;
using HarmonyLib;

namespace MultitaskingMod.Patches;

/// <summary>
/// Vanilla <see cref="ActionComponent.TryStart"/> calls <c>ActionManager.TryStopActive()</c>, which stops
/// every running action before starting a new one. We swap that call for <see cref="TryStopConflicting"/>,
/// which only stops actions that conflict with the one being started.
///
/// <see cref="ActiveActionManager"/> already tracks a list of active actions (and saves/loads all of them),
/// and every entity is ticked regardless of being "active", so this is enough for actions to run in parallel.
/// </summary>
[HarmonyPatch(typeof(ActionComponent), nameof(ActionComponent.TryStart))]
internal static class ActionComponentTryStartPatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var tryStopActive = AccessTools.Method(typeof(ActiveActionManager), nameof(ActiveActionManager.TryStopActive));
        var tryStopConflicting = AccessTools.Method(typeof(ActionComponentTryStartPatch), nameof(TryStopConflicting));

        // Stack before: [manager, force]  ->  after: [manager, force, this]
        return new CodeMatcher(instructions)
            .MatchStartForward(new CodeMatch(OpCodes.Callvirt, tryStopActive))
            .ThrowIfInvalid("ActiveActionManager.TryStopActive call not found in ActionComponent.TryStart")
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
            .SetInstruction(new CodeInstruction(OpCodes.Call, tryStopConflicting))
            .InstructionEnumeration();
    }

    private static bool s_failed;

    /// <returns>True if every conflicting action was stopped, i.e. <paramref name="starting"/> may start.</returns>
    /// <remarks>Falls back to vanilla (stop all actions) for the session if this ever fails, e.g. after a game update.</remarks>
    internal static bool TryStopConflicting(ActiveActionManager manager, bool force, ActionComponent starting)
    {
        if (!s_failed)
        {
            try
            {
                return StopConflicting(manager, force, starting);
            }
            catch (Exception e)
            {
                s_failed = true;
                Log.Error($"Parallel actions disabled after an error (game update?); starting an action stops the others again. {e}");
            }
        }
        return manager.TryStopActive(force);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool StopConflicting(ActiveActionManager manager, bool force, ActionComponent starting)
    {
        var allStopped = true;
        foreach (var active in manager.ActiveActions.ToList())
        {
            if (MultitaskingRules.Conflicts(active, starting))
            {
                allStopped &= active.TryStop(force);
            }
        }
        return allStopped;
    }
}
