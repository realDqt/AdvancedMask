//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Threading;
//using UnityEngine;

//// NetMQ and MessagePack for Pupil Core
//using NetMQ;
//using NetMQ.Sockets;
//using MessagePack;

//// Thorlabs ELL14 for Motor Control
//using Thorlabs.Elliptec.ELLO_DLL;

//public class MotorControlAndDataLogger : MonoBehaviour
//{
//    [Header("Motor Settings")]
//    public string portName = "COM7"; // ������ӵ�COM�˿�
//    public char minAddress = '0';
//    public char maxAddress = '1';
//    [Tooltip("����ĳ�ʼ�Ƕ�ƫ����")]
//    public float initialAngleOffset = 102f;

//    [Header("Auto Run Pattern")]
//    [Range(0, 100)] public int speedPercentCW = 60;  // ˳ʱ���ٶȣ�% of max��
//    [Range(0, 100)] public int speedPercentCCW = 60;  // ��ʱ���ٶȣ�% of max��
//    public float stepDegreesCW = 10f;               // ÿ��˳ʱ����ԽǶȣ��ȣ�
//    public float stepDegreesCCW = 10f;               // ÿ����ʱ����ԽǶȣ��ȣ�

//    [Header("Pupil Core Settings")]
//    public string pupilRemoteAddress = "127.0.0.1";
//    public string pupilReqPort = "50020";

//    [Header("Data Logging")]
//    [Tooltip("ÿ���¼���ݵĴ���")]
//    public float logsPerSecond = 3f;

//    // --- �������ܣ�����ͫ�׵ĵ������ ---
//    [Header("Pupil-Controlled Motor")]
//    [Tooltip("ÿ����������һ��ͫ�ײ����µ���Ƕ�")]
//    public float pupilCheckInterval = 2f;
//    [Tooltip("����ӳ�����Сͫ�װ뾶")]
//    public float minPupilRadius = 0f;
//    [Tooltip("����ӳ������ͫ�װ뾶")]
//    public float maxPupilRadius = 5f;
//    [Tooltip("ӳ�䵽�������С�Ƕ�")]
//    public float minMotorAngle = 0f;
//    [Tooltip("ӳ�䵽��������Ƕ�")]
//    public float maxMotorAngle = 90f;
//    [Tooltip("ͫ�ױ仯��Сʱ�ĵ���ٶ� (0-100)")]
//    [Range(0, 100)] public int minChangeSpeed = 10;
//    [Tooltip("ͫ�ױ仯���ʱ�ĵ���ٶ� (0-100)")]
//    [Range(0, 100)] public int maxChangeSpeed = 100;

//    // --- Private Motor Control ---
//    private ELLDevices _mgr;
//    private ELLDevice _dev;
//    private bool _motorConnected = false;

//    // --- Private Pupil Data ---
//    private RequestSocket _pupilReqSocket;
//    private SubscriberSocket _pupilSubSocket;
//    private Thread _pupilSubThread;
//    private volatile bool _isPupilThreadRunning = false;
//    private volatile float _currentPupilRadius = 0.0f;

//    // --- Private Data Logging ---
//    private StreamWriter _continuousDataWriter;
//    private StreamWriter _manualDataWriter;
//    private float _logInterval;
//    private float _timeSinceLastLog = 0f;

//    // --- �������ܵ�˽�б��� ---
//    private float _timeSinceLastPupilCheck = 0f;
//    private float _previousPupilRadius = 0.0f;


//    #region Unity Lifecycle Methods

//    void Start()
//    {
//        // 1. ��ʼ���������
//        if (!InitializeMotor())
//        {
//            Debug.LogError("ELL14: connect failed, abort auto run.");
//        }

//        // 2. ��ʼ��Pupil Core����
//        InitializePupilSubscriber();

//        // 3. ��ʼ�����ݼ�¼�ļ�
//        InitializeDataLoggers();

//        // 4. �������ݼ�¼Ƶ��
//        if (logsPerSecond > 0)
//        {
//            _logInterval = 1f / logsPerSecond;
//        }

//        // ��ʼ����һ�ε�ͫ�װ뾶�����ڼ���仯��
//        _previousPupilRadius = _currentPupilRadius;
//    }

//    void Update()
//    {
//        // 1. �����û������Կ��Ƶ��
//        HandleMotorInput();

