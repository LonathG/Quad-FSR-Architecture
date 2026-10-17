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


    void Start()
    {
    }

    void Update()
    {
    }
}
