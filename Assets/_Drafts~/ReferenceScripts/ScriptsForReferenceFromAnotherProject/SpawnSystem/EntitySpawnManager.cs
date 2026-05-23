using UnityEngine;

public abstract class EntitySpawnManager : MonoBehaviour
{
    [SerializeField] protected SpawnPointStrategyType spawnPointStrategyType= SpawnPointStrategyType.Linear;
    [SerializeField] protected Transform[] spawnpoints;
    protected ISpawnpointStrategy spawnpointStrategy;
    protected enum SpawnPointStrategyType
    {
        Linear,
        Random
    }

    protected virtual void Awake()
    {
        switch (spawnPointStrategyType)
        {
            case SpawnPointStrategyType.Linear:
                spawnpointStrategy = new LinearSpawnpointStrategy(spawnpoints);
                break;
            case SpawnPointStrategyType.Random:
                spawnpointStrategy = new RandomSpawnpointStrategy(spawnpoints);
                break;
        }
    }

    public abstract void SpawnEntity();
}
