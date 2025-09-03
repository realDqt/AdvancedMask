using UnityEngine;
using System.IO;
using System;
using System.Globalization;

public class Log : MonoBehaviour
{
    #region Singleton
    public static Log Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scene loads
            InitializeLoggers();
        }
    }
    #endregion

    [Header("Log Settings")]
    [Tooltip("The base path where log files will be saved.")]
    public string savePath = "E:\\UnityProjects\\9.2\\AdvancedMask\\Output";

    [Tooltip("How many times per second to log data automatically.")]
    public float logsPerSecond = 3f;

    // Internal state variables that other scripts will update
    private float _currentAngle = 0f;
    private float _currentPupilSize = 0f;
    private int _currentModelID = 0;
    private int _currentLightIntensity = 0;

    // File I/O
    private StreamWriter _continuousDataWriter;
    private StreamWriter _manualDataWriter;

    // Timing for continuous logging
    private float _logInterval;
    private float _timeSinceLastLog = 0f;

    private void InitializeLoggers()
    {
        // Ensure the directory exists
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string continuousLogPath = Path.Combine(savePath, $"ContinuousLog_{timestamp}.csv");
        string manualLogPath = Path.Combine(savePath, $"ManualLog_{timestamp}.csv");
        string header = "Timestamp,Angle,PupilSize,ModelID,LightIntensity";

        try
        {
            _continuousDataWriter = new StreamWriter(continuousLogPath, false, System.Text.Encoding.UTF8);
            _continuousDataWriter.WriteLine(header);
            _continuousDataWriter.Flush();
            Debug.Log($"[Log] Continuous data will be saved to: {continuousLogPath}");

            _manualDataWriter = new StreamWriter(manualLogPath, false, System.Text.Encoding.UTF8);
            _manualDataWriter.WriteLine(header);
            _manualDataWriter.Flush();
            Debug.Log($"[Log] Manual events will be saved to: {manualLogPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Log] Failed to create log files: {e.Message}");
            return;
        }

        if (logsPerSecond > 0)
        {
            _logInterval = 1f / logsPerSecond;
        }
    }

    private void Update()
    {
        // Handle continuous logging
        if (_continuousDataWriter == null || logsPerSecond <= 0) return;

        _timeSinceLastLog += Time.deltaTime;
        if (_timeSinceLastLog >= _logInterval)
        {
            _timeSinceLastLog -= _logInterval;
            WriteLogEntry(_continuousDataWriter);
        }
    }

    /// <summary>
    /// Writes a single log entry to the specified writer.
    /// </summary>
    private void WriteLogEntry(StreamWriter writer)
    {
        if (writer == null) return;
        
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        
        // Use InvariantCulture to ensure '.' is used as the decimal separator
        string angleStr = _currentAngle.ToString(CultureInfo.InvariantCulture);
        string pupilSizeStr = _currentPupilSize.ToString(CultureInfo.InvariantCulture);

        string logEntry = $"{timestamp},{angleStr},{pupilSizeStr},{_currentModelID},{_currentLightIntensity}";
        
        writer.WriteLine(logEntry);
        writer.Flush();
    }

    #region Public Update Methods
    
    /// <summary>
    /// Updates the current angle to be logged.
    /// </summary>
    public void UpdateAngle(float newAngle)
    {
        _currentAngle = newAngle;
    }

    /// <summary>
    /// Updates the current pupil size (radius in mm) to be logged.
    /// </summary>
    public void UpdatePupilSize(float newPupilSize)
    {
        _currentPupilSize = newPupilSize;
    }

    /// <summary>
    /// Updates the current model ID to be logged.
    /// </summary>
    public void UpdateModelID(int newModelID)
    {
        _currentModelID = newModelID;
    }

    /// <summary>
    /// Updates the current light intensity value to be logged.
    /// </summary>
    public void UpdateLightIntensity(int newLightIntensity)
    {
        _currentLightIntensity = newLightIntensity;
    }

    /// <summary>
    /// Logs the current state as a manual event. Call this on spacebar press.
    /// </summary>
    public void LogManualEvent()
    {
        Debug.Log("[Log] Manual event recorded.");
        WriteLogEntry(_manualDataWriter);
    }

    #endregion

    private void OnDestroy()
    {
        Cleanup();
    }

    private void OnApplicationQuit()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        Debug.Log("[Log] Closing log files.");
        _continuousDataWriter?.Close();
        _manualDataWriter?.Close();
    }
}