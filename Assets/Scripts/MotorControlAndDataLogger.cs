using System;
using System.Collections.Generic;
using System.Collections;
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
    public float initialAngleOffset = 0f;

    [Header("Pupil Core Settings")]
    public string pupilRemoteAddress = "127.0.0.1";
    public string pupilReqPort = "50020";
    
    public float coarseAdjustmentDegree = 5.0f;
    public float fineAdjustmentDegree = 1.0f;

    public int coarseSpeedPercent = 100;
    public int fineSpeedPercent = 100;
    
    // 黑色遮罩UI（请在场景中创建Canvas+Image并命名BlackMask）
    public GameObject blackMask;
    
    // Motor control
    private ELLDevices _mgr;
    private ELLDevice _dev;
    private bool _motorConnected = false;

    // Pupil data
    private RequestSocket _pupilReqSocket;
    private SubscriberSocket _pupilSubSocket;
    private Thread _pupilSubThread;
    private volatile bool _isPupilThreadRunning = false;
    private volatile float _currentPupilRadius = 0.0f;

    private decimal _lastCalculatedTargetAngle;

    #region Unity Lifecycle Methods
    void Start()
    {
        if (!InitializeMotor())
        {
            Debug.LogError("ELL14: connect failed, abort auto run.");
        }
        InitializePupilSubscriber();
        // 日志逻辑改为Log类管理
    }

    void Update()
    {
        // Manual motor control is disabled; only pupil-driven control is active
        HandleMotorInput(); 
        // 日志逻辑改为Log类管理
        Log.Instance.UpdateAngle(_motorConnected ? (float)_dev.Position : 0f);
        Log.Instance.UpdatePupilSize(_currentPupilRadius);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Log.Instance.LogManualEvent();
            // 显示黑色遮罩
            if (blackMask != null) blackMask.SetActive(true);
            decimal initialAngle = (decimal)initialAngleOffset;
            _dev.MoveAbsolute(initialAngle);
            Debug.Log($"[Motor] Moved to initial angle: {initialAngleOffset} degrees.");
        }
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
                //_dev.Home(ELLBaseDevice.DeviceDirection.Clockwise);
                //Thread.Sleep(600);
                decimal initialAngle = (decimal)initialAngleOffset;
                _dev.MoveAbsolute(initialAngle);
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

    }

    #endregion

    #region Core Logic & Mapping Methods

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
        }
    // 协程：电机归位并等待完成后隐藏遮罩
    IEnumerator HomeAndUnmaskCoroutine()
    {
        if (_dev != null)
        {
            //_dev.Home(Thorlabs.Elliptec.ELLO_DLL.ELLBaseDevice.DeviceDirection.Clockwise);
            decimal initialAngle = (decimal)initialAngleOffset;
            _dev.MoveAbsolute(initialAngle);
            _lastCalculatedTargetAngle = initialAngle;
            // 等待电机归位完成（假设有IsHoming或类似标志，否则可用延时）
            float waitTime = 2.0f; // 可根据实际归位时间调整
            yield return new WaitForSeconds(waitTime);
        }
        if (blackMask != null) blackMask.SetActive(false);
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

        SetVelocityPercent(speedPercent);
        _dev.SetJogstepSize((decimal)degree);
        _dev.JogForward();
        Debug.Log("Current Angle = " + _dev.Position);
    }
    
    private void CounterClockwise(int speedPercent, float degree)
    {
        if(!EnsureDevice()) return;

        SetVelocityPercent(speedPercent);
        _dev.SetJogstepSize((decimal)degree);
        _dev.JogBackward();
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

    #region Subscriber Thread

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
