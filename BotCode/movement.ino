// Used to trigger a timeout so the bot won't drive forever if it misses a stop
bool timeUp = false;
// Corrects drift in the gyroscope
const double gyroCorrect = 0.015;

void driveForward(uint8_t spaces)
{
  // Initialize some variables
  int Z_threshold = 0;
  uint8_t count = 0;
  bool crossing = false;
  sensors_event_t event;
  sensors_event_t event2;
  int Y_calibration = 0;
  uint8_t countdown = 4;
  elapsedMillis mils;
  float turn_drift = 0;

  // Get starting Y and Z axis magnetometer readings, averaging 10 readings
  for (int i = 0; i < 10; i++)
  {
    mag.getEvent(&event);
    Y_calibration += event.magnetic.y;
    Z_threshold += event.magnetic.z;
    delay(10);
  }
  Y_calibration /= 10;
  Z_threshold = (Z_threshold / 10) - Z_offset;

  // Define a maximum time the robot should drive
  timeUp = false;
  timeout.begin(timedOut, 1500000 * spaces);
   
  mils = 0;
  // Start driving
  left.write(leftForwardSpeed);
  right.write(rightForwardSpeed);
  while (countdown > 0 && !timeUp)
  {
    // Get gyro and magnetometer readings
    gyro.getEvent(&event2);
    turn_drift += ((event2.gyro.z - gyroCorrect) / 1000) * mils;
    mils = 0;
    mag.getEvent(&event);
    #ifdef debug
      Serial.print("Gryo: "); Serial.println(turn_drift);
      Serial.print("X: "); Serial.print(event.magnetic.x); Serial.print("  ");
      Serial.print("Y: "); Serial.print(event.magnetic.y); Serial.print("  ");
      Serial.print("Z: "); Serial.print(event.magnetic.z); Serial.print("  ");
      Serial.println("uT");
    #endif
    // Check if gyro detects the bot has drifted off course, and correct accordingly
    if (abs(turn_drift) > turn_drift_threshold)
    {
      if (turn_drift > 0)
      {
        #ifdef debug
          Serial.println("Gyro turn left");
        #endif
        left.write(leftForwardSpeed);
        right.write(rightForwardSpeed - turnBoost);        
      }
      else 
      {
        #ifdef debug
          Serial.println("Gyro turn right");
        #endif
        left.write(leftForwardSpeed + turnBoost);
        right.write(rightForwardSpeed);
      }
    }
    // Check if magnetometer has detected the robot has drifted off couse, and correct accordingly
    else if (event.magnetic.y < (Y_calibration - drift_threshold))
    {
      #ifdef debug
        Serial.println("Turn left");
      #endif
      left.write(leftForwardSpeed);
      right.write(rightForwardSpeed - turnBoost);
    }
    else if (event.magnetic.y > (Y_calibration + drift_threshold))
    {
      #ifdef debug
        Serial.println("Turn right");
      #endif
      left.write(leftForwardSpeed + turnBoost);
      right.write(rightForwardSpeed);
    }
    else
    {
      left.write(leftForwardSpeed);
      right.write(rightForwardSpeed);
    }
    // Check if robot has entered a board square
    if (event.magnetic.z < Z_threshold || event.magnetic.z < -400)
    {
      if (!crossing)
      {
        crossing = true;
      }
    }
    else
    {
      if (crossing)
      {
        crossing = false;
        count++;
      }
    }
    if (count >= spaces)
    {
      countdown--;
    }
    delay(6);
  }
  // Stop moving
  timeout.end();
  left.write(90);
  right.write(90);
}

// This is identical to drive forward, except the drive speeds and turn correction speeds are reversed
void driveBackward(uint8_t spaces)
{
  int Z_threshold = 0;
  uint8_t count = 0;
  bool crossing = false;
  sensors_event_t event;
  sensors_event_t event2;
  int X_calibration = 0;
  uint8_t countdown = 4;
  elapsedMillis mils;
  float turn_drift = 0;

   for (int i = 0; i < 10; i++)
  {
    mag2.getEvent(&event);
    X_calibration += event.magnetic.x;
    Z_threshold += event.magnetic.z;
    delay(10);
  }
  X_calibration /= 10;
  Z_threshold = (Z_threshold / 10) - Z_offset;

  timeUp = false;
  timeout.begin(timedOut, 1500000 * spaces);
   
  mils = 0;
  left.write(leftBackwardSpeed);
  right.write(rightBackwardSpeed);
  while (countdown > 0 && !timeUp)
  {
    gyro.getEvent(&event2);
    turn_drift += ((event2.gyro.z - gyroCorrect) / 1000) * mils;
    mils = 0;
    mag2.getEvent(&event);
    #ifdef debug
      Serial.print("Gryo: "); Serial.println(turn_drift);
      Serial.print("X: "); Serial.print(event.magnetic.x); Serial.print("  ");
      Serial.print("Y: "); Serial.print(event.magnetic.y); Serial.print("  ");
      Serial.print("Z: "); Serial.print(event.magnetic.z); Serial.print("  ");
      Serial.println("uT");
    #endif
    if (abs(turn_drift) > turn_drift_threshold)
    {
      if (turn_drift > 0)
      {
        #ifdef debug
          Serial.println("Gyro turn left");
        #endif
        right.write(rightBackwardSpeed);
        left.write(leftBackwardSpeed - turnBoost);      
      }
      else 
      {
        #ifdef debug
          Serial.println("Gyro turn right");
        #endif
        left.write(leftBackwardSpeed);
        right.write(rightBackwardSpeed + turnBoost);
      }
    }
    else if (event.magnetic.x > (X_calibration + drift_threshold))
    {
      #ifdef debug
        Serial.println("Turn left");
      #endif
      right.write(rightBackwardSpeed);
      left.write(leftBackwardSpeed - turnBoost);
    }
    else if (event.magnetic.x < (X_calibration - drift_threshold))
    {
      #ifdef debug
        Serial.println("Turn right");
      #endif
      left.write(leftBackwardSpeed);
      right.write(rightBackwardSpeed + turnBoost);
    }
    else
    {
      left.write(leftBackwardSpeed);
      right.write(rightBackwardSpeed);
    }
    if (event.magnetic.z < Z_threshold || event.magnetic.z < -350)
    {
      if (!crossing)
      {
        crossing = true;
      }
    }
    else
    {
      if (crossing)
      {
        crossing = false;
        count++;
      }
    }
    if (count >= spaces)
    {
      countdown--;
    }
    delay(6);
  }
  timeout.end();
  left.write(90);
  right.write(90);
}

void turn(uint8_t dir, uint8_t magnitude)
{
  // Determine the what the final reading of the gyro should be
  float threshold = magnitude * turnFactor;
  elapsedMillis mils;
  sensors_event_t event;
  float total = 0;
  mils = 0;
  // Start turning
  if (dir == 1)
  {
    left.write(leftBackwardSpeed);
    right.write(rightForwardSpeed);
  }
  else
  {
    left.write(leftForwardSpeed);
    right.write(rightBackwardSpeed);
  }
  // Check how far the robot has turned
  while (total < threshold)
  {
    gyro.getEvent(&event);
    total += abs(((event.gyro.z - gyroCorrect) / 1000) * mils);
    mils = 0;
    #ifdef debug
      Serial.println(total);
    #endif
    delay(7);
  }
  // Stop turning
  left.write(90);
  right.write(90);
}

// Used to stop the bot from driving forever
void timedOut()
{
  timeUp = true;
}
