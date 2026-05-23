using UnityEngine;

public class EntityFactory<T> : IEntityFactory<T> where T : Entity
{
    EntityData[] entityData;
    bool parentToSpawnpoint;

    public EntityFactory(EntityData[] entityData, bool parentToSpawnpoint = false){
        this.entityData = entityData;
        this.parentToSpawnpoint = parentToSpawnpoint;
    }
    public T CreateEntity(Transform spawnpoint){
        EntityData randomEntityData = entityData[Random.Range(0, entityData.Length)];
        GameObject instance = parentToSpawnpoint
            ? GameObject.Instantiate(randomEntityData.entityPrefab, spawnpoint.position, spawnpoint.rotation, spawnpoint)
            : GameObject.Instantiate(randomEntityData.entityPrefab, spawnpoint.position, spawnpoint.rotation);
        return instance.GetComponent<T>();
    }
}
