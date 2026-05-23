using UnityEngine;

public class Collectible : Entity
{
    [SerializeField] int scoreValue = 10; //FIXME set using Factory
    [SerializeField] IntEventChannel scoreChannel;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            scoreChannel.Invoke(scoreValue);
            Destroy(gameObject);
        }
    }
}
