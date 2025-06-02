using UnityEngine;
using System.Collections.Generic;

public class AudioAnalyzer : MonoBehaviour
{
    [Header("Analysis Settings")]
    public int sampleRate = 44100;
    public int fftSize = 1024;
    public float analysisTime = 0.5f;
    
    [Header("Audio Components")]
    public AudioSource audioSource;
    
    private float[] spectrumData;
    private float[] waveformData;
    private NoteDetector noteDetector;
    
    // Frecuencias de notas musicales (Hz)
    private Dictionary<string, float> noteFrequencies;
    
    void Awake()
    {
        spectrumData = new float[fftSize];
        waveformData = new float[fftSize];
        noteDetector = GetComponent<NoteDetector>();
        
        InitializeNoteFrequencies();
    }
    
    public void Initialize()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        if (noteDetector == null)
            noteDetector = gameObject.AddComponent<NoteDetector>();
    }
    
    void InitializeNoteFrequencies()
    {
        noteFrequencies = new Dictionary<string, float>();
        
        // Octava 4 (notas principales)
        noteFrequencies["C4"] = 261.63f;
        noteFrequencies["C#4"] = 277.18f;
        noteFrequencies["D4"] = 293.66f;
        noteFrequencies["D#4"] = 311.13f;
        noteFrequencies["E4"] = 329.63f;
        noteFrequencies["F4"] = 349.23f;
        noteFrequencies["F#4"] = 369.99f;
        noteFrequencies["G4"] = 392.00f;
        noteFrequencies["G#4"] = 415.30f;
        noteFrequencies["A4"] = 440.00f;
        noteFrequencies["A#4"] = 466.16f;
        noteFrequencies["B4"] = 493.88f;
        
        // Agregar más octavas si es necesario
        AddOctaveNotes(3, 0.5f);  // Octava 3 (más grave)
        AddOctaveNotes(5, 2f);    // Octava 5 (más agudo)
    }
    
    void AddOctaveNotes(int octave, float multiplier)
    {
        string[] baseNotes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        
        foreach (string note in baseNotes)
        {
            string baseKey = note + "4";
            if (noteFrequencies.ContainsKey(baseKey))
            {
                string newKey = note + octave;
                noteFrequencies[newKey] = noteFrequencies[baseKey] * multiplier;
            }
        }
    }
    
    public string GenerateAndAnalyzeNote(InstrumentType instrumentType, Vector2 collisionPoint)
    {
        // Generar frecuencia basada en la posición de colisión
        float frequency = CalculateFrequencyFromPosition(instrumentType, collisionPoint);
        
        // Sintetizar y reproducir el sonido
        GenerateAndPlayTone(frequency, instrumentType);
        
        // Analizar y detectar la nota
        return noteDetector.DetectNoteFromFrequency(frequency);
    }
    
    float CalculateFrequencyFromPosition(InstrumentType instrumentType, Vector2 position)
    {
        InstrumentBlock dummyBlock = new GameObject().AddComponent<InstrumentBlock>();
        dummyBlock.SetInstrumentType(instrumentType);
        Vector2 freqRange = dummyBlock.GetFrequencyRange();
        Destroy(dummyBlock.gameObject);
        
        // Mapear posición Y a frecuencia (más alto = más agudo)
        float normalizedY = Mathf.InverseLerp(-10f, 10f, position.y);
        float frequency = Mathf.Lerp(freqRange.x, freqRange.y, normalizedY);
        
        // Agregar algo de aleatoriedad
        frequency *= Random.Range(0.9f, 1.1f);
        
        return frequency;
    }
    
    void GenerateAndPlayTone(float frequency, InstrumentType instrumentType)
    {
        StartCoroutine(PlaySynthesizedTone(frequency, instrumentType, 0.5f));
    }
    
    System.Collections.IEnumerator PlaySynthesizedTone(float frequency, InstrumentType instrumentType, float duration)
    {
        // Crear AudioClip sintético
        AudioClip synthesizedClip = CreateSynthesizedAudio(frequency, instrumentType, duration);
        
        // Reproducir
        audioSource.clip = synthesizedClip;
        audioSource.Play();
        
        // Esperar y analizar
        yield return new WaitForSeconds(0.1f);
        AnalyzeCurrentAudio();
        
        yield return new WaitForSeconds(duration - 0.1f);
    }
    
    AudioClip CreateSynthesizedAudio(float frequency, InstrumentType instrumentType, float duration)
    {
        int sampleCount = Mathf.RoundToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];
        
        // Obtener armónicos del instrumento
        InstrumentBlock dummyBlock = new GameObject().AddComponent<InstrumentBlock>();
        dummyBlock.SetInstrumentType(instrumentType);
        float[] harmonics = dummyBlock.GetInstrumentHarmonics();
        Destroy(dummyBlock.gameObject);
        
        // Generar forma de onda con armónicos
        for (int i = 0; i < sampleCount; i++)
        {
            float sample = 0f;
            float time = (float)i / sampleRate;
            
            // Sumar armónicos
            for (int h = 0; h < harmonics.Length; h++)
            {
                float harmonicFreq = frequency * (h + 1);
                float amplitude = harmonics[h];
                sample += amplitude * Mathf.Sin(2 * Mathf.PI * harmonicFreq * time);
            }
            
            // Aplicar envolvente ADSR simple
            float envelope = CalculateEnvelope(time, duration);
            samples[i] = sample * envelope * 0.1f; // Volumen bajo
        }
        
        AudioClip clip = AudioClip.Create("SynthesizedTone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    
    float CalculateEnvelope(float time, float duration)
    {
        float attack = 0.1f;
        float decay = 0.2f;
        float sustain = 0.7f;
        float release = duration * 0.3f;
        
        if (time < attack)
            return time / attack;
        else if (time < attack + decay)
            return 1f - ((time - attack) / decay) * (1f - sustain);
        else if (time < duration - release)
            return sustain;
        else
            return sustain * (duration - time) / release;
    }
    
    void AnalyzeCurrentAudio()
    {
        if (audioSource.isPlaying)
        {
            // Obtener datos de espectro
            audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);
            
            // Encontar frecuencia dominante
            float dominantFrequency = GetDominantFrequency();
            
            Debug.Log($"Frecuencia dominante detectada: {dominantFrequency:F2} Hz");
        }
    }
    
    float GetDominantFrequency()
    {
        float maxAmplitude = 0f;
        int maxIndex = 0;
        
        for (int i = 1; i < spectrumData.Length; i++)
        {
            if (spectrumData[i] > maxAmplitude)
            {
                maxAmplitude = spectrumData[i];
                maxIndex = i;
            }
        }
        
        // Convertir índice a frecuencia
        float frequency = maxIndex * (sampleRate / 2f) / spectrumData.Length;
        return frequency;
    }
    
    public float[] GetCurrentSpectrum()
    {
        if (audioSource.isPlaying)
        {
            audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);
        }
        return spectrumData;
    }
    
    public string GetClosestNote(float frequency)
    {
        string closestNote = "C4";
        float minDifference = float.MaxValue;
        
        foreach (var kvp in noteFrequencies)
        {
            float difference = Mathf.Abs(frequency - kvp.Value);
            if (difference < minDifference)
            {
                minDifference = difference;
                closestNote = kvp.Key;
            }
        }
        
        return closestNote;
    }
}