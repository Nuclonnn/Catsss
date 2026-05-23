using UnityEngine;

public interface IEntityFactory<T> where T : Entity
{
    T CreateEntity(Transform spawnpoint);
}
