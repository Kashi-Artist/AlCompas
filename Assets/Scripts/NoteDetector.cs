using UnityEngine;
using System.Collections.Generic;

public class NoteDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    public float tolerancePercentage = 5f; // Tolerancia para detección de notas
    public int octaveRange = 8; // Rango de octavas a considerar
    
    private Dictionary<string, float> allNoteFrequencies;
    private string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    
    void Start()
    {
        InitializeAllNotes();
    }
    
    void InitializeAllNotes()
    {
        allNoteFrequencies = new Dictionary<string, float>();
        
        // Generar todas las notas desde C0 hasta C8
        for (int octave = 0; octave <= octaveRange; octave++)
        {
            for (int noteIndex = 0; noteIndex < noteNames.Length; noteIndex++)
            {
                string noteName = noteNames[noteIndex] + octave;
                float frequency = CalculateNoteFrequency(noteIndex, octave);
                allNoteFrequencies[noteName] = frequency;
            }
        }
        
        Debug.Log($"Inicializadas {allNoteFrequencies.Count} notas para detección");
    }
    
    float CalculateNoteFrequency(int noteIndex, int octave)
    {
        // Fórmula: f = 440 * 2^((n-69)/12)
        // Donde n es el número MIDI de la nota
        // A4 (440 Hz) es la nota MIDI 69
        
        int midiNumber = (octave + 1) * 12 + noteIndex;
        if (noteIndex >= 3) midiNumber -= 12; // Ajuste para que C sea el inicio de la octava
        
        float frequency = 440f * Mathf.Pow(2f, (midiNumber - 69f) / 12f);
        return frequency;
    }
    
    public string DetectNoteFromFrequency(float inputFrequency)
    {
        if (inputFrequency <= 0) return "Unknown";
        
        string closestNote = "C4";
        float smallestDifference = float.MaxValue;
        
        foreach (var kvp in allNoteFrequencies)
        {
            float noteFrequency = kvp.Value;
            float difference = Mathf.Abs(inputFrequency - noteFrequency);
            
            // Calcular porcentaje de diferencia
            float percentageDifference = (difference / noteFrequency) * 100f;
            
            if (percentageDifference <= tolerancePercentage && difference < smallestDifference)
            {
                smallestDifference = difference;
                closestNote = kvp.Key;
            }
        }
        
        return closestNote;
    }
    
    public string DetectNoteFromSpectrum(float[] spectrum, int sampleRate)
    {
        float dominantFrequency = GetDominantFrequencyFromSpectrum(spectrum, sampleRate);
        return DetectNoteFromFrequency(dominantFrequency);
    }
    
    float GetDominantFrequencyFromSpectrum(float[] spectrum, int sampleRate)
    {
        float maxAmplitude = 0f;
        int maxIndex = 0;
        
        // Buscar el pico más alto en el espectro
        for (int i = 1; i < spectrum.Length / 2; i++) // Solo la mitad positiva del espectro
        {
            if (spectrum[i] > maxAmplitude)
            {
                maxAmplitude = spectrum[i];
                maxIndex = i;
            }
        }
        
        // Convertir índice a frecuencia
        float frequencyResolution = (float)sampleRate / (spectrum.Length * 2);
        float dominantFrequency = maxIndex * frequencyResolution;
        
        return dominantFrequency;
    }
    
    public List<string> DetectMultipleNotes(float[] spectrum, int sampleRate, float threshold = 0.1f)
    {
        List<string> detectedNotes = new List<string>();
        List<float> peakFrequencies = FindSpectralPeaks(spectrum, sampleRate, threshold);
        
        foreach (float frequency in peakFrequencies)
        {
            string note = DetectNoteFromFrequency(frequency);
            if (!detectedNotes.Contains(note))
            {
                detectedNotes.Add(note);
            }
        }
        
        return detectedNotes;
    }
    
    List<float> FindSpectralPeaks(float[] spectrum, int sampleRate, float threshold)
    {
        List<float> peaks = new List<float>();
        float frequencyResolution = (float)sampleRate / (spectrum.Length * 2);
        
        // Encontrar picos locales
        for (int i = 2; i < spectrum.Length / 2 - 2; i++)
        {
            float currentAmplitude = spectrum[i];
            
            // Verificar si es un pico local y supera el umbral
            if (currentAmplitude > threshold &&
                currentAmplitude > spectrum[i - 1] &&
                currentAmplitude > spectrum[i + 1] &&
                currentAmplitude > spectrum[i - 2] &&
                currentAmplitude > spectrum[i + 2])
            {
                float frequency = i * frequencyResolution;
                peaks.Add(frequency);
            }
        }
        
        return peaks;
    }
    
    public float GetNoteFrequency(string noteName)
    {
        if (allNoteFrequencies.ContainsKey(noteName))
        {
            return allNoteFrequencies[noteName];
        }
        
        Debug.LogWarning($"Nota no encontrada: {noteName}");
        return 440f; // A4 por defecto
    }
    
    public string GetNoteInfo(string noteName)
    {
        if (allNoteFrequencies.ContainsKey(noteName))
        {
            float frequency = allNoteFrequencies[noteName];
            return $"{noteName}: {frequency:F2} Hz";
        }
        
        return $"{noteName}: Frecuencia desconocida";
    }
    
    public bool IsValidNote(string noteName)
    {
        return allNoteFrequencies.ContainsKey(noteName);
    }
    
    public string GetNearestNote(float frequency, out float cents)
    {
        string nearestNote = DetectNoteFromFrequency(frequency);
        float nearestFrequency = GetNoteFrequency(nearestNote);
        
        // Calcular diferencia en cents (1 semitono = 100 cents)
        cents = 1200f * Mathf.Log(frequency / nearestFrequency, 2f);
        
        return nearestNote;
    }
    
    public int GetMIDINoteNumber(string noteName)
    {
        if (!IsValidNote(noteName))
            return -1;
            
        // Extraer octava y nombre de nota
        string noteOnly = noteName.Substring(0, noteName.Length - 1);
        int octave = int.Parse(noteName.Substring(noteName.Length - 1));
        
        // Encontrar índice de la nota
        int noteIndex = System.Array.IndexOf(noteNames, noteOnly);
        if (noteIndex == -1)
            return -1;
            
        // Calcular número MIDI
        int midiNumber = (octave + 1) * 12 + noteIndex;
        if (noteIndex >= 3) midiNumber -= 12;
        
        return midiNumber;
    }
    
    public void SetTolerance(float newTolerance)
    {
        tolerancePercentage = Mathf.Clamp(newTolerance, 0.1f, 50f);
    }
    
    // Método para debugging - mostrar todas las notas detectables
    public void PrintAllNotes()
    {
        Debug.Log("=== TODAS LAS NOTAS DETECTABLES ===");
        foreach (var kvp in allNoteFrequencies)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value:F2} Hz");
        }
    }
}