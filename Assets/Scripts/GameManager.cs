using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Game Objects")]
    public GameObject ballPrefab;
    public GameObject instrumentBlockPrefab;
    public Transform ballSpawnPoint;
    
    [Header("UI Elements")]
    public Button launchButton;
    public Button exportButton;
    public Button playSequenceButton;
    public Button showSequenceButton; // Nuevo botón para mostrar/ocultar secuencia
    public Slider velocitySlider;
    public TextMeshProUGUI velocityText;
    public GameObject gameUIPanel;
    public GameObject spectrumPanel;
    
    [Header("Sequence Display")]
    public GameObject sequenceScrollViewObject; // El ScrollView completo
    public TextMeshProUGUI sequenceNotesText;
    public ScrollRect sequenceScrollRect;
    
    [Header("Game Duration Settings")]
    public Slider gameDurationSlider;
    public TextMeshProUGUI gameDurationText;
    public float minGameDuration = 10f;
    public float maxGameDuration = 30f;
    
    [Header("Wall Sound Settings")]
    public TMP_Dropdown wallSoundDropdown;
    public InstrumentType selectedWallInstrument = InstrumentType.Drums;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioAnalyzer audioAnalyzer;
    public MusicSequencer musicSequencer;
    
    [Header("Game Settings")]
    public float maxLaunchForce = 20f;
    public LayerMask wallLayer;
    
    private GameObject currentBall;
    private bool gameInProgress = false;
    private Camera mainCamera;
    private float gameStartTime;
    private SpectrogramVisualizer spectrogramVisualizer;
    private float currentGameDuration;
    private bool sequenceViewVisible = false;
    
    // Modo de construcción
    public bool buildMode = true;
    public InstrumentType selectedInstrument = InstrumentType.Piano;
    
    void Start()
    {
        mainCamera = Camera.main;
        spectrogramVisualizer = FindObjectOfType<SpectrogramVisualizer>();
        SetupUI();
        SetupWallSoundDropdown();
        SetupGameDurationSlider();
        audioAnalyzer.Initialize();
        musicSequencer.Initialize();
        
        SetupSpectrogram();
        SetupSequenceView();
        UpdateSequenceDisplay();
    }
    
    void SetupSpectrogram()
    {
        if (spectrumPanel != null)
        {
            spectrumPanel.SetActive(false);
        }
    }
    
    void SetupSequenceView()
    {
        // Inicialmente ocultar el ScrollView de secuencia
        if (sequenceScrollViewObject != null)
        {
            sequenceScrollViewObject.SetActive(false);
        }
    }
    
    void SetupUI()
    {
        launchButton.onClick.AddListener(LaunchBall);
        exportButton.onClick.AddListener(ExportSequence);
        playSequenceButton.onClick.AddListener(PlaySequence);
        velocitySlider.onValueChanged.AddListener(UpdateVelocityText);
        
        // Configurar botón de secuencia
        if (showSequenceButton != null)
        {
            showSequenceButton.onClick.AddListener(ToggleSequenceView);
        }
        
        UpdateVelocityText(velocitySlider.value);
    }
    
    void ToggleSequenceView()
    {
        sequenceViewVisible = !sequenceViewVisible;
        if (sequenceScrollViewObject != null)
        {
            sequenceScrollViewObject.SetActive(sequenceViewVisible);
            
            // Si se activa, actualizar el display y hacer scroll al final
            if (sequenceViewVisible)
            {
                UpdateSequenceDisplay();
                StartCoroutine(ScrollToBottomDelayed());
            }
        }
        
        // Actualizar texto del botón
        if (showSequenceButton != null)
        {
            TextMeshProUGUI buttonText = showSequenceButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = sequenceViewVisible ? "Ocultar Secuencia" : "Mostrar Secuencia";
            }
        }
    }
    
    void SetupGameDurationSlider()
    {
        if (gameDurationSlider != null)
        {
            gameDurationSlider.minValue = minGameDuration;
            gameDurationSlider.maxValue = maxGameDuration;
            gameDurationSlider.value = (minGameDuration + maxGameDuration) / 2f;
            gameDurationSlider.onValueChanged.AddListener(UpdateGameDurationText);
            
            UpdateGameDurationText(gameDurationSlider.value);
        }
    }
    
    void UpdateGameDurationText(float value)
    {
        currentGameDuration = value;
        if (gameDurationText != null)
        {
            gameDurationText.text = $"Duración: {value:F0}s";
        }
    }
    
    void SetupWallSoundDropdown()
    {
        if (wallSoundDropdown != null)
        {
            wallSoundDropdown.ClearOptions();
            
            List<string> instrumentNames = new List<string>();
            foreach (InstrumentType instrument in System.Enum.GetValues(typeof(InstrumentType)))
            {
                instrumentNames.Add(instrument.ToString());
            }
            
            wallSoundDropdown.AddOptions(instrumentNames);
            wallSoundDropdown.value = (int)selectedWallInstrument;
            wallSoundDropdown.onValueChanged.AddListener(OnWallSoundChanged);
        }
    }
    
    void OnWallSoundChanged(int index)
    {
        selectedWallInstrument = (InstrumentType)index;
    }
    
    void Update()
    {
        HandleInput();
        
        if (gameInProgress)
        {
            float elapsedTime = Time.time - gameStartTime;
            if (elapsedTime >= currentGameDuration)
            {
                EndGame();
            }
        }
    }
    
    void HandleInput()
    {
        if (buildMode && Input.GetMouseButtonDown(0))
        {
            PlaceInstrumentBlock();
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleBuildMode();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameInProgress)
            {
                EndGame();
            }
            else
            {
                ReturnToMainMenu();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedInstrument = InstrumentType.Piano;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedInstrument = InstrumentType.Guitar;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedInstrument = InstrumentType.Drums;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedInstrument = InstrumentType.Violin;
    }
    
    void PlaceInstrumentBlock()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
        
        GameObject newBlock = Instantiate(instrumentBlockPrefab, worldPos, Quaternion.identity);
        InstrumentBlock blockScript = newBlock.GetComponent<InstrumentBlock>();
        blockScript.SetInstrumentType(selectedInstrument);
    }
    
    void ToggleBuildMode()
    {
        buildMode = !buildMode;
        launchButton.interactable = !buildMode;
    }
    
    public void LaunchBall()
    {
        if (gameInProgress || buildMode) return;
        
        // Detener cualquier reproducción de secuencia en curso
        StopSequencePlayback();
        
        if (spectrumPanel != null)
        {
            spectrumPanel.SetActive(true);
            StartCoroutine(FadeInSpectrum());
        }
        
        if (gameUIPanel != null)
            gameUIPanel.SetActive(false);
        
        float force = velocitySlider.value * maxLaunchForce;
        Vector2 direction = GetLaunchDirection();
        
        currentBall = Instantiate(ballPrefab, ballSpawnPoint.position, Quaternion.identity);
        BallController ballController = currentBall.GetComponent<BallController>();
        ballController.Launch(direction * force, this);
        
        gameInProgress = true;
        gameStartTime = Time.time;
        musicSequencer.StartNewSequence();
        
        if (spectrogramVisualizer != null)
        {
            spectrogramVisualizer.ClearSpectrogram();
        }
        
        UpdateSequenceDisplay();
    }
    
    IEnumerator FadeInSpectrum()
    {
        if (spectrumPanel != null)
        {
            CanvasGroup canvasGroup = spectrumPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = spectrumPanel.AddComponent<CanvasGroup>();
            }
            
            canvasGroup.alpha = 0f;
            float fadeTime = 0.5f;
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeTime);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
    }
    
    Vector2 GetLaunchDirection()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
    
    public void OnBallCollision(InstrumentBlock block, Vector2 collisionPoint)
    {
        if (!gameInProgress) return;
        
        string note = audioAnalyzer.GenerateAndAnalyzeNote(block.instrumentType, collisionPoint);
        musicSequencer.AddNote(note, Time.time, block.instrumentType);
        
        UpdateSequenceDisplay();
        
        Debug.Log($"Colisión detectada: {block.instrumentType} - Nota: {note}");
    }
    
    public void OnBallWallCollision(Vector2 collisionPoint)
    {
        if (!gameInProgress) return;
        
        string note = audioAnalyzer.GenerateAndAnalyzeNote(selectedWallInstrument, collisionPoint);
        musicSequencer.AddNote(note, Time.time, selectedWallInstrument);
        
        UpdateSequenceDisplay();
        
        Debug.Log($"Colisión con pared: {selectedWallInstrument} - Nota: {note}");
    }
    
    public void UpdateSequenceDisplay()
    {
        if (sequenceNotesText == null) return;
        
        List<NoteData> currentSequence = musicSequencer.GetCurrentSequence();
        
        if (currentSequence.Count == 0)
        {
            sequenceNotesText.text = "=== SECUENCIA DE NOTAS ===\n\nNo hay notas registradas.\nJuega para generar música!";
            return;
        }
        
        System.Text.StringBuilder displayText = new System.Text.StringBuilder();
        displayText.AppendLine("=== SECUENCIA DE NOTAS ===");
        displayText.AppendLine($"Total: {currentSequence.Count} notas");
        displayText.AppendLine("");
        displayText.AppendLine("TIEMPO    NOTA      INSTRUMENTO");
        displayText.AppendLine("────────────────────────────────");
        
        foreach (var note in currentSequence)
        {
            string timeStr = note.timestamp.ToString("F1").PadRight(8);
            string noteStr = note.noteName.PadRight(8);
            string instrumentStr = note.instrument.ToString();
            
            displayText.AppendLine($"{timeStr}  {noteStr}  {instrumentStr}");
        }
        
        sequenceNotesText.text = displayText.ToString();
        
        // Forzar actualización del layout
        if (sequenceScrollRect != null && sequenceViewVisible)
        {
            StartCoroutine(ScrollToBottomDelayed());
        }
    }
    
    IEnumerator ScrollToBottomDelayed()
    {
        // Esperar 2 frames para que el layout se actualice completamente
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (sequenceScrollRect != null)
        {
            // Forzar recálculo del contenido
            LayoutRebuilder.ForceRebuildLayoutImmediate(sequenceScrollRect.content);
            sequenceScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    public void EndGame()
    {
        gameInProgress = false;
        
        if (gameUIPanel != null)
            gameUIPanel.SetActive(true);
        
        if (spectrumPanel != null)
            spectrumPanel.SetActive(false);
        
        exportButton.interactable = true;
        playSequenceButton.interactable = true;
        
        UpdateSequenceDisplay();
        
        if (currentBall != null)
        {
            Destroy(currentBall);
        }
    }
    
    void ReturnToMainMenu()
    {
        StopSequencePlayback();
        SceneManager.LoadScene("MainMenu");
    }
    
    void UpdateVelocityText(float value)
    {
        velocityText.text = $"Velocidad: {value:F1}";
    }
    
    void ExportSequence()
    {
        musicSequencer.ExportSequence();
        UpdateSequenceDisplay();
    }
    
    void PlaySequence()
    {
        // Solo permitir reproducir si no estamos en juego
        if (gameInProgress)
        {
            Debug.Log("No se puede reproducir secuencia durante el juego");
            return;
        }
        
        // Si ya se está reproduciendo, detener
        if (musicSequencer.IsPlaying())
        {
            StopSequencePlayback();
        }
        else
        {
            musicSequencer.PlaySequence();
        }
    }
    
    void StopSequencePlayback()
    {
        if (musicSequencer != null)
        {
            musicSequencer.StopPlayback();
        }
    }
}