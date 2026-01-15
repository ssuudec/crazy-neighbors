using UnityEngine;

public class Bullet_sc : MonoBehaviour
{
    public Character_sc owner; 
    private bool hasHit = false; 

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;

        // Eski güvenli komut
        GameManager_sc gameManager = FindFirstObjectByType<GameManager_sc>();
        Character_sc hitCharacter = collision.gameObject.GetComponent<Character_sc>();
        
        if (hitCharacter != null)
        {
            if (hitCharacter == owner) return; 

            hasHit = true;
            hitCharacter.TakeDamage(10); 
            
            if (gameManager != null && owner != null && owner.name == gameManager.enemyCharacter.name)
            {
                gameManager.RegisterEnemyHit(10); 
            }
        }
        else 
        {
            hasHit = true; 
        }

        if (gameManager != null) gameManager.EndTurn();
        Destroy(gameObject);
    }
    
    void OnBecameInvisible() 
    {
        if (!hasHit)
        {
            hasHit = true;
            GameManager_sc gameManager = FindFirstObjectByType<GameManager_sc>();
            if (gameManager != null) gameManager.EndTurn();
            Destroy(gameObject);
        }
    }
}