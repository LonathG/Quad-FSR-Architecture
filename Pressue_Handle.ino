// Component 01: Motion Rendering - Quad Pressure Sensor Handle (Thrust, Altitude, Left Turn, Right Turn)
// Pure FSR sensor controller (No haptic motor load on USB 5V rail)

const int thrustFsrPin    = A0; // FSR 1: Forward Thrust (Pin A0)
const int altitudeFsrPin  = A1; // FSR 2: Altitude / Lift (Pin A1)
const int leftTurnFsrPin  = A2; // FSR 3: Left Turn (Pin A2)
const int rightTurnFsrPin = A3; // FSR 4: Right Turn (Pin A3)

// Calibration thresholds (Adjust based on your squeeze intensity)
const int thrustDeadzone = 15;    // Below this value is considered resting/zero
const int thrustMax = 350;        // Max expected reading for FSR 1 (e.g. 100-500)

const int altitudeDeadzone = 25;  // Below this value is considered resting/zero
const int altitudeMax = 700;      // Max expected reading for FSR 2 (e.g. 700)

void setup() {
  Serial.begin(115200); // 115200 baud (matching BroomFlightController default)
  Serial.println(F("--- QUAD FSR MODE: THRUST (A0), ALTITUDE (A1), LEFT TURN (A2), RIGHT TURN (A3) ---"));
}

void loop() {
  // 1. Read FSRs with ADC settling dummy reads to prevent channel crosstalk
  analogRead(thrustFsrPin); // dummy read to settle ADC multiplexer
  int rawThrust = analogRead(thrustFsrPin);

  analogRead(altitudeFsrPin); // dummy read to settle ADC multiplexer
  int rawAltitude = analogRead(altitudeFsrPin);

  analogRead(leftTurnFsrPin); // dummy read to settle ADC multiplexer
  int rawLeftTurn = analogRead(leftTurnFsrPin);

  analogRead(rightTurnFsrPin); // dummy read to settle ADC multiplexer
  int rawRightTurn = analogRead(rightTurnFsrPin);

  Serial.print(F("Raw_Thrust:"));
  Serial.print(rawThrust);
  Serial.print(F(",Raw_Altitude:"));
  Serial.print(rawAltitude);
  Serial.print(F(",Raw_LeftTurn:"));
  Serial.print(rawLeftTurn);
  Serial.print(F(",Raw_RightTurn:"));
  Serial.println(rawRightTurn);

  delay(25);
}
