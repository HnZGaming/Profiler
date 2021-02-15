using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;
using Profiler.Utils;
using Torch.Managers.PatchManager;
using VRage;
using VRage.Network;

namespace Profiler.Core.Patches
{
    public static class MyReplicationLayer_OnEvent
    {
        const ProfilerCategory Category = ProfilerCategory.UpdateNetworkEvent;
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        static readonly Type SelfType = typeof(MyReplicationLayer_OnEvent);
        static readonly Type Type = typeof(MyReplicationLayer);
        static readonly MethodInfo Method = Type.GetMethod("OnEvent", new[] {typeof(MyPacket)});
        static readonly int MethodIndex = StringIndexer.Instance.IndexOf($"{Type.FullName}#{Method.Name}");

        public static void Patch(PatchContext ctx)
        {
            try
            {
                var prefix = SelfType.GetStaticMethod(nameof(Prefix));
                var suffix = SelfType.GetStaticMethod(nameof(Suffix));

                ctx.GetPattern(Method).Prefixes.Add(prefix);
                ctx.GetPattern(Method).Suffixes.Add(suffix);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to patch: {e.Message}");
            }
        }

        // ReSharper disable once RedundantAssignment
        // ReSharper disable once UnusedParameter.Local
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Prefix(object __instance, ref ProfilerToken? __localProfilerHandle)
        {
            __localProfilerHandle = ProfilerPatch.StartToken(__instance, MethodIndex, Category);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Suffix(ref ProfilerToken? __localProfilerHandle)
        {
            ProfilerPatch.StopToken(in __localProfilerHandle);
        }
    }
}