using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using System.Text;

public class BroomFlightController : MonoBehaviour
{
    [Header("Serial Port Settings")]
    [Tooltip("COM port for Arduino / ESP hardware.")]
    public string portName = "COM6";
    public int baudRate = 115200;
    public bool enableSerial = true;

    [Header("Movement Target")]
    [Tooltip("Target GameObject to move (e.g. XROrigin / VR Rig root). If unassigned, automatically uses parent or this transform.")]
    public Transform rigTarget;
    public Transform vrCamera;

    [Header("Flight Physics - Forward")]
    public float speedMultiplier = 25.0f;
    public float accelerationSmooth = 0.05f;
    public float decelerationSmooth = 0.02f;

    [Header("Flight Physics - Altitude (FSR 2)")]
    [Tooltip("Ascent speed when pressure is applied to FSR 2 or Spacebar is held.")]
    public float maxClimbSpeed = 10.0f;
    [Tooltip("Gradual descent speed when no pressure is applied to FSR 2 (smoothly brings broom down to ground).")]
    public float gravityFallSpeed = 4.0f;
    [Tooltip("Fast descent speed when Q / Left Control is pressed.")]
    public float maxDescendSpeed = 7.0f;
    [Tooltip("Smooth time for vertical velocity transitions.")]
    public float altitudeSmoothTime = 0.22f;
    [Tooltip("Minimum Y height floor level to prevent falling into void.")]
    public float minGroundY = 0.0f;
    public bool useGroundLimit = true;

    [Header("Steering Settings (FSR 3 & 4 + Head Tilt)")]
    [Tooltip("Maximum yaw turning speed in degrees per second when squeezing Left/Right Turn FSRs or pressing A/D.")]
    public float turnSpeed = 60.0f;
    [Tooltip("Smoothing response speed for FSR turning transitions.")]
    public float turnSmoothSpeed = 15.0f;
    [Tooltip("Enable steering via FSR 3 (Left Turn) and FSR 4 (Right Turn).")]
    public bool enableFsrSteering = true;
    [Tooltip("Enable steering via VR head tilt roll.")]
    public bool enableHeadTiltSteering = true;
    [Tooltip("Sensitivity multiplier for head tilt roll steering.")]
    public float turnSensitivity = 1.5f;
    [Tooltip("Minimum head tilt roll angle before tilt steering kicks in.")]
    public float tiltThreshold = 8f;

    [Header("Sensor Mapping Settings")]
    [Tooltip("Minimum expected raw sensor value from hardware (deadzone).")]
    public float rawInputMin = 15f;
    [Tooltip("Maximum expected raw sensor value for Thrust FSR 1 (e.g. 350).")]
    public float rawThrustMax = 350f;
    [Tooltip("Maximum expected raw sensor value for Altitude FSR 2 (e.g. 700).")]
    public float rawAltitudeMax = 700f;
    [Tooltip("Maximum expected raw sensor value for Left Turn FSR 3 (e.g. 350).")]
    public float rawLeftTurnMax = 350f;
    [Tooltip("Maximum expected raw sensor value for Right Turn FSR 4 (e.g. 350).")]
    public float rawRightTurnMax = 350f;
    [Tooltip("Automatically scale hardware values to 0-100% flight thrust, altitude & steering.")]
    public bool autoScaleSensorInput = true;

    [Header("Testing & Debug Fallback")]
    [Tooltip("Allow pressing W/Up (thrust), Space/E (climb), Q/Ctrl (descend), A/Left (turn left), D/Right (turn right) without hardware.")]
    public bool enableKeyboardFallback = true;
    public bool debugLogs = true;
    [Tooltip("Display an on-screen real-time telemetry HUD in the Game view / VR.")]
    public bool showOnScreenDebug = true;

    // Serial & Threading State (Crash-proof architecture)
    private SerialPort sp;
    private Thread serialThread;
    private readonly object dataLock = new object();
    private string lastData = "";
    private volatile bool isRunning = false;
    private volatile bool isThreadActive = false;

    // Diagnostics & Metrics
    private int totalPacketsReceived = 0;
    private int packetsInLastSecond = 0;
    private int measuredPacketsPerSec = 0;
    private float ppsTimer = 0f;
    private float lastPacketReceivedTime = 0f;
    private string latestTelemetryRaw = "Waiting for hardware...";
    private string lastConnectionError = "";
    private bool portNamesLogged = false;
    private int consecutiveErrors = 0;

