using GamesByMalcs.Melvor2.Components.Trackers;
using GamesByMalcs.Melvor2.Entities;
using GamesByMalcs.Melvor2.GameMenus.Footer;
using GamesByMalcs.Melvor2.GameMenus.Pages;
using HarmonyLib;

namespace MultitaskingMod.Patches;

/// <summary>
/// Vanilla binds the footer (current action, XP to next level, gains/losses per hour) to the most recently
/// started action. We bind it to the skill whose page is currently open instead, active or not.
/// Pages without a tracker (Bank, Shop, ...) keep the vanilla behaviour.
/// </summary>
internal static class FooterPatches
{
    private static AccessTools.FieldRef<EntityActiveGamePage, EntityPage> s_pageData = null!;
    private static Func<GameFooter, TrackerComponentFooterView> s_trackerView = null!;

    public static readonly Feature Feature = new("Footer follows the open page", typeof(FooterPatches), () =>
    {
        s_pageData = AccessTools.FieldRefAccess<EntityActiveGamePage, EntityPage>("<PageData>k__BackingField");
        s_trackerView = AccessTools.MethodDelegate<Func<GameFooter, TrackerComponentFooterView>>(
            AccessTools.PropertyGetter(typeof(GameFooter), "TrackerView"));
    });

    /// <summary>The entity whose page is open, or null when that page has nothing to track.</summary>
    private static IGameEntity? ViewedEntity { get; set; }

    [HarmonyPatch(typeof(EntityActiveGamePage), "SetFooter")]
    private static class SetFooter
    {
        private static bool Prepare() => Feature.Available;

        private static void Postfix(EntityActiveGamePage __instance, GameFooter footer) =>
            Feature.Run((__instance, footer), static args =>
            {
                var entity = s_pageData(args.__instance).Entity;
                ViewedEntity = entity is ITrackedEntity ? entity : null;
                args.footer.Update();
            });
    }

    [HarmonyPatch(typeof(EntityActiveGamePage), "CleanupFooter")]
    private static class CleanupFooter
    {
        private static bool Prepare() => Feature.Available;

        private static void Prefix() => ViewedEntity = null;
    }

    [HarmonyPatch(typeof(GameFooter), nameof(GameFooter.Update))]
    private static class Update
    {
        private static bool Prepare() => Feature.Available;

        // Returns false (skip vanilla) only when we bound the footer ourselves.
        private static bool Prefix(GameFooter __instance) =>
            ViewedEntity is not ITrackedEntity || Feature.Run(__instance, static footer =>
            {
                var tracked = (ITrackedEntity)ViewedEntity!;
                var view = s_trackerView(footer);
                view.Bind(tracked.Tracker);
                if (ViewedEntity.FindAction() is { } action)
                {
                    view.BindAction(action);
                }
                else
                {
                    view.UnbindAction();
                }
                return false;
            }, fallback: true);
    }
}
