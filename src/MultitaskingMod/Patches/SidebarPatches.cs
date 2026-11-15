using GamesByMalcs.Melvor2;
using GamesByMalcs.Melvor2.Components.Actions;
using GamesByMalcs.Melvor2.Entities;
using GamesByMalcs.Melvor2.Entities.Combat;
using GamesByMalcs.Melvor2.GameMenus.Pages;
using GamesByMalcs.Melvor2.GameMenus.Sidebar;
using GamesByMalcs.Melvor2.GameMenus.SidebarLegacy;
using Godot;
using HarmonyLib;
using MultitaskingMod.UI;

namespace MultitaskingMod.Patches;

/// <summary>
/// Vanilla shows the single active action as an extra button at the top of the sidebar. Skills now show their
/// progress on their own sidebar button, so the top button is reserved for combat (which has no sidebar button).
/// </summary>
internal static class GameSidebarPatches
{
    private static AccessTools.FieldRef<GameSidebar, Game?> s_game = null!;
    private static Func<GameSidebar, MarginContainer> s_activeActionContainer = null!;
    private static Action<GameSidebar, ActionComponent> s_updateForActiveAction = null!;

    public static readonly Feature Feature = new("Combat in the sidebar's top button", typeof(GameSidebarPatches), () =>
    {
        s_game = AccessTools.FieldRefAccess<GameSidebar, Game?>("<Game>k__BackingField");
        s_activeActionContainer = AccessTools.MethodDelegate<Func<GameSidebar, MarginContainer>>(
            AccessTools.PropertyGetter(typeof(GameSidebar), "ActiveActionContainer"));
        s_updateForActiveAction = AccessTools.MethodDelegate<Action<GameSidebar, ActionComponent>>(
            AccessTools.Method(typeof(GameSidebar), "UpdateForActiveAction"));
    });

    // One sidebar exists at a time; remember its handler so UnhandleEvents can remove it.
    private static EventHandler<ActionStoppedEventArgs>? s_stoppedHandler;

    private static bool IsCombat(ActionComponent action) => action.Entity is CombatEntity;

    // Skills only leave the top button while their own sidebar buttons show their progress.
    [HarmonyPatch(typeof(GameSidebar), "UpdateForActiveAction")]
    private static class UpdateForActiveAction
    {
        private static bool Prepare() => Feature.Available;

        private static bool Prefix(ActionComponent action) =>
            !SkillNavigationButtonPatches.Feature.Enabled || Feature.Run(action, IsCombat, fallback: true);
    }

    // Vanilla only looks at ActiveActions.First(); combat may be anywhere in the list.
    [HarmonyPatch(typeof(GameSidebar), nameof(GameSidebar.Update))]
    private static class Update
    {
        private static bool Prepare() => Feature.Available;

        private static void Postfix(GameSidebar __instance) => Feature.Run(__instance, static sidebar =>
        {
            if (s_game(sidebar)?.ActionManager.ActiveActions.FirstOrDefault(IsCombat) is { } combat)
            {
                s_updateForActiveAction(sidebar, combat);
            }
        });
    }

    // Vanilla never hides the button once shown; hide it when combat stops.
    [HarmonyPatch(typeof(GameSidebar), nameof(GameSidebar.HandleEvents))]
    private static class HandleEvents
    {
        private static bool Prepare() => Feature.Available;

        private static void Postfix(GameSidebar __instance) => Feature.Run(__instance, static sidebar =>
        {
            if (s_game(sidebar) is not { } game || s_stoppedHandler != null)
            {
                return;
            }
            s_stoppedHandler = (_, e) => Feature.Run((sidebar, e.Action), static args =>
            {
                if (IsCombat(args.Action))
                {
                    s_activeActionContainer(args.sidebar).Visible = false;
                }
            });
            game.ActionManager.ActionStopped += s_stoppedHandler;
        });
    }

    // Not skipped when the feature got disabled: the handler must still be removed.
    [HarmonyPatch(typeof(GameSidebar), nameof(GameSidebar.UnhandleEvents))]
    private static class UnhandleEvents
    {
        private static bool Prepare() => Feature.Available;

        private static void Prefix(GameSidebar __instance)
        {
            if (s_stoppedHandler != null)
            {
                try
                {
                    RemoveHandler(__instance);
                }
                catch (Exception e)
                {
                    Feature.Disable(e);
                }
            }
        }

