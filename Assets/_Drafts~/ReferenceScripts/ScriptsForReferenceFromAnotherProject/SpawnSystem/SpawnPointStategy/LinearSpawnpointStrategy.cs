using UnityEngine;
public class LinearSpawnpointStrategy : ISpawnpointStrategy
{
    int index=0;
    Transform[] spawnpoints;
    public LinearSpawnpointStrategy(Transform[] spawnpoints){
        this.spawnpoints = spawnpoints;
    }
    public Transform NextSpawnpoint(){
        Transform result = spawnpoints[index];
        index = (index + 1) % spawnpoints.Length;
        return result;
    }
}
