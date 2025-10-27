using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int health;
    public int maxHealth = 5;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
    }

    // Update is called once per frame
    public void TakeDame(int amount)
    {
        health -= amount;
        if(health <= 0)
        {
            Destroy(gameObject);
        } 

    }

    public void GetHealth(int amount)
    {
        health += amount;   
        if (health >= maxHealth)
        {
            health = maxHealth;
        }
    }
}
