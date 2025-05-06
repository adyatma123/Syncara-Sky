using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProps : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth = 100;
    public int movSpeed;
    public int enemyDmg = 1;
    public float fireRate;
    public int scoreVal = 100;

    public event Action<int> OnEnemyDestroyedByPlayer;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    public void TakeDamage(int damageAmount, GameObject damageSource = null)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            // Check if the damage source was the player before triggering the event
            if (damageSource != null && damageSource.CompareTag("Player"))
            {
                // Trigger the event, passing the score value
                OnEnemyDestroyedByPlayer?.Invoke(scoreVal);
            }

            Destroy(gameObject);
            AudioManager.Instance.PlaySFX("Explode");
        }
    }
}
