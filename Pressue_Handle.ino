// Component 01: Motion Rendering - Quad Pressure Sensor Handle (Thrust, Altitude, Left Turn, Right Turn)

const int thrustFsrPin    = A0; // FSR 1: Forward Thrust (Pin A0)
const int altitudeFsrPin  = A1; // FSR 2: Altitude / Lift (Pin A1)
const int leftTurnFsrPin  = A2; // FSR 3: Left Turn (Pin A2)
const int rightTurnFsrPin = A3; // FSR 4: Right Turn (Pin A3)

void setup() {
  Serial.begin(115200); // 115200 baud (matching BroomFlightController default)
  Serial.println(F("--- QUAD FSR MODE: THRUST (A0), ALTITUDE (A1), LEFT TURN (A2), RIGHT TURN (A3) ---"));
}

void loop() {
  int rawThrust = analogRead(thrustFsrPin);
  int rawAltitude = analogRead(altitudeFsrPin);
  int rawLeftTurn = analogRead(leftTurnFsrPin);
  int rawRightTurn = analogRead(rightTurnFsrPin);

  Serial.print(F("Raw_Thrust:"));
  Serial.print(rawThrust);
  Serial.print(F(",Raw_Altitude:"));
  Serial.print(rawAltitude);
  Serial.print(F(",Raw_LeftTurn:"));
  Serial.print(rawLeftTurn);
  Serial.print(F(",Raw_RightTurn:"));
  Serial.println(rawRightTurn);

  delay(30);
}
