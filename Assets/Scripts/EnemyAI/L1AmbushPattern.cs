using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;

public class L1AmbushPattern : IBehaviorPattern
{
    private StrategicSystem strategicSystem;

    public L1AmbushPattern(StrategicSystem system)
    {
        strategicSystem = system;
    }

    public async UniTask RunAsync(List<StrategicSystem.EnemyStatus> statuses, CancellationToken cancellationToken)
    {
        // Filter statuses for this pattern: intruderAlert, not seeing player
        var candidates = statuses.FindAll(s => !s.isDead && s.intruderAlert && !s.PlayerSeen);
        if (candidates.Count < 2)
            return;

        // Group into fixed-size groups
        var pairs = new List<List<StrategicSystem.EnemyStatus>>();
        int gs = strategicSystem.ambushGroupSize;
        for (int i = 0; i + gs - 1 < candidates.Count; i += gs)
        {
            var group = new List<StrategicSystem.EnemyStatus>();
            for (int j = 0; j < gs; j++)
                group.Add(candidates[i + j]);
            pairs.Add(group);
        }

        // For each pair, send out to random point within 12m of player, then sit and wait
        foreach (var group in pairs)
        {
            if (cancellationToken.IsCancellationRequested) break;
            // sample random point around player within ambush radius
            Vector3 center = strategicSystem.playerTransform.position;
            Vector3 randOffset = UnityEngine.Random.insideUnitSphere * strategicSystem.ambushRadius;
            randOffset.y = 0;
            Vector3 desired = center + randOffset;
            if (!NavMesh.SamplePosition(desired, out var hit, 1f, NavMesh.AllAreas))
                continue;
            // move agents
            var agents = new List<NavMeshAgent>();
            // move each status out of sitting
            foreach (var status in group)
            {
                var agent = status.enemyObject.GetComponent<NavMeshAgent>();
                agents.Add(agent);
                status.isSitting = false;
                agent.isStopped = false;
                // approach distance offset from origin
                float d = UnityEngine.Random.Range(strategicSystem.approachDistanceMin, strategicSystem.approachDistanceMax);
                Vector3 origin = status.enemyObject.transform.position;
                Vector3 dir = (center - origin).normalized;
                Vector3 target = origin + dir * d;
                if (NavMesh.SamplePosition(target, out var p, 1f, NavMesh.AllAreas))
                    agent.SetDestination(p.position);
            }
            // wait until all arrive
            await UniTask.WaitUntil(() => agents.TrueForAll(a => !a.pathPending && a.remainingDistance <= a.stoppingDistance), cancellationToken: cancellationToken);
            // then sit and set flag
            foreach (var status in group)
            {
                var agent = status.enemyObject.GetComponent<NavMeshAgent>();
                agent.isStopped = true;
                status.isSitting = true;
            }
            // wait ambush duration or abort if player seen
            var endTime = Time.time + strategicSystem.ambushWaitDuration;
            while (Time.time < endTime && !cancellationToken.IsCancellationRequested)
            {
                bool anySaw = group.Exists(s => s.PlayerSeen);
                if (anySaw) return;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
    }
} 