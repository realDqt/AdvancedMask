using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Globalization;
using UnityEngine;

// NetMQ and MessagePack for Pupil Core
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

// Thorlabs ELL14 for Motor Control
using Thorlabs.Elliptec.ELLO_DLL;

public class MotorControlAndDataLogger : MonoBehaviour
{
    [Header("Motor Settings")]
    public string portName = "COM7";
    public char minAddress = '0';
    public char maxAddress = '1';
    [Tooltip("Initial angle offset for motor position")]
    public float initialAngleOffset = 102f;

    [Header("Pupil Core Settings")]
    public string pupilRemoteAddress = "127.0.0.1";
    public string pupilReqPort = "50020";

    [Header("Data Logging")]
    [Tooltip("Number of logs per second")]
    public float logsPerSecond = 3f;

    [Header("Pupil-Controlled Motor")]
    [Tooltip("Minimum angle change required to move motor")]
    public float minimumAngleChangeToMove = 0.5f;
    [Tooltip("Interval (seconds) to check pupil data and update motor")]
    public float pupilCheckInterval = 2f;

    [Tooltip("Minimum log luminance (log10 cd/m^2), e.g. -3 for 0.001 cd/m^2")]
    public float minLogLuminance = -3f;
    [Tooltip("Maximum log luminance (log10 cd/m^2), e.g. 4 for 10,000 cd/m^2")]
    public float maxLogLuminance = 4f;

    [Tooltip("Motor angle for low luminance")]
    public float motorAngleForLowLuminance = 90f;
    [Tooltip("Motor angle for high luminance")]
    public float motorAngleForHighLuminance = 0f;

    
    public float coarseAdjustmentDegree = 10.0f;
    public float fineAdjustmentDegree = 5.0f;

    public int coarseSpeedPercent = 60;
    public int fineSpeedPercent = 30;
    
    public int maxUserCount = 100;

    public string finalDegreeSavePath = "D:\\DALAB\\Research\\AdvancedMask\\Output\\test.csv";

    private int curUserId = 0;
    private float[] finalDegrees;
    
    

    // Motor control
    private ELLDevices _mgr;
    private ELLDevice _dev;
    private bool _motorConnected = false;

    // Pupil data
    private RequestSocket _pupilReqSocket;
    private SubscriberSocket _pupilSubSocket;
    private Thread _pupilSubThread;
    private volatile bool _isPupilThreadRunning = false;
    private volatile float _currentPupilRadius = 0.0f; // in mm

    // Data logging
    private StreamWriter _continuousDataWriter;
    private StreamWriter _manualDataWriter;
    private float _logInterval;
    private float _timeSinceLastLog = 0f;
    private decimal _lastCalculatedTargetAngle = 0m;
    private float _lastEstimatedLuminance = 0f;

    // Pupil control
    private float _timeSinceLastPupilCheck = 0f;

    #region Unity Lifecycle Methods

    void Start()
    {
        if (!InitializeMotor())
        {
            Debug.LogError("ELL14: connect failed, abort auto run.");
        }
        InitializePupilSubscriber();
        InitializeDataLoggers();
        if (logsPerSecond > 0)
        {
            _logInterval = 1f / logsPerSecond;
        }
        
        finalDegrees = new float[maxUserCount];
        for (int i = 0; i < maxUserCount; i++)
            finalDegrees[i] = initialAngleOffset;
    }

    void Update()
    {
        // Manual motor control is disabled; only pupil-driven control is active
        HandleMotorInput(); 
        HandleContinuousLogging();
        HandleManualLogging();
        //HandlePupilMotorControl();
    }

    void OnDestroy() => Cleanup();
    void OnApplicationQuit() => Cleanup();

    #endregion

    #region Initialization and Cleanup

    private bool InitializeMotor()
    {
        if (_motorConnected) return true;
        _mgr = new ELLDevices();
        if (!ELLDevicePort.Connect(portName))
        {
            Debug.LogError($"[Motor] Failed to open port {portName}.");
            return false;
        }
        try
        {
            List<string> devices = _mgr.ScanAddresses(minAddress, maxAddress);
            foreach (string devStr in devices)
            {
                if (!_mgr.Configure(devStr)) continue;
                var d = _mgr.AddressedDevice(devStr[0]) as ELLDevice;
                if (d == null) continue;

                _dev = d;

                decimal initialAngle = (decimal)initialAngleOffset;
                _dev.MoveAbsolute(initialAngle);
                _lastCalculatedTargetAngle = initialAngle;
                Debug.Log($"[Motor] Moved to initial angle: {initialAngleOffset} degrees.");
                Thread.Sleep(600);

                _motorConnected = true;

                foreach (string line in _dev.DeviceInfo.Description())
                    Debug.Log($"ELL14 {line}");
                Debug.Log($"ELL14 connected on {portName}, addr {_dev.DeviceInfo.Address}");
                return true;
            }
            Debug.LogError("ELL14: no device found on bus.");
            ELLDevicePort.Disconnect();
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError("ELL14 connect error: " + ex.Message);
            try { ELLDevicePort.Disconnect(); } catch { }
            _motorConnected = false;
            _dev = null;
            _mgr = null;
            return false;
        }
    }

