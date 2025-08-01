using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public interface IBehaviorPattern
{
    // Run behavior pattern asynchronously on a group of enemies, cancelable via token
    UniTask RunAsync(List<StrategicSystem.EnemyStatus> group, CancellationToken cancellationToken);
} 