    // Flight Dynamics State
    private float currentSpeed = 0f;
    private float currentVerticalSpeed = 0f;
    private float verticalSpeedVelocity = 0f;
    private float smoothThrustInput = 0f;
    private float smoothAltitudeInput = 0f;
    private float smoothTurnInput = 0f;
    private int serialThrust = 0;
    private int serialAltitude = 0;
    private int serialLeftTurn = 0;
    private int serialRightTurn = 0;
    private int targetThrust = 0;
    private int targetAltitude = 0;
    private int targetLeftTurn = 0;
    private int targetRightTurn = 0;
    private float logTimer = 0f;
    private bool lastGroundedState = false;
    private CollisionFlags lastCollisionFlags = CollisionFlags.None;

    // Cached GUI Styles to prevent native IMGUI memory leaks
    private GUIStyle cachedBoxStyle;
    private GUIStyle cachedHeaderStyle;
    private GUIStyle cachedDataStyle;

    // Public Telemetry Accessors for external controllers (Fans, Lighting, Audio)
    public float CurrentSpeed => currentSpeed;
    public float CurrentVerticalSpeed => currentVerticalSpeed;
    public int TargetThrust => targetThrust;
    public int TargetAltitude => targetAltitude;
    public int TargetLeftTurn => targetLeftTurn;
    public int TargetRightTurn => targetRightTurn;
    public float SmoothTurnInput => smoothTurnInput;
    public int PacketsPerSecond => measuredPacketsPerSec;
    public bool IsSerialConnected => sp != null && sp.IsOpen && isRunning;

    private CharacterController characterController;
    private Rigidbody rb;

    void Start()
    {
        // 1. Resolve VR Camera
        if (vrCamera == null)
        {
            if (Camera.main != null) vrCamera = Camera.main.transform;
            else vrCamera = transform;
        }

        // 2. Resolve Rig Target (Do not move Camera directly because XR tracking overrides it)
        if (rigTarget == null)
        {
            if (transform == vrCamera && transform.parent != null)
            {
                rigTarget = transform.parent;
                Debug.Log($"[BroomFlight] Script attached to Camera. Movement target set to parent: {rigTarget.name}");
            }
            else
            {
                rigTarget = transform;
            }
        }

        // 3. Check for Physics Components on Rig Target
        if (rigTarget != null)
        {
            characterController = rigTarget.GetComponent<CharacterController>();
            rb = rigTarget.GetComponent<Rigidbody>();

            Debug.Log($"[BroomFlight Setup] Rig Target: '{rigTarget.name}' | Position: {rigTarget.position}");
            Debug.Log($"[BroomFlight Setup] CharacterController found: {(characterController != null ? $"YES (Enabled: {characterController.enabled}, Height: {characterController.height}, Center: {characterController.center})" : "NO")}");
            Debug.Log($"[BroomFlight Setup] Rigidbody found: {(rb != null ? $"YES (isKinematic: {rb.isKinematic}, useGravity: {rb.useGravity})" : "NO")}");

            // Scan and report any other locomotion/movement scripts
            MonoBehaviour[] allScripts = rigTarget.GetComponents<MonoBehaviour>();
            foreach (var s in allScripts)
            {
                if (s != null && s != this)
                {
                    string scriptName = s.GetType().Name;
                    if (scriptName.Contains("Move") || scriptName.Contains("Locomotion") || scriptName.Contains("Gravity") || scriptName.Contains("Driver"))
                    {
                        Debug.LogWarning($"[BroomFlight Conflict Warning] Detected locomotion script '{scriptName}' on RigTarget. Ensure gravity/movement scripts do not fight BroomFlight.");
                    }
                }
            }
        }

        // 4. Connect Serial Hardware with Auto-Reconnect Coroutine
        if (enableSerial)
        {
            StartCoroutine(AutoConnectLoop());
        }
    }


    private IEnumerator AutoConnectLoop()
    {
        while (enableSerial)
        {
            if (sp == null || !sp.IsOpen || !isThreadActive)
            {
                InitSerial();
            }

            // Non-aggressive 4-second check to avoid driver lockups
            yield return new WaitForSeconds(4.0f);
        }
    }