        private static void RemoveHandler(GameSidebar sidebar)
        {
            if (s_game(sidebar) is { } game)
            {
                game.ActionManager.ActionStopped -= s_stoppedHandler;
                s_stoppedHandler = null;
            }
        }
    }
}

/// <summary>
/// Hooks the IView lifecycle of sidebar skill buttons to draw an <see cref="ActionRing"/> on them.
/// Following HandleEvents/UnhandleEvents matters: the game unhooks all views before running offline ticks on a
/// background thread.
///
/// We patch SkillNavigationButton's own On* overrides, which PageNavigationButton&lt;T&gt; calls from
/// Update/HandleEvents/UnhandleEvents. Do NOT patch the generic base methods themselves: their code is shared
/// across instantiations and tiered compilation later re-JITs it, silently dropping the Harmony patch
/// (rings vanished after exiting to the main menu and loading again).
/// </summary>
internal static class SkillNavigationButtonPatches
{
    private static Func<SkillNavigationButton, PageNavigationOption?> s_navigationOption = null!;

    public static readonly Feature Feature = new("Progress rings on sidebar skills", typeof(SkillNavigationButtonPatches), () =>
        s_navigationOption = AccessTools.MethodDelegate<Func<SkillNavigationButton, PageNavigationOption?>>(
            AccessTools.PropertyGetter(typeof(SkillNavigationButton), "NavigationOption")));

    private static ActionComponent? GetAction(SkillNavigationButton button) => s_navigationOption(button)?.Entity.FindAction();

    [HarmonyPatch(typeof(SkillNavigationButton), "OnUpdate")]
    private static class OnUpdate
    {
        private static bool Prepare() => Feature.Available;

        private static void Postfix(SkillNavigationButton __instance) => Feature.Run(__instance, static button =>
        {
            if (GetAction(button) is { } action)
            {
                var ring = ActionRing.GetOrCreate(button, action);
                ring.HandleEvents();
                ring.Update();
            }
        });
    }

    [HarmonyPatch(typeof(SkillNavigationButton), "OnHandleEvents")]
    private static class OnHandleEvents
    {
        private static bool Prepare() => Feature.Available;

        private static void Postfix(SkillNavigationButton __instance) =>
            Feature.Run(__instance, static button => ActionRing.Get(button)?.HandleEvents());
    }

    // Not skipped when the feature got disabled: rings must stop listening to the game.
    [HarmonyPatch(typeof(SkillNavigationButton), "OnUnhandleEvents")]
    private static class OnUnhandleEvents
    {
        private static bool Prepare() => Feature.Available;

        private static void Postfix(SkillNavigationButton __instance)
        {
            try
            {
                ActionRing.Get(__instance)?.UnhandleEvents();
            }
            catch (Exception e)
            {
                Feature.Disable(e);
            }
        }
    }
}

/// <summary>
/// The legacy sidebar's skill buttons have their own interval bar, one per skill, so they handle parallel actions as
/// is. But they only learn about an action from ActionStarted/ActionStopped, which don't fire when a save loads or
/// while events are unhooked for offline progress, so sync them with the actions' real state on every update.
/// </summary>
[HarmonyPatch(typeof(GameSidebarLegacy), nameof(GameSidebarLegacy.Update))]
internal static class GameSidebarLegacyUpdatePatch
{
    private static AccessTools.FieldRef<GameSidebarLegacy, Dictionary<IGameEntity, SidebarOptionContainer>> s_options = null!;

    public static readonly Feature Feature = new("Legacy sidebar progress bars", typeof(GameSidebarLegacyUpdatePatch), () =>
        s_options = AccessTools.FieldRefAccess<GameSidebarLegacy, Dictionary<IGameEntity, SidebarOptionContainer>>("_options"));

    private static bool Prepare() => Feature.Available;

    private static void Postfix(GameSidebarLegacy __instance) => Feature.Run(__instance, static sidebar =>
    {
        foreach (var (entity, container) in s_options(sidebar))
        {
            if (container.Button is LegacySkillNavigationButton button)
            {
                if (entity.FindAction() is { IsStarted: true } action)
                {
                    button.BindActionComponent(action);
                }
                else
                {
                    button.UnbindActionComponent();
                }
            }
        }
    });
}
