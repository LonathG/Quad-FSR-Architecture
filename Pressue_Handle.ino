// Component 01: Motion Rendering - Dual Pressure Sensor Handle (Thrust & Altitude)

const int thrustFsrPin   = A0; // FSR 1: Forward Thrust (Pin A0)
const int altitudeFsrPin = A1; // FSR 2: Altitude / Lift (Pin A1)

void setup() {
  Serial.begin(9600);
}

void loop() {
  int rawThrust = analogRead(thrustFsrPin);
  int rawAltitude = analogRead(altitudeFsrPin);

  Serial.print("Raw_Thrust:");
  Serial.print(rawThrust);
  Serial.print(",Raw_Altitude:");
  Serial.println(rawAltitude);

  delay(50);
}
