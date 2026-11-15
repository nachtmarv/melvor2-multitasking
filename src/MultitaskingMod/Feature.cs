using System.Reflection;
using HarmonyLib;

namespace MultitaskingMod;

/// <summary>
/// A group of patches that only work together (e.g. a HandleEvents/UnhandleEvents pair), switched on or off as a
/// whole so that a game update never leaves a feature half applied or breaks the game:
/// <list type="bullet">
/// <item>At startup (<see cref="Available"/>, called from each patch class's Harmony <c>Prepare</c>): resolves the
/// game members the feature reads and every method its patch classes target. If anything is missing, none of the
/// feature's patches are applied.</item>
/// <item>At runtime (<see cref="Run{T}"/>, <see cref="Run{T, TResult}"/>): patch code runs guarded. The first exception
/// is logged and disables the feature for the session; the guarded code then no longer runs and the game falls back
/// to its vanilla behaviour.</item>
/// </list>
/// Guarded code must be passed as a separate method (a lambda): .NET throws MissingMethodException etc. when it
/// compiles a method that uses a member removed by a game update, i.e. in the caller, before any try block in the
/// method itself runs.
/// </summary>
internal sealed class Feature(string name, Type patches, Action resolveMembers)
{
    private bool? _available;

    public string Name { get; } = name;

    /// <summary>True until the feature failed (at startup or at runtime).</summary>
    public bool Enabled => Available;

    /// <summary>Resolves the feature's members and patch targets once; false if any is missing.</summary>
    public bool Available => _available ??= Check();

    public void Run<T>(T arg, Action<T> body)
    {
        if (!Enabled)
        {
            return;
        }
        try
        {
            body(arg);
        }
        catch (Exception e)
        {
            Disable(e);
        }
    }

    /// <returns><paramref name="body"/>'s result, or <paramref name="fallback"/> when the feature is (or gets) disabled.</returns>
    public TResult Run<T, TResult>(T arg, Func<T, TResult> body, TResult fallback)
    {
        if (!Enabled)
        {
            return fallback;
        }
        try
        {
            return body(arg);
        }
        catch (Exception e)
        {
            Disable(e);
            return fallback;
        }
    }

    public void Disable(Exception e)
    {
        if (_available == false)
        {
            return;
        }
        _available = false;
        Log.Error($"{Name} disabled after an error (game update?); the game uses its own behaviour instead. {e}");
    }

    private bool Check()
    {
        try
        {
            resolveMembers();
            var missing = PatchClasses(patches).Where(t => ResolveTarget(t) == null).Select(t => t.FullName).ToList();
            if (missing.Count > 0)
            {
                throw new MissingMemberException($"Target method not found for {string.Join(", ", missing)}");
            }
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"{Name} disabled: the game changed (game update?). {e.GetBaseException().Message}");
            return false;
        }
    }

    private static IEnumerable<Type> PatchClasses(Type type) =>
        type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).SelectMany(PatchClasses)
            .Prepend(type)
            .Where(t => t.IsDefined(typeof(HarmonyPatch), inherit: false));

    private static MethodBase? ResolveTarget(Type patchClass)
    {
        var info = HarmonyMethod.Merge(HarmonyMethodExtensions.GetFromType(patchClass));
        return info.declaringType == null || info.methodName == null
            ? null
            : AccessTools.Method(info.declaringType, info.methodName, info.argumentTypes);
    }
}