//        // 2. ������ʱ���ݼ�¼
//        HandleContinuousLogging();

//        // 3. �����ֶ��¼���¼
//        HandleManualLogging();

//        // 4. ��������������ͫ�׵ĵ���Զ�����
//        HandlePupilMotorControl();
//    }

//    void OnDestroy()
//    {
//        Cleanup();
//    }

//    void OnApplicationQuit()
//    {
//        Cleanup();
//    }

//    #endregion

//    #region Initialization and Cleanup

//    private bool InitializeMotor()
//    {
//        if (_motorConnected) return true;

//        _mgr = new ELLDevices();
//        if (!ELLDevicePort.Connect(portName))
//        {
//            Debug.LogError($"[Motor] Failed to open port {portName}.");
//            return false;
//        }

//        try
//        {
//            List<string> devices = _mgr.ScanAddresses(minAddress, maxAddress);
//            foreach (string devStr in devices)
//            {
//                if (!_mgr.Configure(devStr)) continue;

//                var d = _mgr.AddressedDevice(devStr[0]) as ELLDevice;
//                if (d == null) continue;

//                // �����ʼ���Ƕ�Ϊ 0
//                _dev = d;
//                _dev.MoveAbsolute((decimal)initialAngleOffset);
//                Thread.Sleep(600);

//                _motorConnected = true;

//                // ��ӡ�豸��Ϣ
//                foreach (string line in _dev.DeviceInfo.Description())
//                    Debug.Log($"ELL14 {line}");

//                Debug.Log($"ELL14 connected on {portName}, addr {_dev.DeviceInfo.Address}");
//                return true;
//            }

//            Debug.LogError("ELL14: no device found on bus.");
//            ELLDevicePort.Disconnect();
//            return false;
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError("ELL14 connect error: " + ex.Message);
//            try { ELLDevicePort.Disconnect(); } catch { }
//            _motorConnected = false;
//            _dev = null;
//            _mgr = null;
//            return false;
//        }
//    }

//    private void InitializePupilSubscriber()
//    {
//        AsyncIO.ForceDotNet.Force(); // ����Unity�༭������
//        try
//        {
//            // ���� SUB_PORT
//            _pupilReqSocket = new RequestSocket();
//            _pupilReqSocket.Connect($"tcp://{pupilRemoteAddress}:{pupilReqPort}");
//            _pupilReqSocket.SendFrame("SUB_PORT");
//            string subPort = _pupilReqSocket.ReceiveFrameString();
//            Debug.Log($"[Pupil] SUB_PORT = {subPort}");

//            // ���� pupil ����
//            _pupilSubSocket = new SubscriberSocket();
//            _pupilSubSocket.Connect($"tcp://{pupilRemoteAddress}:{subPort}");
//            _pupilSubSocket.Subscribe("pupil.");

//            // ������̨�߳̽�������
//            _isPupilThreadRunning = true;
//            _pupilSubThread = new Thread(SubscriberLoop);
//            _pupilSubThread.IsBackground = true;
//            _pupilSubThread.Start();
//            Debug.Log("[Pupil] Subscriber thread started.");
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"[Pupil] Initialization failed: {e.Message}");
//        }
//    }

//    private void InitializeDataLoggers()
//    {
//        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
//        string continuousLogPath = Path.Combine(Application.persistentDataPath, $"ContinuousLog_{timestamp}.csv");
//        string manualLogPath = Path.Combine(Application.persistentDataPath, $"ManualLog_{timestamp}.csv");

//        try
//        {
//            // ������д��������־�ļ�ͷ
//            _continuousDataWriter = new StreamWriter(continuousLogPath, true);
//            _continuousDataWriter.WriteLine("Timestamp,RotationAngle,PupilRadius");
//            _continuousDataWriter.Flush(); // ȷ����ͷ����д��
//            Debug.Log($"[Logger] Continuous data will be saved to: {continuousLogPath}");

//            // ������д���ֶ���־�ļ�ͷ
//            _manualDataWriter = new StreamWriter(manualLogPath, true);
//            _manualDataWriter.WriteLine("Timestamp,RotationAngle,PupilRadius");
//            _manualDataWriter.Flush();
//            Debug.Log($"[Logger] Manual events will be saved to: {manualLogPath}");
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"[Logger] Failed to create log files: {e.Message}");
//        }
//    }


