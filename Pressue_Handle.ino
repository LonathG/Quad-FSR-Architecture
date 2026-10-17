// Component 01: Motion Rendering - Quad Pressure Sensor Handle (Thrust, Altitude, Left Turn, Right Turn)

const int thrustFsrPin    = A0; // FSR 1: Forward Thrust (Pin A0)
const int altitudeFsrPin  = A1; // FSR 2: Altitude / Lift (Pin A1)
const int leftTurnFsrPin  = A2; // FSR 3: Left Turn (Pin A2)
const int rightTurnFsrPin = A3; // FSR 4: Right Turn (Pin A3)

void setup() {
  Serial.begin(9600);
}

void loop() {
  int rawThrust = analogRead(thrustFsrPin);
  int rawAltitude = analogRead(altitudeFsrPin);
  int rawLeftTurn = analogRead(leftTurnFsrPin);
  int rawRightTurn = analogRead(rightTurnFsrPin);

  Serial.print("Raw_Thrust:");
  Serial.print(rawThrust);
  Serial.print(",Raw_Altitude:");
  Serial.print(rawAltitude);
  Serial.print(",Raw_LeftTurn:");
  Serial.print(rawLeftTurn);
  Serial.print(",Raw_RightTurn:");
  Serial.println(rawRightTurn);

  delay(50);
}
