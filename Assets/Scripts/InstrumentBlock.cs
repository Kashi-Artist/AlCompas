using UnityEngine;

public enum InstrumentType
{
    Piano,
    Guitar,
    Drums,
    Violin,
    Flute,
    Trumpet
}

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class InstrumentBlock : MonoBehaviour
{
    [Header("Instrument Settings")]
    public InstrumentType instrumentType = InstrumentType.Piano;
    public AudioClip[] instrumentSounds;
    
    [Header("Visual Settings")]
    public Color[] instrumentColors = {
        Color.white,    // Piano
        Color.yellow,   // Guitar
        Color.red,      // Drums
        Color.blue,     // Violin
        Color.green,    // Flute
        Color.magenta   // Trumpet
    };
    
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        
        // Configurar como trigger si se desea detectar colisiones sin física
        // boxCollider.isTrigger = true;
    }
    
    void Start()
    {
        UpdateVisualAppearance();
    }
    
    public void SetInstrumentType(InstrumentType type)
    {
        instrumentType = type;
        UpdateVisualAppearance();
    }
    
    void UpdateVisualAppearance()
    {
        if (spriteRenderer != null)
        {
            // Cambiar color según el instrumento
            int colorIndex = (int)instrumentType;
            if (colorIndex < instrumentColors.Length)
            {
                spriteRenderer.color = instrumentColors[colorIndex];
            }
        }
        
        // Cambiar etiqueta para identificación
        gameObject.name = $"Block_{instrumentType}";
    }
    
    public AudioClip GetInstrumentSound(float pitch = 1f)
    {
        if (instrumentSounds != null && instrumentSounds.Length > 0)
        {
            // Seleccionar sonido basado en pitch o aleatoriamente
            int index = Mathf.FloorToInt(pitch * instrumentSounds.Length);
            index = Mathf.Clamp(index, 0, instrumentSounds.Length - 1);
            return instrumentSounds[index];
        }
        
        return null;
    }
    
    public float[] GetInstrumentHarmonics()
    {
        // Definir armónicos típicos para cada instrumento
        switch (instrumentType)
        {
            case InstrumentType.Piano:
                return new float[] { 1f, 0.5f, 0.3f, 0.2f, 0.1f }; // Fundamental + armónicos
                
            case InstrumentType.Guitar:
                return new float[] { 1f, 0.7f, 0.4f, 0.3f, 0.15f };
                
            case InstrumentType.Drums:
                return new float[] { 1f, 0.3f, 0.6f, 0.2f, 0.4f }; // Más complejo
                
            case InstrumentType.Violin:
                return new float[] { 1f, 0.8f, 0.6f, 0.4f, 0.3f };
                
            case InstrumentType.Flute:
                return new float[] { 1f, 0.2f, 0.1f, 0.05f, 0.02f }; // Más puro
                
            case InstrumentType.Trumpet:
                return new float[] { 1f, 0.6f, 0.8f, 0.5f, 0.3f };
                
            default:
                return new float[] { 1f, 0.5f, 0.25f, 0.12f, 0.06f };
        }
    }
    
    public Vector2 GetFrequencyRange()
    {
        // Rango de frecuencias típico para cada instrumento
        switch (instrumentType)
        {
            case InstrumentType.Piano:
                return new Vector2(27.5f, 4186f); // A0 to C8
                
            case InstrumentType.Guitar:
                return new Vector2(82.4f, 1320f); // E2 to E6
                
            case InstrumentType.Drums:
                return new Vector2(60f, 200f); // Frecuencias bajas
                
            case InstrumentType.Violin:
                return new Vector2(196f, 3520f); // G3 to A7
                
            case InstrumentType.Flute:
                return new Vector2(262f, 2093f); // C4 to C7
                
            case InstrumentType.Trumpet:
                return new Vector2(165f, 1175f); // E3 to D6
                
            default:
                return new Vector2(200f, 2000f);
        }
    }
    
    // Método para cuando la pelota colisiona
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            // Crear efecto visual de impacto
            StartCoroutine(FlashEffect());
        }
    }
    
    System.Collections.IEnumerator FlashEffect()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        
        yield return new WaitForSeconds(0.1f);
        
        spriteRenderer.color = originalColor;
    }
}