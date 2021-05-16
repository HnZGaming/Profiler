using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NLog;
using Sandbox.Engine.Physics;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;
using Torch.Utils;
using VRageMath.Spatial;

namespace Profiler.Core.Patches
{
    public static class MyPhysics_StepWorlds
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        
#pragma warning disable 649
        [ReflectedMethodInfo(typeof(MyPhysics_StepWorlds), nameof(StartToken))]
        static readonly MethodInfo _startTokenMethod;
        
        [ReflectedMethodInfo(typeof(MyPhysics_StepWorlds), nameof(StopToken))]
        static readonly MethodInfo _stopTokenMethod;

        [ReflectedMethodInfo(typeof(MyPhysics), "StepWorldsParallel")]
        static readonly MethodInfo _stepWorldsParallelMethod;

        [ReflectedMethodInfo(typeof(MyPhysics_StepWorlds), nameof(StepWorldsParallelTranspiler))]
        static readonly MethodInfo _stepWorldsParallelTranspilerMethod;
#pragma warning restore 649

        static readonly int MethodIndex = StringIndexer.Instance.IndexOf($"{typeof(MyPhysics).FullName}#StepWorlds");

        public static bool Enabled;

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(_stepWorldsParallelMethod).Transpilers.Add(_stepWorldsParallelTranspilerMethod);
        }

        static IEnumerable<MsilInstruction> StepWorldsParallelTranspiler(IEnumerable<MsilInstruction> ins, Func<Type, MsilLocal> __localCreator)
        {
            var tokenStore = __localCreator(typeof(ProfilerToken?));
            var initFound = false;
            var finishFound = false;
            foreach (var instruction in ins)
            {
                if (instruction.OpCode == OpCodes.Pop) continue;
                if (instruction.OpCode == OpCodes.Callvirt &&
                    instruction.Operand is MsilOperandInline.MsilOperandReflected<MethodBase> operand)
                {
                    switch (operand.Value.Name)
                    {
                        case "InitMtStep":
                            // call virt
                            yield return instruction;
                            // pop
                            yield return new MsilInstruction(OpCodes.Pop);
                            // load cluster
                            yield return new MsilInstruction(OpCodes.Ldloc_S).InlineValue(new MsilLocal(4));
                            // create token
                            yield return new MsilInstruction(OpCodes.Call).InlineValue(_startTokenMethod);
                            // save token to local ver
                            yield return tokenStore.AsValueStore();

                            initFound = true;
                            continue;
                        case "FinishMtStep":
                            // call virt
                            yield return instruction;
                            // pop
                            yield return new MsilInstruction(OpCodes.Pop);
                            // load saved token
                            yield return tokenStore.AsReferenceLoad();
                            // finish token
                            yield return new MsilInstruction(OpCodes.Call).InlineValue(_stopTokenMethod);

                            finishFound = true;
                            continue;
                    }
                }

                yield return instruction;
            }

            if (!initFound || !finishFound)
            {
                throw new MissingMemberException();
            }
        }

        static ProfilerToken? StartToken(MyClusterTree.MyCluster cluster)
        {
            if (!Enabled) return null;

            Log.Info($"physics profiling starting: {cluster} ({cluster.GetHashCode()})");
            return ProfilerPatch.StartToken(cluster, MethodIndex, ProfilerCategory.Physics);
        }
        
        static void StopToken(in ProfilerToken? tokenOrNull)
        {
            try
            {
                if (!Enabled) return;
                if (!(tokenOrNull is ProfilerToken token)) return;

                var result = new ProfilerResult(token);
                ProfilerResultQueue.Enqueue(result);
                
                Log.Info($"physics profiling ended: {token.GameEntity} ({token.GameEntity.GetHashCode()})");
            }
            catch (Exception e)
            {
                Log.Error($"{e}; token: {tokenOrNull?.ToString() ?? "no token"}");
            }
        }
    }
}