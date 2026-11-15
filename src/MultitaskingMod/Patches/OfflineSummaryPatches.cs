using GamesByMalcs.Melvor2.Entities.OfflineProgress;
using Godot;
using HarmonyLib;

namespace MultitaskingMod.Patches;

/// <summary>
/// The "Welcome Back!" summary lays out one ~450px panel per skill side by side in a horizontal ScrollContainer,
/// but the modal's panel is 3000px wide, so the scroll area is wider than the screen: with many skills the extra
/// panels end up off-screen and the scrollbar is never shown. Cap the scroll area to the visible width.
/// </summary>
[HarmonyPatch(typeof(OfflineSnapshotModal), nameof(OfflineSnapshotModal.Update))]
internal static class OfflineSummaryPatch
{
    private const float ScreenMargin = 64;

    private static Func<OfflineSnapshotModal, HBoxContainer> s_messageContainer = null!;

    public static readonly Feature Feature = new("Scrollable offline summary", typeof(OfflineSummaryPatch), () =>
        s_messageContainer = AccessTools.MethodDelegate<Func<OfflineSnapshotModal, HBoxContainer>>(
            AccessTools.PropertyGetter(typeof(OfflineSnapshotModal), "MessageContainer")));

    private static bool Prepare() => Feature.Available;

    private static void Postfix(OfflineSnapshotModal __instance) => Feature.Run(__instance, static modal =>
    {
        if (s_messageContainer(modal).GetParent() is not ScrollContainer scroll
            || Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }
        var visibleWidth = tree.Root.GetVisibleRect().Size.X - 2 * ScreenMargin;
        scroll.CustomMinimumSize = new Vector2(visibleWidth, scroll.CustomMinimumSize.Y);
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Auto;
    });
}