    void InitSerial()
    {
        try
        {
            CloseSerialConnection();

            if (!portNamesLogged && debugLogs)
            {
                portNamesLogged = true;
                try
                {
                    string[] availablePorts = SerialPort.GetPortNames();
                    Debug.Log($"[BroomFlight] Available Serial Ports on PC: [{string.Join(", ", availablePorts)}]");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BroomFlight] Could not query COM ports: {ex.Message}");
                }
            }

            Debug.Log($"[BroomFlight] Attempting connection on {portName} @ {baudRate} baud...");

            sp = new SerialPort(portName, baudRate)
            {
                ReadTimeout = 50,    // Short timeout so thread loop responds to shutdown quickly
                WriteTimeout = 50,
                DtrEnable = true,
                RtsEnable = true
            };

            sp.Open();

            isRunning = true;
            lastConnectionError = "";
            consecutiveErrors = 0;

            serialThread = new Thread(ReadSerialData)
            {
                IsBackground = true,
                Name = "BroomFlight_SerialReader"
            };
            serialThread.Start();

            Debug.Log($"<color=#00FF66><b>[BroomFlight] Successfully Connected to Hardware on {portName} ({baudRate} baud)!</b></color>");
        }
        catch (System.Exception e)
        {
            lastConnectionError = e.Message;
            if (debugLogs)
            {
                Debug.LogWarning($"[BroomFlight] Serial Connection Pending on {portName}: {e.Message}");
            }
            CloseSerialConnection();
        }
    }


    /// <summary>
    /// Thread-safe, non-blocking serial packet collector.
    /// Accumulates raw incoming bytes without blocking ReadLine hangs or mid-line buffer clearing.
    /// </summary>
    void ReadSerialData()
    {
        isThreadActive = true;
        StringBuilder lineBuilder = new StringBuilder(128);

        while (isRunning)
        {
            try
            {
                if (sp != null && sp.IsOpen)
                {
                    // Check available bytes to prevent synchronous kernel hang
                    int bytesAvailable = sp.BytesToRead;
                    if (bytesAvailable > 0)
                    {
                        // Prevent extreme buffer pileup if computer hitched
                        if (bytesAvailable > 512)
                        {
                            sp.DiscardInBuffer();
                            lineBuilder.Clear();
                            continue;
                        }

                        for (int i = 0; i < bytesAvailable; i++)
                        {
                            int byteRead = sp.ReadByte();
                            if (byteRead == -1) break;

                            char c = (char)byteRead;
                            if (c == '\n')
                            {
                                string completeLine = lineBuilder.ToString();
                                lineBuilder.Clear();

                                if (!string.IsNullOrEmpty(completeLine))
                                {
                                    lock (dataLock)
                                    {
                                        lastData = completeLine;
                                    }
                                }
                            }
                            else if (c != '\r')
                            {
                                if (lineBuilder.Length < 256)
                                {
                                    lineBuilder.Append(c);
                                }
                                else
                                {
                                    lineBuilder.Clear(); // Discard oversized garbage
                                }
                            }
                        }

                        consecutiveErrors = 0;
                    }
                    else
                    {
                        // No bytes currently in buffer; sleep briefly to yield CPU
                        Thread.Sleep(5);
                    }
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
            catch (System.TimeoutException)
            {
                // Expected when waiting for next sensor burst
            }
            catch (System.IO.IOException ioEx)
            {
                consecutiveErrors++;
                if (consecutiveErrors < 3 && debugLogs)
                {
                    Debug.LogWarning($"[BroomFlight Serial IO] {ioEx.Message}");
                }
                Thread.Sleep(20);
            }
            catch (System.InvalidOperationException)
            {
                Thread.Sleep(50);
            }
            catch (System.Exception ex)
            {
                consecutiveErrors++;
                if (consecutiveErrors < 3 && debugLogs)
                {
                    Debug.LogWarning($"[BroomFlight Serial Error] {ex.Message}");
                }
                Thread.Sleep(20);
            }
        }

        isThreadActive = false;
    }


    void Update()
    {
    }

    private void CloseSerialConnection() { }
}
