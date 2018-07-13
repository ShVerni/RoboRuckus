bool timeUp = false;
double gyroCorrect =  -7.1;

uint8_t rightForwardSpeed_max = rightForwardSpeed;
uint8_t rightBackwardSpeed_min = rightBackwardSpeed;

void driveForward(uint8_t spaces)
{
  sensors_event_t accel, mag2, gyro, temp;
  lsm.getEvent(&accel, &mag2, &gyro, &temp);
  
  int Z_threshold = 0;
  uint8_t count = 0;
  bool crossing = false;
  sensors_event_t event;
  int Y_calibration = 0;
  uint8_t countdown = 5;
  elapsedMillis mils;
  elapsedMillis speedAdjust;
  float turn_drift = 0;

  for (int i = 0; i < 10; i++)
  {
    mag.getEvent(&event);
    Y_calibration += event.magnetic.y;
    delay(10);
  }
  
  double cur = 0;
  for (int i = 0; i < 10; i++)
  {
    lsm.getEvent(&accel, &mag2, &gyro, &temp);
    cur += gyro.gyro.z;
    delay(10);
  }
  gyroCorrect = cur / 10.0;  
  Y_calibration /= 10;
  
  timeUp = false;
  timeout.begin(timedOut, 881000 * spaces);
   
  left.write(leftForwardSpeed);
  right.write(rightForwardSpeed);

  mils = 0;
  speedAdjust = 0;

  while (event.magnetic.z < Z_threshold) 
  {
    delay(50);
    #ifdef debug
      Serial.println("Waiting...");
    #endif
    mag.getEvent(&event);
  }
  
  while (countdown > 0 && !timeUp)
  {
    lsm.getEvent(&accel, &mag2, &gyro, &temp);
    turn_drift += ((gyro.gyro.z - gyroCorrect) / 1000) * mils;
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
    else if (event.magnetic.y < (Y_calibration + drift_threshold))
    {
      #ifdef debug
        Serial.println("Turn left");
      #endif
      left.write(leftForwardSpeed);
      right.write(rightForwardSpeed - turnBoost);
    }
    else if (event.magnetic.y > (Y_calibration - drift_threshold_backup))
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
    if (speedAdjust > 350 && event.magnetic.z <= Z_threshold)
    {
      if (!crossing)
      {
        crossing = true;
      }
      #ifdef debug
        Serial.println("<<<Crossing");
      #endif
    }
    else
    {
      if (crossing)
      {
        uint16_t elapsed = speedAdjust;
        if (elapsed > 1300)
        {
          rightForwardSpeed--;
          leftForwardSpeed++;
        }
        else if (elapsed < 1000 && rightForwardSpeed < rightForwardSpeed_max)
        {
          rightForwardSpeed++;
          leftForwardSpeed--;
        }
        #ifdef debug
          Serial.println(">>>Done Crossing");
        #endif
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

void driveBackward(uint8_t spaces)
{
  sensors_event_t accel, mag2, gyro, temp;
  lsm.getEvent(&accel, &mag2, &gyro, &temp);
  
  int Z_threshold = 0;
  uint8_t count = 0;
  bool crossing = false;
  int Y_calibration = 0;
  uint8_t countdown = 5;
  elapsedMillis mils;
  elapsedMillis speedAdjust;
  float turn_drift = 0;

   for (int i = 0; i < 10; i++)
  {
    lsm.getEvent(&accel, &mag2, &gyro, &temp);
    Y_calibration += mag2.magnetic.y;
    delay(10);
  }

  double cur = 0;
  for (int i = 0; i < 10; i++)
  {
    lsm.getEvent(&accel, &mag2, &gyro, &temp);
    cur += gyro.gyro.z;
    delay(10);
  }
  gyroCorrect = cur / 10.0;
  
  Y_calibration /= 10;

  timeUp = false;
  timeout.begin(timedOut, 1001000 * spaces);
   
  mils = 0;
  speedAdjust = 0;
  
  left.write(leftBackwardSpeed);
  right.write(rightBackwardSpeed);
  
  lsm.getEvent(&accel, &mag2, &gyro, &temp);
  while (mag2.magnetic.z >=  Z_threshold_backup) 
  {
    delay(50);
    #ifdef debug
      Serial.println("Waiting...");
    #endif
    lsm.getEvent(&accel, &mag2, &gyro, &temp);
  }
  
  while (countdown > 0 && !timeUp)
  {
    lsm.getEvent(&accel, &mag2, &gyro, &temp);
    turn_drift += ((gyro.gyro.z - gyroCorrect) / 1000) * mils;
    mils = 0;
    #ifdef debug
      Serial.print("Gryo: "); Serial.println(turn_drift);
      Serial.print("X: "); Serial.print(mag2.magnetic.x); Serial.print("  ");
      Serial.print("Y: "); Serial.print(mag2.magnetic.y); Serial.print("  ");
      Serial.print("Z: "); Serial.print(mag2.magnetic.z); Serial.print("  ");
      Serial.println("uT");
    #endif
    if (abs(turn_drift) > turn_drift_threshold)
    {
      if (turn_drift < 0)
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
    else if (mag2.magnetic.y > (Y_calibration - drift_threshold_backup))
    {
      #ifdef debug
        Serial.println("Turn left");
      #endif
      right.write(rightBackwardSpeed);
      left.write(leftBackwardSpeed - turnBoost);
    }
    else if (mag2.magnetic.y < (Y_calibration + drift_threshold_backup))
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
    if (speedAdjust > 350 && abs(mag2.magnetic.z) >= Z_threshold_backup)
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
        if (elapsed > 1200)
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
  sensors_event_t accel, mag2, gyro, temp;
  
  float threshold = magnitude * turnFactor;
  elapsedMillis mils;
  float total = 0;
  double cur = 0;
  
  for (int i = 0; i < 10; i++)
  {
    lsm.getEvent(&accel, &mag2, &gyro, &temp);
    cur += gyro.gyro.z;
    delay(10);
  }
  gyroCorrect = cur / 10.0;
  
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
    lsm.getEvent(&accel, &mag2, &gyro, &temp);
    total += abs(((gyro.gyro.z - gyroCorrect) / 1000) * mils);
    mils = 0;
    #ifdef debug
      Serial.println(total);
    #endif
    delay(7);
  }
  left.write(90);
  right.write(90);
}

void timedOut()
{
  timeUp = true;
}
