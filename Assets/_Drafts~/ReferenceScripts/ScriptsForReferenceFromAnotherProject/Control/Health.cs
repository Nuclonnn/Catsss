using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField, Min(1)] int maxHealth = 100;
    [SerializeField] FloatEventChannel playerHealthChannel;

    [SerializeField] int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    void Awake()
    {
        currentHealth = maxHealth;
    }
    void Start()
    {
        PublishHealthPercentage();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        PublishHealthPercentage();
    }

    void PublishHealthPercentage()
    {
        if (playerHealthChannel != null)
        {
            // UI получает значение от 0 до 1, которое напрямую подходит для Image.fillAmount.
            playerHealthChannel.Invoke(Mathf.Clamp01(currentHealth / (float)maxHealth));
        }
    }
}
