using UnityEngine;

public class EntitySpawner<T> where T : Entity
{
    IEntityFactory<T> entityFactory;
    ISpawnpointStrategy spawnpointStrategy;

    public EntitySpawner(IEntityFactory<T> entityFactory, ISpawnpointStrategy spawnpointStrategy){
        this.entityFactory = entityFactory;
        this.spawnpointStrategy = spawnpointStrategy;
    }
    public T SpawnEntity(){
        Transform spawnpoint = spawnpointStrategy.NextSpawnpoint();
        return entityFactory.CreateEntity(spawnpoint);
    }
}
