using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SpectrogramVisualizer : MonoBehaviour
{
    [Header("UI Components")]
    public RawImage spectrogramDisplay;
    public Slider frequencyRangeSlider;
    public TextMeshProUGUI frequencyRangeText;
    public Toggle showSpectrogramToggle;
    
    [Header("Visualization Settings")]
    public int textureWidth = 512;
    public int textureHeight = 256;
    public float updateRate = 30f; // FPS del espectrograma
    public float intensityMultiplier = 10f; // Aumentado para mejor visibilidad
    
    [Header("Frequency Settings")]
    public float minFrequency = 20f;
    public float maxFrequency = 8000f; // Reducido para mejor rango musical
    public bool logarithmicScale = true;
    
    // Colores más vibrantes para mejor visibilidad
    [Header("Color Settings")]
    public Color backgroundColor = Color.black;
    public Color lowIntensityColor = new Color(0, 0, 0.5f, 1f); // Azul oscuro
    public Color mediumIntensityColor = new Color(0, 1f, 0, 1f); // Verde
    public Color highIntensityColor = new Color(1f, 1f, 0, 1f); // Amarillo
    public Color maxIntensityColor = new Color(1f, 0, 0, 1f); // Rojo
    
    private Texture2D spectrogramTexture;
    private Color[] texturePixels;
    private AudioAnalyzer audioAnalyzer;
    private float lastUpdateTime;
    
    // Buffer circular para datos históricos
    private Queue<float[]> spectrumHistory;
    private int maxHistorySize;
    
    void Start()
    {
        Initialize();
        // NO configurar para pantalla completa
    }
    
    void Initialize()
    {
        // Configurar textura
        spectrogramTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        texturePixels = new Color[textureWidth * textureHeight];
        
        // Limpiar textura
        for (int i = 0; i < texturePixels.Length; i++)
        {
            texturePixels[i] = backgroundColor;
        }
        spectrogramTexture.SetPixels(texturePixels);
        spectrogramTexture.Apply();
        
        // Configurar UI
        if (spectrogramDisplay != null)
        {
            spectrogramDisplay.texture = spectrogramTexture;
        }
        
        // Buscar AudioAnalyzer
        audioAnalyzer = FindObjectOfType<AudioAnalyzer>();
        if (audioAnalyzer == null)
        {
            Debug.LogError("AudioAnalyzer no encontrado. Asegúrate de que exista en la escena.");
        }
        
        // Configurar historial
        maxHistorySize = textureWidth;
        spectrumHistory = new Queue<float[]>();
        
        SetupUI();
    }
    
    void SetupUI()
    {
        if (frequencyRangeSlider != null)
        {
            frequencyRangeSlider.onValueChanged.AddListener(UpdateFrequencyRange);
            frequencyRangeSlider.value = 0.5f; // Rango medio por defecto
        }
        
        if (showSpectrogramToggle != null)
        {
            showSpectrogramToggle.onValueChanged.AddListener(ToggleSpectrogram);
            showSpectrogramToggle.isOn = false; // Empezar desactivado
        }
        
        UpdateFrequencyRange(0.5f);
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime >= 1f / updateRate)
        {
            UpdateSpectrogram();
            lastUpdateTime = Time.time;
        }
    }
    
    void UpdateSpectrogram()
    {
        if (audioAnalyzer == null || spectrogramDisplay == null)
            return;
            
        // Solo actualizar si el toggle está activado Y el GameObject está activo
        if (showSpectrogramToggle != null && !showSpectrogramToggle.isOn)
            return;
            
        if (!gameObject.activeInHierarchy)
            return;
            
        // Obtener datos de espectro actuales
        float[] currentSpectrum = audioAnalyzer.GetCurrentSpectrum();
        if (currentSpectrum == null || currentSpectrum.Length == 0)
            return;
            
        // Agregar al historial
        AddToHistory(currentSpectrum);
        
        // Actualizar visualización
        UpdateTextureFromHistory();
    }
    
    void AddToHistory(float[] spectrum)
    {
        // Procesar espectro para el rango de frecuencias seleccionado
        float[] processedSpectrum = ProcessSpectrumForRange(spectrum);
        
        spectrumHistory.Enqueue(processedSpectrum);
        
        // Mantener tamaño del historial
        while (spectrumHistory.Count > maxHistorySize)
        {
            spectrumHistory.Dequeue();
        }
    }
    
    float[] ProcessSpectrumForRange(float[] originalSpectrum)
    {
        float[] processedSpectrum = new float[textureHeight];
        
        for (int y = 0; y < textureHeight; y++)
        {
            // Mapear índice Y a frecuencia
            float normalizedY = (float)y / textureHeight;
            float targetFrequency;
            
            if (logarithmicScale)
            {
                // Escala logarítmica
                float logMin = Mathf.Log10(minFrequency);
                float logMax = Mathf.Log10(maxFrequency);
                float logFreq = Mathf.Lerp(logMin, logMax, normalizedY);
                targetFrequency = Mathf.Pow(10f, logFreq);
            }
            else
            {
                // Escala lineal
                targetFrequency = Mathf.Lerp(minFrequency, maxFrequency, normalizedY);
            }
            
            // Convertir frecuencia a índice del espectro
            int spectrumIndex = FrequencyToSpectrumIndex(targetFrequency, originalSpectrum.Length);
            
            if (spectrumIndex >= 0 && spectrumIndex < originalSpectrum.Length)
            {
                processedSpectrum[y] = originalSpectrum[spectrumIndex] * intensityMultiplier;
            }
        }
        
        return processedSpectrum;
    }
    
    int FrequencyToSpectrumIndex(float frequency, int spectrumLength)
    {
        // Convertir frecuencia a índice del array de espectro
        // Asumiendo sample rate de 44100 Hz
        float sampleRate = 44100f;
        float frequencyResolution = sampleRate / (2f * spectrumLength);
        int index = Mathf.RoundToInt(frequency / frequencyResolution);
        
        return Mathf.Clamp(index, 0, spectrumLength - 1);
    }
    
    void UpdateTextureFromHistory()
    {
        // Desplazar contenido existente hacia la izquierda
        ShiftTextureLeft();
        
        // Agregar nueva columna de datos
        if (spectrumHistory.Count > 0)
        {
            float[] latestSpectrum = spectrumHistory.ToArray()[spectrumHistory.Count - 1];
            DrawSpectrumColumn(textureWidth - 1, latestSpectrum);
        }
        
        // Aplicar cambios a la textura
        spectrogramTexture.SetPixels(texturePixels);
        spectrogramTexture.Apply();
    }
    
    void ShiftTextureLeft()
    {
        // Mover todos los píxeles una columna hacia la izquierda
        for (int x = 0; x < textureWidth - 1; x++)
        {
            for (int y = 0; y < textureHeight; y++)
            {
                int sourceIndex = (y * textureWidth) + (x + 1);
                int destIndex = (y * textureWidth) + x;
                texturePixels[destIndex] = texturePixels[sourceIndex];
            }
        }
    }
    
    void DrawSpectrumColumn(int columnX, float[] spectrum)
    {
        for (int y = 0; y < textureHeight && y < spectrum.Length; y++)
        {
            int pixelIndex = (y * textureWidth) + columnX;
            
            // Convertir intensidad a color
            float intensity = Mathf.Clamp01(spectrum[y]);
            Color pixelColor = GetColorForIntensity(intensity);
            
            texturePixels[pixelIndex] = pixelColor;
        }
    }
    
    Color GetColorForIntensity(float intensity)
    {
        if (intensity <= 0f)
            return backgroundColor;
        
        // Sistema de colores por niveles para mejor visibilidad
        if (intensity < 0.25f)
        {
            return Color.Lerp(backgroundColor, lowIntensityColor, intensity * 4f);
        }
        else if (intensity < 0.5f)
        {
            return Color.Lerp(lowIntensityColor, mediumIntensityColor, (intensity - 0.25f) * 4f);
        }
        else if (intensity < 0.75f)
        {
            return Color.Lerp(mediumIntensityColor, highIntensityColor, (intensity - 0.5f) * 4f);
        }
        else
        {
            return Color.Lerp(highIntensityColor, maxIntensityColor, (intensity - 0.75f) * 4f);
        }
    }
    
    void UpdateFrequencyRange(float value)
    {
        // Mapear valor del slider a rango de frecuencias
        float totalRange = Mathf.Log10(maxFrequency) - Mathf.Log10(minFrequency);
        float rangeSize = totalRange * (0.2f + value * 0.8f); // 20% a 100% del rango total
        
        float centerLog = Mathf.Log10(minFrequency) + totalRange * 0.5f;
        float minLog = centerLog - rangeSize * 0.5f;
        float maxLog = centerLog + rangeSize * 0.5f;
        
        float newMinFreq = Mathf.Pow(10f, minLog);
        float newMaxFreq = Mathf.Pow(10f, maxLog);
        
        // Actualizar solo si es necesario
        if (Mathf.Abs(newMinFreq - minFrequency) > 1f || Mathf.Abs(newMaxFreq - maxFrequency) > 1f)
        {
            minFrequency = newMinFreq;
            maxFrequency = newMaxFreq;
        }
        
        // Actualizar texto
        if (frequencyRangeText != null)
        {
            frequencyRangeText.text = $"Rango: {minFrequency:F0} - {maxFrequency:F0} Hz";
        }
    }
    
    void ToggleSpectrogram(bool isEnabled)
    {
        if (spectrogramDisplay != null)
        {
            spectrogramDisplay.gameObject.SetActive(isEnabled);
        }
    }
    
    // Método público para limpiar el espectrograma
    public void ClearSpectrogram()
    {
        for (int i = 0; i < texturePixels.Length; i++)
        {
            texturePixels[i] = backgroundColor;
        }
        spectrogramTexture.SetPixels(texturePixels);
        spectrogramTexture.Apply();
        spectrumHistory.Clear();
    }
}