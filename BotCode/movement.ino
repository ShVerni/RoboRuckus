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
   
  mils = 0;
  left.write(leftForwardSpeed);
  right.write(rightForwardSpeed);
  while (countdown > 0)
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
   
  mils = 0;
  left.write(leftBackwardSpeed);
  right.write(rightBackwardSpeed);
  while (countdown > 0)
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

/*
 * Old code for magnetometer only based turning
void turn(uint8_t dir, uint8_t magnitude)
{
  sensors_event_t event;
  mag.getEvent(&event);
  int count;
  int z_count;
  bool z_trigger = false;
  float previous_y = event.magnetic.y;
  float previous_z = event.magnetic.z;
  #ifdef debug
    int counter = 0;
  #endif
  if (dir == 1)
  {
    left.write(leftBackwardSpeed);
    right.write(rightForwardSpeed);
    // Make sure we're on the upward slope
    do
    {
      delay(10);
      mag.getEvent(&event);
      if (event.magnetic.y > previous_y)
      {
        previous_z = event.magnetic.z;
        break;
      }
      #ifdef debug
        Serial.print(counter); Serial.print(","); Serial.println(event.magnetic.y); Serial.print(","); Serial.println(event.magnetic.z);
        counter++;
      #endif
      previous_y = event.magnetic.y;
    } while (true);
    for (int i = 0; i < magnitude; i++)
    {
      delay(50);
      count = 0;
      z_count = 0;
      z_trigger = false;
      do
      {
        delay(10);
        mag.getEvent(&event);
        if (event.magnetic.y < previous_y)
        {
          count++;
        }
        
        if (!z_trigger)
        {
          if (event.magnetic.z < previous_z)
          {
             z_trigger = true;   
          }
          previous_z = event.magnetic.z;         
        }
        else
        {
          z_count++;
        }
   
        #ifdef debug
          Serial.print(counter); Serial.print(","); Serial.println(event.magnetic.y); Serial.print(","); Serial.println(event.magnetic.z);
          counter++;
        #endif
        previous_y = event.magnetic.y;
      } while (count < turnCount && z_count < z_turnCount);

      #ifdef debug
        Serial.print(counter); Serial.print(",,"); Serial.println(event.magnetic.y); Serial.print(","); Serial.println(event.magnetic.z);
        counter++;
      #endif

      count = 0;
      z_count = 0;
      z_trigger = false;
      do
      {
        delay(10);
        mag.getEvent(&event);
        if (event.magnetic.y > previous_y)
        {
          count++;
        }
        
        if (!z_trigger)
        {
          if (event.magnetic.z > previous_z)
          {
             z_trigger = true;   
          }
          previous_z = event.magnetic.z;         
        }
        else
        {
          z_count++;
        }
        
        #ifdef debug
          Serial.print(counter); Serial.print(","); Serial.println(event.magnetic.y); Serial.print(","); Serial.println(event.magnetic.z);
          counter++;
        #endif
        previous_y = event.magnetic.y;
      } while (count < turnCount && z_count < z_turnCount);

      #ifdef debug
        Serial.print(counter); Serial.print(",,,"); Serial.println(event.magnetic.y); Serial.print(","); Serial.println(event.magnetic.z);
        counter++;
      #endif
    }
  }
  else if (dir == 0)
  {
    left.write(leftForwardSpeed);
    right.write(rightBackwardSpeed);
    // Make sure we're on the downward slope
    do
    {
      delay(10);
      mag.getEvent(&event);
      if (event.magnetic.y < previous_y)
      {
        previous_z = event.magnetic.z;
        break;
      }
      #ifdef debug
        Serial.print(counter); Serial.print(","); Serial.println(event.magnetic.y); Serial.print(","); Serial.println(event.magnetic.z);
        counter++;
      #endif
      previous_y = event.magnetic.y;
    } while (true);

    for (int i = 0; i < magnitude; i++)
    {
      delay(50);
      count = 0;
      z_count = 0;
      z_trigger = false;
      do
      {
        delay(10);
        mag.getEvent(&event);
        if (event.magnetic.y > previous_y)
        {
          count++;
        }
        
        if (!z_trigger)
        {
          if (event.magnetic.z > previous_z)
          {
             z_trigger = true;   
          }
          previous_z = event.magnetic.z;         
        }
        else
        {
          z_count++;
        }
        
        #ifdef debug
          Serial.print(counter); Serial.print(","); Serial.println(event.magnetic.y); Serial.print(","); Serial.println(event.magnetic.z);
          counter++;
        #endif
        previous_y = event.magnetic.y;
      } while (count < turnCount && z_count < z_turnCount);

      #ifdef debug
        Serial.print(counter); Serial.print(",,"); Serial.println(event.magnetic.y); Serial.print(","); Serial.println(event.magnetic.z);
        counter++;
      #endif
      delay(50);
      
      count = 0;
      z_count = 0;
      z_trigger = false;
      do
      {
        delay(10);
        mag.getEvent(&event);
        if (event.magnetic.y < previous_y)
        {
          count++;
        }
        
        if (!z_trigger)
        {
          if (event.magnetic.z < previous_z)
          {
             z_trigger = true;   
          }
          previous_z = event.magnetic.z;         
        }
        else
        {
          z_count++;
        }
        
        #ifdef debug
          Serial.print(counter); Serial.print(","); Serial.println(event.magnetic.y);
          counter++;
        #endif
        previous_y = event.magnetic.y;
      } while (count < turnCount && z_count < z_turnCount);

      #ifdef debug
        Serial.print(counter); Serial.print(",,,"); Serial.println(event.magnetic.y);
        counter++;
      #endif
    }
  }
  delay(turnDealy);
  left.write(90);
  right.write(90);
}
*/
