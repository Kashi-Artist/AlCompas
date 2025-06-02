using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class BallController : MonoBehaviour
{
    [Header("Ball Settings")]
    public float maxSpeed = 15f;
    public float minSpeed = 5f;
    public float bounceDamping = 0.9f;
    
    private Rigidbody2D rb;
    private GameManager gameManager;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Sin gravedad para un comportamiento más controlado
    }
    
    public void Launch(Vector2 force, GameManager manager)
    {
        gameManager = manager;
        rb.AddForce(force, ForceMode2D.Impulse);
    }
    
    void FixedUpdate()
    {
        // Limitar velocidad
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
        else if (rb.velocity.magnitude < minSpeed)
        {
            rb.velocity = rb.velocity.normalized * minSpeed;
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Verificar colisión con bloque de instrumento
        InstrumentBlock instrumentBlock = collision.gameObject.GetComponent<InstrumentBlock>();
        if (instrumentBlock != null)
        {
            Vector2 collisionPoint = collision.contacts[0].point;
            gameManager.OnBallCollision(instrumentBlock, collisionPoint);
        }
        
        // Verificar colisión con pared (usando tag "Wall")
        if (collision.gameObject.CompareTag("Wall"))
        {
            Vector2 collisionPoint = collision.contacts[0].point;
            gameManager.OnBallWallCollision(collisionPoint);
        }
        
        // Aplicar damping al rebote
        rb.velocity *= bounceDamping;
        
        // Asegurar velocidad mínima después del rebote
        if (rb.velocity.magnitude < minSpeed)
        {
            rb.velocity = rb.velocity.normalized * minSpeed;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Detectar si sale de los límites del juego
        if (other.CompareTag("GameBounds"))
        {
            EndBallLife();
        }
    }
    
    void EndBallLife()
    {
        if (gameManager != null)
        {
            gameManager.EndGame();
        }
        Destroy(gameObject);
    }
}