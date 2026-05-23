using UnityEngine;

public class CollectibleSpawnManager : EntitySpawnManager
{
    [SerializeField] protected CollectibleData[] collectibleData;
    [SerializeField] protected float spawnInterval = 1f;
    [SerializeField] bool parentSpawnedCollectiblesToSpawnpoint = true;

    CooldownTimer spawnCooldownTimer;
    int counter = 0;

    EntitySpawner<Collectible> collectibleSpawner;
    protected override void Awake()
    {
        base.Awake();
        collectibleSpawner = new EntitySpawner<Collectible>(
            new EntityFactory<Collectible>(collectibleData, parentSpawnedCollectiblesToSpawnpoint),
            spawnpointStrategy
        );
        spawnCooldownTimer = new CooldownTimer(spawnInterval);
        spawnCooldownTimer.OnTimerStop+=()=>{
            if(counter++ >= spawnpoints.Length){
                spawnCooldownTimer.Stop();
                return;
            }
            SpawnEntity();
            spawnCooldownTimer.Start();
        };
    }

    void Start()
    {
        spawnCooldownTimer.Start();
    }

    void Update()
    {
        spawnCooldownTimer.Tick(Time.deltaTime);
    }

    public override void SpawnEntity()
    {
        collectibleSpawner.SpawnEntity();
    }
}