    private void SaveFinalDegreesToDisk(string filepath)
    {
        if (finalDegrees == null || finalDegrees.Length == 0) return;

        using (var writer = new StreamWriter(filepath, false, System.Text.Encoding.UTF8))
        {
            // table's title
            writer.WriteLine("UserID,Degree");

            for (int i = 0; i < finalDegrees.Length; i++)
            {
                writer.WriteLine($"{i},{finalDegrees[i].ToString(CultureInfo.InvariantCulture)}");
            }
        }
    }

    private void InitializePupilSubscriber()
    {
        AsyncIO.ForceDotNet.Force();
        try
        {
            _pupilReqSocket = new RequestSocket();
            _pupilReqSocket.Connect($"tcp://{pupilRemoteAddress}:{pupilReqPort}");
            _pupilReqSocket.SendFrame("SUB_PORT");
            string subPort = _pupilReqSocket.ReceiveFrameString();
            Debug.Log($"[Pupil] SUB_PORT = {subPort}");
            _pupilSubSocket = new SubscriberSocket();
            _pupilSubSocket.Connect($"tcp://{pupilRemoteAddress}:{subPort}");
            _pupilSubSocket.Subscribe("pupil.");
            _isPupilThreadRunning = true;
            _pupilSubThread = new Thread(SubscriberLoop);
            _pupilSubThread.IsBackground = true;
            _pupilSubThread.Start();
            Debug.Log("[Pupil] Subscriber thread started.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Pupil] Initialization failed: {e.Message}");
        }
    }

    private void InitializeDataLoggers()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string continuousLogPath = Path.Combine(Application.persistentDataPath, $"ContinuousLog_{timestamp}.csv");
        string manualLogPath = Path.Combine(Application.persistentDataPath, $"ManualLog_{timestamp}.csv");

        string header = "Timestamp,CalculatedTargetAngle,ActualMotorAngle,PupilRadius,EstimatedLuminance(cd/m^2)";

