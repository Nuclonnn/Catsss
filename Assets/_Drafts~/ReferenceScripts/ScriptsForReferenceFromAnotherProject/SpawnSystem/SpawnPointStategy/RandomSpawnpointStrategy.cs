using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class RandomSpawnpointStrategy : ISpawnpointStrategy
{
    List<Transform> UnusedSpawnpoints;
    Transform[] spawnpoints;

    public RandomSpawnpointStrategy(Transform[] spawnpoints){
        this.spawnpoints = spawnpoints;
        UnusedSpawnpoints = new List<Transform>(spawnpoints);
    }

    public Transform NextSpawnpoint(){
        if (!UnusedSpawnpoints.Any())
            UnusedSpawnpoints = new List<Transform>(spawnpoints);
        var randomIndex = Random.Range(0, UnusedSpawnpoints.Count);
        Transform result = UnusedSpawnpoints[randomIndex];
        UnusedSpawnpoints.RemoveAt(randomIndex);
        return result;
    }

}
