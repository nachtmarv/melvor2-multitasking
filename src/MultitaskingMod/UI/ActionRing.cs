using System.Runtime.CompilerServices;
using GamesByMalcs.Melvor2.Components.Actions;
using GamesByMalcs.Melvor2.Core.TickTimer.Views;
using Godot;
using MultitaskingMod.Patches;

namespace MultitaskingMod.UI;

/// <summary>
/// The grey "current action" progress ring from the vanilla ActiveActionButton
/// (res://GameMenus/Sidebar/active_action_button.tscn), grafted onto a sidebar navigation button.
/// Mirrors ActiveActionButton.ShowActionTimer/HideActionTimer.
/// </summary>
internal sealed class ActionRing
{
    private const string RadialTexturePath = "res://Assets/RadialProgressTexture.png";
    private static readonly Color s_tint = new(0.647395f, 0.647395f, 0.647395f);
    private static readonly ConditionalWeakTable<Control, ActionRing> s_rings = new();

    private readonly MarginContainer _host;
    private readonly TickTimerTextureProgressBar _interval;
    private readonly TextureProgressBar _randomInterval;
    private Tween? _spin;
    private bool _subscribed;

    private ActionComponent Action { get; }

    private ActionRing(Control button, ActionComponent action)
    {
        Action = action;
        var texture = GD.Load<Texture2D>(RadialTexturePath);

        _host = new MarginContainer { Name = "MultitaskingIntervalContainer", MouseFilter = Control.MouseFilterEnum.Ignore };
        _interval = CreateRing(new TickTimerTextureProgressBar(), texture);
        _randomInterval = CreateRing(new TextureProgressBar(), texture);
        _randomInterval.Value = 20;
        _randomInterval.Resized += () => _randomInterval.PivotOffset = _randomInterval.Size / 2;
        _host.AddChild(_interval);
        _host.AddChild(_randomInterval);

        // Skill buttons share the ActiveActionButton layout: same spot as there, just inside the XP ring.
        foreach (var side in new[] { "left", "top", "right", "bottom" })
        {
            _host.AddThemeConstantOverride($"margin_{side}", -5);
        }
        button.AddChild(_host);
        if (button.GetNodeOrNull<Control>("ExperienceContainer") is { } experience)
        {
            button.MoveChild(_host, experience.GetIndex() + 1);
        }
    }

    /// <summary>Gets or creates the ring for a button showing <paramref name="action"/>'s progress.</summary>
    public static ActionRing GetOrCreate(Control button, ActionComponent action)
    {
        if (s_rings.TryGetValue(button, out var ring))
        {
            if (ring.Action == action)
            {
                return ring;
            }
            ring.Destroy();
        }
        ring = new ActionRing(button, action);
        s_rings.AddOrUpdate(button, ring);
        return ring;
    }

    public static ActionRing? Get(Control button) => s_rings.TryGetValue(button, out var ring) ? ring : null;

    public void HandleEvents()
    {
        if (!_subscribed)
        {
            Action.Started += ActionStartedOrStoppedHandler;
            Action.Stopped += ActionStartedOrStoppedHandler;
            _subscribed = true;
        }
    }

    public void UnhandleEvents()
    {
        if (_subscribed)
        {
            Action.Started -= ActionStartedOrStoppedHandler;
            Action.Stopped -= ActionStartedOrStoppedHandler;
            _subscribed = false;
        }
    }

    public void Update()
    {
        if (Action.IsStarted)
        {
            ShowActionTimer();
        }
        else
        {
            HideActionTimer();
        }
    }

    private void ShowActionTimer()
    {
        if (Action.IsTimerIndeterminate)
        {
            _interval.Unbind();
            _interval.Visible = false;
            StartSpin();
        }
        else
        {
            StopSpin();
            if (Action.ActionTimer != null)
            {
                _interval.Bind(Action.ActionTimer);
                _interval.Visible = true;
            }
            else
            {
                _interval.Visible = false;
            }
        }
    }

    private void HideActionTimer()
    {
        StopSpin();
        _interval.Unbind();
        _interval.Visible = false;
    }

    private void StartSpin()
    {
        _randomInterval.Visible = true;
        if (_spin == null || !_spin.IsValid())
        {
            _spin = _randomInterval.CreateTween().SetLoops();
            _spin.TweenProperty(_randomInterval, "rotation", Mathf.Tau, 1.0).From(0f);
        }
    }

    private void StopSpin()
    {
        _spin?.Kill();
        _spin = null;
        _randomInterval.Rotation = 0;
        _randomInterval.Visible = false;
    }

    private void Destroy()
    {
        UnhandleEvents();
        HideActionTimer();
        _host.QueueFree();
    }

    // Called by the game: an exception here would break starting/stopping actions.
    private void ActionStartedOrStoppedHandler(object? sender, EventArgs e) =>
        SkillNavigationButtonPatches.Feature.Run(this, static ring => ring.Update());

    public override string ToString() =>
        $"{Action.Entity.Id} started={Action.IsStarted} subscribed={_subscribed} timer={Action.ActionTimer?.State} hostInTree={_host.IsInsideTree()}";

    private static T CreateRing<T>(T bar, Texture2D texture) where T : TextureProgressBar
    {
        bar.Visible = false;
        bar.MouseFilter = Control.MouseFilterEnum.Ignore;
        bar.Step = 0.01;
        bar.FillMode = (int)TextureProgressBar.FillModeEnum.Clockwise;
        bar.NinePatchStretch = true;
        bar.TextureUnder = texture;
        bar.TextureProgress = texture;
        bar.TintUnder = new Color(0, 0, 0, 0);
        bar.TintProgress = s_tint;
        return bar;
    }
}