        try
        {
            _continuousDataWriter = new StreamWriter(continuousLogPath, true);
            _continuousDataWriter.WriteLine(header);
            _continuousDataWriter.Flush();
            Debug.Log($"[Logger] Continuous data will be saved to: {continuousLogPath}");

            _manualDataWriter = new StreamWriter(manualLogPath, true);
            _manualDataWriter.WriteLine(header);
            _manualDataWriter.Flush();
            Debug.Log($"[Logger] Manual events will be saved to: {manualLogPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Logger] Failed to create log files: {e.Message}");
        }
    }

    private void Cleanup()
    {
        if (_isPupilThreadRunning)
        {
            _isPupilThreadRunning = false;
            _pupilSubThread?.Join();
            Debug.Log("[Pupil] Subscriber thread stopped.");
        }
        _pupilSubSocket?.Dispose();
        _pupilReqSocket?.Dispose();
        NetMQConfig.Cleanup();
        if (_motorConnected && _dev != null)
        {
            try { ELLDevicePort.Disconnect(); } catch { }
            _motorConnected = false;
            _dev = null;
            _mgr = null;
            Debug.Log("ELL14: disconnected.");
        }
        _continuousDataWriter?.Close();
        _manualDataWriter?.Close();
        Debug.Log("[Logger] Log files closed.");
    }

    #endregion

    #region Core Logic & Mapping Methods

    // Main pupil-driven motor control logic
    private void HandlePupilMotorControl()
    {
        if (!EnsureDevice()) return;

        _timeSinceLastPupilCheck += Time.deltaTime;

        if (_timeSinceLastPupilCheck >= pupilCheckInterval)
        {
            _timeSinceLastPupilCheck -= pupilCheckInterval;

            // 1. Estimate luminance from pupil radius
            float estimatedLuminance = CalculateLuminanceFromPupilRadius(_currentPupilRadius);
            _lastEstimatedLuminance = estimatedLuminance;

            // 2. Map luminance to target motor angle
            float targetAngle = CalculateAngleFromLuminance(estimatedLuminance);

            decimal finalTargetAngle = (decimal)(targetAngle + initialAngleOffset);
            _lastCalculatedTargetAngle = finalTargetAngle;

            // 3. Move motor only if angle change exceeds threshold
            decimal currentPosition = _dev.Position;
            decimal angleDifference = Math.Abs(finalTargetAngle - currentPosition);

            if (angleDifference >= (decimal)minimumAngleChangeToMove)
            {
                _dev.MoveAbsolute(finalTargetAngle);
                Debug.Log($"[Pupil Control] Move triggered. Luminance: {estimatedLuminance:F4} cd/m^2 -> Target: {Math.Round(finalTargetAngle, 2)} deg");
            }
            else
            {
                Debug.Log($"[Pupil Control] Move skipped. Change {Math.Round(angleDifference, 2)} deg is below threshold.");
            }
        }
    }

    private void HandleMotorInput()
    {
        if (!_motorConnected) return;
        
        if (Input.GetKey(KeyCode.RightArrow))
        {
            Clockwise(coarseSpeedPercent, coarseAdjustmentDegree);
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
           CounterClockwise(coarseSpeedPercent, coarseAdjustmentDegree);
        }else if (Input.GetKey(KeyCode.UpArrow))
        {
            Clockwise(fineSpeedPercent, fineAdjustmentDegree);
        }else if (Input.GetKey(KeyCode.DownArrow))
        {
            CounterClockwise(fineSpeedPercent, fineAdjustmentDegree);
        }else if (Input.GetKey(KeyCode.Space))
        {
            Debug.Log("Final degree selected by user is " + finalDegrees[curUserId++]);
        }else if (Input.GetKey(KeyCode.S))
        {
            SaveFinalDegreesToDisk(finalDegreeSavePath);
        }
    }

    private void SetVelocityPercent(int speedPercent)
    {
        if(!EnsureDevice()) return;
        speedPercent = Mathf.Clamp(speedPercent, 0, 100);
        string pp = speedPercent.ToString("X2");
        char addr = _dev.DeviceInfo.Address;
        _mgr.SendFreeCommand($"{addr}sv{pp}");
    }
    private void Clockwise(int speedPercent, float degree)
    {
        if(!EnsureDevice()) return;
        if (curUserId >= maxUserCount) return;
        if(degree < minimumAngleChangeToMove){
            Debug.LogWarning("degree is too small to move!");
            return;
        }

        SetVelocityPercent(speedPercent);
        _dev.SetJogstepSize((decimal)degree);
        _dev.JogForward();
        finalDegrees[curUserId] += degree;
    }
    
    private void CounterClockwise(int speedPercent, float degree)
    {
        if(!EnsureDevice()) return;
        if (curUserId >= maxUserCount) return;
        if(degree < minimumAngleChangeToMove){
            Debug.LogWarning("degree is too small to move!");
            return;
        }

        SetVelocityPercent(speedPercent);
        _dev.SetJogstepSize((decimal)degree);
        _dev.JogBackward();
        finalDegrees[curUserId] -= degree;
    }
    
    

    // Estimate luminance (cd/m^2) from pupil radius (mm)
    private float CalculateLuminanceFromPupilRadius(float radiusMm)
    {
        float diameterMm = radiusMm * 2f;

        // Empirical formula: L = 10^((a - d) / b)
        const float a = 5.428f;
        const float b = 0.857f;

        float logLuminance = (a - diameterMm) / b;

        return Mathf.Pow(10f, logLuminance);
    }

    // Map luminance to motor angle
    private float CalculateAngleFromLuminance(float luminance)
    {
        if (luminance <= 0) luminance = 0.00001f;

        float logLuminance = Mathf.Log10(luminance);

        float clampedLogLuminance = Mathf.Clamp(logLuminance, minLogLuminance, maxLogLuminance);

        float normalizedValue = (clampedLogLuminance - minLogLuminance) / (maxLogLuminance - minLogLuminance);

        // Interpolate between low and high luminance motor angles
        return Mathf.Lerp(motorAngleForLowLuminance, motorAngleForHighLuminance, normalizedValue);
    }

    private bool EnsureDevice()
    {
        if (!_motorConnected || _dev == null || _mgr == null)
        {
            Debug.LogWarning("ELL14: not connected.");
            return false;
        }
        return true;
    }

    #endregion

    #region Logging and Subscriber Thread

    private void HandleContinuousLogging()
    {
        if (_continuousDataWriter == null || logsPerSecond <= 0) return;
        _timeSinceLastLog += Time.deltaTime;
        if (_timeSinceLastLog >= _logInterval)
        {
            _timeSinceLastLog -= _logInterval;
            LogCurrentData(_continuousDataWriter);
        }
    }

    private void HandleManualLogging()
    {
        if (_manualDataWriter == null) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LogCurrentData(_manualDataWriter);
            Debug.Log("[Logger] Manual event recorded.");
        }
    }

    // Log current data to file
    private void LogCurrentData(StreamWriter writer)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        decimal currentActualAngle = _motorConnected ? _dev.Position : 0m;
        float pupilRadius = _currentPupilRadius;

        string logEntry = $"{timestamp},{_lastCalculatedTargetAngle},{currentActualAngle},{pupilRadius},{_lastEstimatedLuminance}";
        writer.WriteLine(logEntry);
        writer.Flush();
    }

    private void SubscriberLoop()
    {
        // Pupil Core subscriber thread
        while (_isPupilThreadRunning)
        {
            try
            {
                if (!_pupilSubSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(100), out string topic)) continue;
                byte[] msg = _pupilSubSocket.ReceiveFrameBytes();
                var data = MessagePackSerializer.Deserialize<Dictionary<string, object>>(msg);
                if (topic.StartsWith("pupil."))
                {
                    if (data.TryGetValue("circle_3d", out var circle3d) && circle3d is Dictionary<object, object> circle3dDict && circle3dDict.TryGetValue("radius", out var radius3d))
                    {
                        // Pupil Core radius is in mm
                        _currentPupilRadius = Convert.ToSingle(radius3d);
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Pupil Thread] Error: " + e.Message);
                Thread.Sleep(100);
            }
        }
    }

    #endregion
}
