using UnityEngine;

public class Bullet_sc : MonoBehaviour
{
    public float damage = 20f;
    private float dogumZamani;

    [HideInInspector]
    public Character_sc owner;

    void Start()
    {
        dogumZamani = Time.time;
        Destroy(gameObject, 8f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time - dogumZamani < 0.05f) return;

        Character_sc target = collision.collider.GetComponent<Character_sc>();
        if (target != null)
        {
            // kendi kendine çarpmaz
            if (owner != null && target == owner) return;

            target.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // wall veya grounda çarpınca yok etmek için 
        if (collision.collider.CompareTag("Wall") || collision.collider.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (transform.position.y < -6f || transform.position.x < -15f || transform.position.x > 15f)
        {
            Destroy(gameObject);
        }
    }
}