//    private void Cleanup()
//    {
//        // ֹͣ��̨�߳�
//        if (_isPupilThreadRunning)
//        {
//            _isPupilThreadRunning = false;
//            _pupilSubThread?.Join(); // �ȴ��߳̽���
//            Debug.Log("[Pupil] Subscriber thread stopped.");
//        }

//        // �ر�����Socket
//        _pupilSubSocket?.Dispose();
//        _pupilReqSocket?.Dispose();
//        NetMQConfig.Cleanup();

//        // ֹͣ������Ͽ�����
//        if (_motorConnected && _dev != null)
//        {
//            try { ELLDevicePort.Disconnect(); } catch { }
//            _motorConnected = false;
//            _dev = null;
//            _mgr = null;
//            Debug.Log("ELL14: disconnected.");
//        }

//        // �ر��ļ�д����
//        _continuousDataWriter?.Close();
//        _manualDataWriter?.Close();
//        Debug.Log("[Logger] Log files closed.");
//    }

//    #endregion

//    #region Core Logic in Update

//    private void HandleMotorInput()
//    {
//        if (!_motorConnected) return;

//        if (Input.GetKey(KeyCode.RightArrow))
//        {
//            Clockwise(speedPercentCW, stepDegreesCW);
//        }
//        else if (Input.GetKey(KeyCode.LeftArrow))
//        {
//            CounterClockwise(speedPercentCCW, stepDegreesCCW);
//        }
//    }

//    // --- ��������������ͫ�����ݿ��Ƶ�� ---
//    private void HandlePupilMotorControl()
//    {
//        if (!EnsureDevice()) return;

//        // ��ʱ��
//        _timeSinceLastPupilCheck += Time.deltaTime;

//        // ����ﵽ�趨�ļ����
//        if (_timeSinceLastPupilCheck >= pupilCheckInterval)
//        {
//            _timeSinceLastPupilCheck -= pupilCheckInterval; // ���ü�ʱ��

//            // 1. ����ͫ�״�С�ı仯��
//            float pupilChange = Mathf.Abs(_currentPupilRadius - _previousPupilRadius);
//            _previousPupilRadius = _currentPupilRadius; // ������һ�ε�ͫ�״�С

//            // 2. ��ͫ�ױ仯��ӳ�䵽����ٶ�
//            // ���Ƚ��仯����һ���� 0-1 ��Χ (����ͫ�װ뾶���仯�����ᳬ��maxPupilRadius)
//            float normalizedChange = Mathf.Clamp01(pupilChange / maxPupilRadius);
//            // ʹ�����Բ�ֵ�����ٶȰٷֱ�
//            int speedPercent = (int)Mathf.Lerp(minChangeSpeed, maxChangeSpeed, normalizedChange);
//            SetVelocityPercent(speedPercent); // ���õ���ٶ�

//            // 3. ����ǰͫ�״�Сӳ�䵽Ŀ��Ƕ�
//            // ���Ƚ���ǰͫ�װ뾶�������趨�ķ�Χ��
//            float clampedRadius = Mathf.Clamp(_currentPupilRadius, minPupilRadius, maxPupilRadius);
//            // �����һ���� 0-1 ��Χ
//            float normalizedRadius = (clampedRadius - minPupilRadius) / (maxPupilRadius - minPupilRadius);
//            // ʹ�����Բ�ֵ����Ŀ��Ƕ�
//            decimal targetAngle = (decimal)(initialAngleOffset + minMotorAngle + normalizedRadius * (maxMotorAngle - minMotorAngle));

//            // 4. �������ƶ������ԽǶ�
//            // ע�⣺��������ʹ�� MoveAbsolute�������õ��ֱ���ƶ���ָ����Ŀ��Ƕ�
//            _dev.MoveAbsolute(targetAngle);

//            Debug.Log($"[Pupil Control] New Target Angle: {Math.Round(targetAngle, 2)} deg | Speed: {speedPercent}% | Current Pupil Radius: {_currentPupilRadius:F2}");
//        }
//    }


