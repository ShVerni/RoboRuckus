bool timeUp = false;

void driveForward(uint8_t spaces)
{
  int Z_threshold = 0;
  uint8_t count = 0;
  bool crossing = false;
  sensors_event_t event;
  sensors_event_t event2;
  int Y_calibration = 0;
  uint8_t countdown = 4;
  elapsedMillis mils;
  float turn_drift = 0;

   for (int i = 0; i < 10; i++)
  {
    mag.getEvent(&event);
    Y_calibration += event.magnetic.y;
    Z_threshold += event.magnetic.z;
    delay(10);
  }
  Y_calibration /= 10;
  Z_threshold = (Z_threshold / 10) - Z_offset;
  
  timeUp = false;
  timeout.begin(timedOut, 2000000 * spaces);
   
  mils = 0;
  left.write(leftForwardSpeed);
  right.write(rightForwardSpeed);
  while (countdown > 0 && timeUp)
  {
    gyro.getEvent(&event2);
    turn_drift += ((event2.gyro.z - 0.015) / 1000) * mils;
    mils = 0;
    mag.getEvent(&event);
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
    delay(5);
  }
  timeout.end();
  left.write(90);
  right.write(90);
}


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
  timeout.begin(timedOut, 2000000 * spaces);
   
  mils = 0;
  left.write(leftBackwardSpeed);
  right.write(rightBackwardSpeed);
  while (countdown > 0 && timeUp)
  {
    gyro.getEvent(&event2);
    turn_drift += ((event2.gyro.z - 0.015) / 1000) * mils;
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
    delay(5);
  }
  timeout.end();
  left.write(90);
  right.write(90);
}

void turn(uint8_t dir, uint8_t magnitude)
{
  float threshold = magnitude * turnFactor;
  elapsedMillis mils;
  sensors_event_t event;
  float total = 0;
  mils = 0;
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
  while (total < threshold)
  {
    gyro.getEvent(&event);
    total += abs(((event.gyro.z - 0.015) / 1000) * mils);
    mils = 0;
    #ifdef debug
      Serial.println(total);
    #endif
    delay(5);
  }
  left.write(90);
  right.write(90);
}

void timedOut()
{
  timeUp = true;
}
