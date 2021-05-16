using System.Collections.Generic;
using Havok;
using Sandbox;
using Sandbox.Engine.Physics;
using VRage.ModAPI;
using VRageMath.Spatial;

namespace Profiler.Utils
{
    internal static class VRageUtils
    {
        /// <summary>
        /// Get the nearest parent object of given type searching up the hierarchy.
        /// </summary>
        /// <param name="entity">Entity to search up from.</param>
        /// <typeparam name="T">Type of the entity to search for.</typeparam>
        /// <returns>The nearest parent object of given type searched up from given entity if found, otherwise null.</returns>
        public static T GetParentEntityOfType<T>(this IMyEntity entity) where T : class, IMyEntity
        {
            while (entity != null)
            {
                if (entity is T match) return match;
                entity = entity.Parent;
            }

            return null;
        }

        public static IEnumerable<T> GetEntities<T>(this MyClusterTree.MyCluster cluster) where T : IMyEntity
        {
            var rigidbodies = ((HkWorld) cluster.UserData).RigidBodies;
            var entities = new List<T>();
            foreach (var rigidBody in rigidbodies)
            foreach (var entity in rigidBody.GetAllEntities())
            {
                if (entity is T typedEntity)
                {
                    entities.Add(typedEntity);
                }
            }

            return entities;
        }

        public static ulong CurrentGameFrameCount => MySandboxGame.Static.SimulationFrameCounter;
    }
}