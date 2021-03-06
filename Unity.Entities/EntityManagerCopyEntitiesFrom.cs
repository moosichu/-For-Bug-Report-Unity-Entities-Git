using System;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.Entities
{
    public sealed unsafe partial class EntityManager
    {
        /// <summary>
        /// Copies all entities from srcEntityManager and replaces all entities in this EntityManager
        /// </summary>
        /// <remarks>
        /// Guarantees that the chunk layout & order of the entities will match exactly, thus this method can be used for deterministic rollback.
        /// This feature is not complete and only supports a subset of the EntityManager features at the moment:
        /// * Currently it copies all SystemStateComponents (They should not be copied)
        /// * Currently does not support class based components
        /// </remarks>
        public void CopyAndReplaceEntitiesFrom(EntityManager srcEntityManager)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (srcEntityManager == null || !srcEntityManager.IsCreated)
                throw new ArgumentNullException(nameof(srcEntityManager));
            if (!IsCreated)
                throw new ArgumentException("This EntityManager has been destroyed");
#endif
            
            srcEntityManager.CompleteAllJobs();
            CompleteAllJobs();

            using (var srcChunks = srcEntityManager.m_UniversalQueryWithChunks.CreateArchetypeChunkArray(Allocator.TempJob, out var srcChunksJob))
            using (var dstChunks = m_UniversalQueryWithChunks.CreateArchetypeChunkArray(Allocator.TempJob, out var dstChunksJob))
            {
                using (var archetypeChunkChanges = EntityDiffer.GetArchetypeChunkChanges(
                    srcChunks, 
                    dstChunks, 
                    Allocator.TempJob,                                                     
                    jobHandle: out var archetypeChunkChangesJob, 
                    dependsOn: JobHandle.CombineDependencies(srcChunksJob, dstChunksJob)))
                {
                    archetypeChunkChangesJob.Complete();
                    
                    EntityDiffer.CopyAndReplaceChunks(srcEntityManager, this, m_UniversalQueryWithChunks, archetypeChunkChanges);
                    Unity.Entities.EntityComponentStore.AssertSameEntities(srcEntityManager.EntityComponentStore, EntityComponentStore);
                }
            }
        }
    }
}