//    // ��ԡ�˳ʱ�롱�ƶ���degree>0����λ���ȣ�speedPercent 0..100��
//    public void Clockwise(int speedPercent, float degree)
//    {
//        if (!EnsureDevice()) return;
//        if (degree <= 0f) { Debug.LogWarning("degree must be > 0"); return; }
//        SetVelocityPercent(speedPercent);               // �ٶ����� fw/bw��jog��
//        _dev.SetJogstepSize((decimal)degree);           // ��ת̨��λ=��
//        _dev.JogForward();                              // ˳ʱ��һ��
//    }
//    private bool EnsureDevice()
//    {
//        if (!_motorConnected || _dev == null || _mgr == null)
//        {
//            Debug.LogWarning("ELL14: not connected.");
//            return false;
//        }
//        return true;
//    }

//    // ��ԡ���ʱ�롱�ƶ�
//    public void CounterClockwise(int speedPercent, float degree)
//    {
//        if (!EnsureDevice()) return;
//        if (degree <= 0f) { Debug.LogWarning("degree must be > 0"); return; }
//        SetVelocityPercent(speedPercent);
//        _dev.SetJogstepSize((decimal)degree);
//        _dev.JogBackward();                             // ��ʱ��һ��
//    }

//    private void SetVelocityPercent(int percent)
//    {
//        if (!EnsureDevice()) return;
//        percent = Mathf.Clamp(percent, 0, 100);
//        string pp = percent.ToString("X2");             // 50 -> "32"
//        char addr = _dev.DeviceInfo.Address;            // '0'..'F' / 'A' ��
//        _mgr.SendFreeCommand($"{addr}sv{pp}");          // ���� CRLF��$"{addr}sv{pp}\r\n"
//    }

//    private void HandleContinuousLogging()
//    {
//        if (_continuousDataWriter == null || logsPerSecond <= 0) return;

//        _timeSinceLastLog += Time.deltaTime;
//        if (_timeSinceLastLog >= _logInterval)
//        {
//            _timeSinceLastLog -= _logInterval;
//            LogCurrentData(_continuousDataWriter);
//        }
//    }

//    private void HandleManualLogging()
//    {
//        if (_manualDataWriter == null) return;

//        if (Input.GetKeyDown(KeyCode.Space))
//        {
//            LogCurrentData(_manualDataWriter);
//            Debug.Log("[Logger] Manual event recorded.");
//        }
//    }

//    #endregion

//    #region Helper Methods

//    private void LogCurrentData(StreamWriter writer)
//    {
//        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//        // ��ȡ�����ǰλ�ã��Ƕȣ�
//        decimal currentAngle = _motorConnected ? _dev.Position : 0m;
//        // ��ȡͫ������
//        float pupilRadius = _currentPupilRadius;

//        string logEntry = $"{timestamp},{currentAngle},{pupilRadius}";
//        writer.WriteLine(logEntry);
//        writer.Flush(); // ����д���ļ����������ݶ�ʧ
//    }

//    #endregion

//    #region Pupil Subscriber Thread

//    private void SubscriberLoop()
//    {
//        while (_isPupilThreadRunning)
//        {
//            try
//            {
//                // ʹ�÷�����ʽ���գ������߳����˳�ʱ��ס
//                if (!_pupilSubSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(100), out string topic)) continue;

//                byte[] msg = _pupilSubSocket.ReceiveFrameBytes();
//                var data = MessagePackSerializer.Deserialize<Dictionary<string, object>>(msg);

//                // ����ֻ����ͫ������
//                if (topic.StartsWith("pupil."))
//                {
//                    // ���ԴӲ�ͬ��ͫ�����ݸ�ʽ�л�ȡ�뾶
//                    // Example for pupil.0.3d
//                    if (data.TryGetValue("circle_3d", out var circle3d) && circle3d is Dictionary<object, object> circle3dDict && circle3dDict.TryGetValue("radius", out var radius3d))
//                    {
//                        _currentPupilRadius = Convert.ToSingle(radius3d);
//                        continue; // �ɹ���ȡ��������һ��ѭ��
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Debug.LogError("[Pupil Thread] Error: " + e.Message);
//                // ��������ʱ�������ߣ�����CPUռ�ù���
//                Thread.Sleep(100);
//            }
//        }
//    }

//    #endregion
//}


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
    }

    void Update()
    {
        // Manual motor control is disabled; only pupil-driven control is active
        // HandleMotorInput(); 
        HandleContinuousLogging();
        HandleManualLogging();
        HandlePupilMotorControl();
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
