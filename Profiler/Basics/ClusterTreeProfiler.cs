using Profiler.Core;
using Profiler.Core.Patches;
using VRage.Collections;
using VRageMath.Spatial;

namespace Profiler.Basics
{
    public sealed class ClusterTreeProfiler : BaseProfiler<MyClusterTree.MyCluster>
    {
        static readonly MyConcurrentHashSet<object> _activeProfilers;

        static ClusterTreeProfiler()
        {
            _activeProfilers = new MyConcurrentHashSet<object>();
        }

        public ClusterTreeProfiler()
        {
            _activeProfilers.Add(this);
            MyPhysics_StepWorlds.Enabled = true;
        }

        public override void Dispose()
        {
            base.Dispose();

            _activeProfilers.Remove(this);
            if (_activeProfilers.Count == 0)
            {
                MyPhysics_StepWorlds.Enabled = false;
            }
        }

        protected override bool TryAccept(in ProfilerResult profilerResult, out MyClusterTree.MyCluster key)
        {
            key = null;

            if (profilerResult.Category != ProfilerCategory.Physics) return false;
            if (profilerResult.GameEntity is not MyClusterTree.MyCluster cluster) return false;

            key = cluster;
            return true;
        }
    }
}