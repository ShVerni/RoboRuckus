// Used to trigger a timeout so the bot won't drive forever if it misses a stop
bool timeUp = false;
// Corrects drift in the gyroscope
double gyroCorrect = 0.015;
// Used to prevent bot from slowing down too much
uint8_t rightForwardSpeed_max = rightForwardSpeed;
uint8_t rightBackwardSpeed_min = rightBackwardSpeed;


void driveForward(uint8_t spaces)
{
  // Initialize some variables
  int Z_threshold = 0;
  uint8_t count = 0;
  bool crossing = false;
  sensors_event_t event;
  sensors_event_t event2;
  int Y_calibration = 0;
  uint8_t countdown = 1;
  elapsedMillis mils;
  elapsedMillis speedAdjust;
  float turn_drift = 0;

  // Get starting Y and Z axis magnetometer readings, averaging 10 readings
  for (int i = 0; i < 10; i++)
  {
    mag.getEvent(&event);
    Y_calibration += event.magnetic.y;
    delay(10);
  }
  double cur = 0;
  for (int i = 0; i < 10; i++)
  {
    gyro.getEvent(&event2);
    cur += event2.gyro.z;
    delay(10);
  }
  gyroCorrect = cur / 10.0;  
  Y_calibration /= 10;
  
  // Define a maximum time the robot should drive
  timeUp = false;
  timeout.begin(timedOut, 1500000 * spaces);
   
  mils = 0;
  speedAdjust = 0;
  // Start driving
  left.write(leftForwardSpeed);
  right.write(rightForwardSpeed);

  // Make sure the robot is clear of the current square's magnet
  do
  {
    mag.getEvent(&event);
    delay(50);
    #ifdef debug
      Serial.println("Waiting...");
    #endif
  } while (event.magnetic.z <=  Z_threshold);
  // Start movement
  while (countdown > 0 && !timeUp)
  {
    Serial.println(mils);
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
    // Check if robot has entered a board square, making sure enough time has passed since the last square
    if (speedAdjust > 300 && event.magnetic.z <= Z_threshold)
    {
      if (!crossing)
      {
        crossing = true;
      }
      #ifdef debug
        Serial.println("Crossing");
      #endif
    }
    else
    {
      if (crossing)
      {
        // Check to see if the robot's speed needs to be adjusted
        uint16_t elapsed = speedAdjust;
        if (elapsed > 1500)
        {
          rightForwardSpeed--;
          leftForwardSpeed++;
        }
        else if (elapsed < 1000 && rightForwardSpeed < rightForwardSpeed_max)
        {
          rightForwardSpeed++;
          leftForwardSpeed--;
        }
        speedAdjust = 0;
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
  uint8_t countdown = 1;
  elapsedMillis mils;
  elapsedMillis speedAdjust;
  float turn_drift = 0;

   for (int i = 0; i < 10; i++)
  {
    mag2.getEvent(&event);
    X_calibration += event.magnetic.x;
    delay(10);
  }
  double cur = 0;
  for (int i = 0; i < 10; i++)
  {
    gyro.getEvent(&event2);
    cur += event2.gyro.z;
    delay(10);
  }
  gyroCorrect = cur / 10.0;  
  X_calibration /= 10;

  timeUp = false;
  timeout.begin(timedOut, 1500000 * spaces);
   
  mils = 0;
  speedAdjust = 0;
  left.write(leftBackwardSpeed);
  right.write(rightBackwardSpeed);

  do
  {
    mag.getEvent(&event);
    delay(50);
    #ifdef debug
      Serial.println("Waiting...");
    #endif
  } while (event.magnetic.z <=  Z_threshold);
  
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
        uint16_t elapsed = speedAdjust;
        if (elapsed > 1300)
        {
          rightBackwardSpeed++;
          leftBackwardSpeed--;
        }
        else if (elapsed < 950 && rightBackwardSpeed > rightBackwardSpeed_min)
        {
          rightBackwardSpeed--;
          leftBackwardSpeed++;
        }
        speedAdjust = 0;
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
  double cur = 0;
  for (int i = 0; i < 10; i++)
  {
    gyro.getEvent(&event);
    cur += event.gyro.z;
    delay(10);
  }
  gyroCorrect = cur / 10.0;  
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
