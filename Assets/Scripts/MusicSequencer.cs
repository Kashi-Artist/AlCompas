using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

[System.Serializable]
public class NoteData
{
    public string noteName;
    public float timestamp;
    public InstrumentType instrument;
    
    public NoteData(string note, float time, InstrumentType inst)
    {
        noteName = note;
        timestamp = time;
        instrument = inst;
    }
}

public class MusicSequencer : MonoBehaviour
{
    [Header("Sequence Settings")]
    public float bpm = 120f;
    public int quantizeSteps = 16;
    
    private List<NoteData> currentSequence;
    private AudioSource audioSource;
    private AudioAnalyzer audioAnalyzer;
    private float sequenceStartTime;
    private Coroutine playbackCoroutine;
    private bool isPlaying = false;
    
    public void Initialize()
    {
        currentSequence = new List<NoteData>();
        audioSource = GetComponent<AudioSource>();
        audioAnalyzer = FindObjectOfType<AudioAnalyzer>();
    }
    
    public void StartNewSequence()
    {
        // Detener cualquier reproducción en curso
        StopPlayback();
        currentSequence.Clear();
        sequenceStartTime = Time.time;
    }
    
    public void AddNote(string noteName, float timestamp, InstrumentType instrument)
    {
        float relativeTime = timestamp - sequenceStartTime;
        NoteData noteData = new NoteData(noteName, relativeTime, instrument);
        currentSequence.Add(noteData);
        
        Debug.Log($"Nota agregada: {instrument} - {noteName} en tiempo {relativeTime:F2}s");
    }
    
    public List<NoteData> GetCurrentSequence()
    {
        return new List<NoteData>(currentSequence);
    }
    
    public bool IsPlaying()
    {
        return isPlaying;
    }
    
    public void StopPlayback()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }
        isPlaying = false;
        
        // Detener cualquier audio que se esté reproduciendo
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        Debug.Log("Reproducción de secuencia detenida");
    }
    
    public void ExportSequence()
    {
        if (currentSequence.Count == 0)
        {
            Debug.LogWarning("No hay notas en la secuencia para exportar");
            return;
        }
        
        string fileName = $"MusicSequence_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        ExportAsTextFile(fileName);
        Debug.Log($"Secuencia exportada como: {fileName}.txt");
    }
    
    void ExportAsTextFile(string fileName)
    {
        StringBuilder content = new StringBuilder();
        content.AppendLine("=== SECUENCIA MUSICAL GENERADA ===");
        content.AppendLine($"BPM: {bpm}");
        content.AppendLine($"Fecha: {System.DateTime.Now}");
        content.AppendLine($"Total de notas: {currentSequence.Count}");
        content.AppendLine("");
        content.AppendLine("Tiempo(s)\tInstrumento\tNota");
        content.AppendLine("-------------------------------");
        
        foreach (var note in currentSequence)
        {
            content.AppendLine($"{note.timestamp:F2}\t{note.instrument}\t{note.noteName}");
        }
        
        string directory = Path.Combine(Application.persistentDataPath, "MusicSequences");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        string filePath = Path.Combine(directory, fileName + ".txt");
        File.WriteAllText(filePath, content.ToString());
        
        Debug.Log($"Archivo guardado en: {filePath}");
    }
    
    public void PlaySequence()
    {
        if (currentSequence.Count == 0)
        {
            Debug.LogWarning("No hay notas en la secuencia para reproducir");
            return;
        }
        
        // Detener cualquier reproducción previa
        StopPlayback();
        
        // Iniciar nueva reproducción
        playbackCoroutine = StartCoroutine(PlaySequenceCoroutine());
    }
    
    System.Collections.IEnumerator PlaySequenceCoroutine()
    {
        isPlaying = true;
        Debug.Log("Reproduciendo secuencia...");
        
        float lastTime = 0f;
        
        foreach (var noteData in currentSequence)
        {
            // Calcular tiempo de espera
            float waitTime = noteData.timestamp - lastTime;
            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }
            
            // Verificar si todavía estamos reproduciendo (por si se detuvo)
            if (!isPlaying)
            {
                yield break;
            }
            
            // Reproducir la nota
            if (audioAnalyzer != null)
            {
                Vector2 dummyPosition = Vector2.zero;
                audioAnalyzer.GenerateAndAnalyzeNote(noteData.instrument, dummyPosition);
            }
            
            Debug.Log($"Reproduciendo: {noteData.instrument} - {noteData.noteName} en {noteData.timestamp:F2}s");
            lastTime = noteData.timestamp;
        }
        
        isPlaying = false;
        playbackCoroutine = null;
        Debug.Log("Reproducción de secuencia completada");
    }
    
    public int GetSequenceLength()
    {
        return currentSequence.Count;
    }
    
    public void ClearSequence()
    {
        StopPlayback();
        currentSequence.Clear();
        Debug.Log("Secuencia limpia");
    }
}