// Component 01: Motion Rendering - Pressure Sensor Handle
// Single FSR sensor controller

const int thrustFsrPin = A0; // FSR 1: Forward Thrust (Pin A0)

void setup() {
  Serial.begin(9600);
}

void loop() {
  int rawThrust = analogRead(thrustFsrPin);

  Serial.print("Raw_Thrust:");
  Serial.println(rawThrust);

  delay(50);
}
