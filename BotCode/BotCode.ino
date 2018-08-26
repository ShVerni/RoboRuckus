#include <Servo.h>
#include <EEPROM.h>
#include <Adafruit_HMC5883_U.h>
#include <Adafruit_LSM9DS1.h>

// Movement parameters and wheel speeds
uint8_t leftForwardSpeed = EEPROM.read(100);
uint8_t rightForwardSpeed = EEPROM.read(101);
uint8_t rightBackwardSpeed = EEPROM.read(102);
uint8_t leftBackwardSpeed = EEPROM.read(103);
// Forward and backward movement 
int16_t Z_threshold = 0;
float const Z_threshold_backup = 2.5;
uint8_t turnBoost = EEPROM.read(106);
uint8_t drift_threshold = EEPROM.read(107);
float const drift_threshold_backup = 0.2;
float turn_drift_threshold = 0.0;
// Turning
float turnFactor = 0.0;


// Robot name, use URL encoding characters if needed  
String robotName = "";

/* Use to debug */
//#define debug
//#define debug2

/*
 * LED pinout -> Shift register byte reference:
 * BL	128
 * BM	64
 * BR	32
 * M	8
 * TL	4
 * TM	2
 * TR	1
 * DP	16
*/

// Pin assignments and constants
uint8_t setupBtn = 0;
uint8_t const latchPin = 7;
uint8_t const clockPin = 8;
uint8_t const dataPin = 3;

Adafruit_HMC5883_Unified mag = Adafruit_HMC5883_Unified(31, 1);
Adafruit_LSM9DS1 lsm = Adafruit_LSM9DS1(&Wire, 0);

uint8_t const numbers[] = {231, 33, 203, 107, 45, 110, 238, 35, 239, 111, 16};

uint8_t playerNumber = 0;

Servo left;
Servo right;

String server = "192.168.3.1";
String port = "8082";
String botNum;
String connection = "AT+CIPSTART=1,\"TCP\",\"";

bool started = false;

IntervalTimer timeout;

#define wifi Serial2

// Let's begin
void setup()
{  
  Serial.begin(115200);
  // Get remaining value from EEPROM
  EEPROM.get(104, Z_threshold);
  EEPROM.get(108, turn_drift_threshold);
  EEPROM.get(112, turnFactor);
  robotName = loadName();
  
  // Set default values if none are set
  if (leftForwardSpeed == 0 || leftForwardSpeed > 180)
  {
    leftForwardSpeed = 94;
    rightForwardSpeed = 85;
    rightBackwardSpeed = 94;
    leftBackwardSpeed = 85;
    Z_threshold = -100;
    turnBoost = 4;
    drift_threshold = 1;
    turnFactor = 80;
    turn_drift_threshold = 12;
    robotName = "Beta%20Bot";
    delay(100);
    saveParameters();
  }
  Serial.println("Loaded EEEPROM");

  // Attach and initialize servos
  delay(1000);
  left.attach(4);
  right.attach(6);
  right.write(90);
  left.write(90);

  // Set up LED shift register
  pinMode(latchPin, OUTPUT);
  pinMode(dataPin, OUTPUT);
  pinMode(clockPin, OUTPUT);

  // Set up setup mode button
  //pinMode(setupBtn, INPUT_PULLUP);
  
  // Turn off onbaord LED
  pinMode(13, OUTPUT);
  digitalWrite(13, LOW);
  
  if (!mag.begin())
  {
    Serial.println(F("Mag not detected ... Check your wiring or I2C ADDR!"));
    while(true);
  }  
  mag.setMagGain(HMC5883_MAGGAIN_4_7);
  if(!lsm.begin())
  {
    Serial.print(F("No LSM9DS1 detected ... Check your wiring or I2C ADDR!"));
    while(true);
  }
  lsm.setupAccel(lsm.LSM9DS1_ACCELRANGE_8G);
  lsm.setupMag(lsm.LSM9DS1_MAGGAIN_4GAUSS);
  lsm.setupGyro(lsm.LSM9DS1_GYROSCALE_500DPS);

  /*
  * Your WiFi radio's baud rate might be different, default baud is
  * likely 9600, 57600, or 115200. Can be changed with AT+CIOBAUD=<baud>
  * When using software serial, maximum reliable baud is 9600
  */
  wifi.begin(115200);
  updateShiftRegister(16);
  
  // Create connection string
  connection = connection + server + "\"," + port;

  // Check for setup/tuning mode
  if (false)//!digitalRead(setupBtn))
  {
    Serial.println("Entering setup");
    softAPSetup();
  }
  
  #ifndef debug2
    // Initialize robot
    while (!startup())
    {
      // Robot encountered a problem during set up
      updateShiftRegister(206); // Letter E
      delay(1000);
    }
    // Robot is ready to go
    updateShiftRegister(175); // Letter A
  #endif
}

void loop()
{
  #ifndef debug2
    if (wifi.available()) // Check if the WiFi has a message
    {
      if (wifi.find("+IPD,0,")) // Check if it's a message from the server
      {
        delay(10);
        wifi.find(":");
        if (started)
        {
          uint8_t movement = wifi.read() - 48; // Convert from ASCII to Int
          uint8_t magnitude = wifi.read() - 48;
          uint8_t outOfTurn = wifi.read() - 48;
          #ifdef debug
            Serial.print(F(">>>>>>>Order: "));
            Serial.print(movement);
            Serial.print(magnitude);
            Serial.println(outOfTurn);
          #endif
          // Empty the buffer, just in case
          while (wifi.available())
          {
            wifi.read(); // read the next character
          }
          // Respond and close the connection
          #ifdef debug
            Serial.println(sendCommand(F("AT+CIPSEND=0,2"), F("\nOK")));
            Serial.println(sendCommand(F("OK"), F("CLOSED")));
          #else
            sendCommand(F("AT+CIPSEND=0,2"), F("\nOK"));
            sendCommand(F("OK"), F("CLOSED"));
          #endif
          if (outOfTurn == 2)
          {
            // Bot received reset command
            started = false;
            updateShiftRegister(175); // Letter A
          }
          else
          {
            // Bot received move order
            executeMove(movement, magnitude, outOfTurn);
          }
        }
        else
        {
          String instruction_str = wifi.readStringUntil(':');
          uint8_t instruction = instruction_str.toInt();
          if (instruction == 0)
          {
            // Get assigned player
            playerNumber = wifi.read() - 48;
            // Get assigned bot number
            botNum = wifi.readStringUntil('\n');
            updateShiftRegister(numbers[playerNumber]);
            // Empty the buffer, just in case
            while (wifi.available())
            {
              wifi.read(); // Read the next character
            }
            // Respond and close the connection
            #ifdef debug
              Serial.print(F("Bot number: "));
              Serial.println(botNum);
              Serial.print(F("Player number: "));
              Serial.println(playerNumber);
              Serial.println(sendCommand(F("AT+CIPSEND=0,2"), F("\nOK")));
              Serial.println(sendCommand(F("OK"), F("CLOSED")));
            #else
              sendCommand(F("AT+CIPSEND=0,2"), F("\nOK"));
              sendCommand(F("OK"), F("CLOSED"));
            #endif
            started = true;
          }
          else if (instruction == 1)
          {
            // Empty the buffer, just in case
            while (wifi.available())
            {
              wifi.read(); // read the next character
            }
            sendCommand(F("AT+CIPSEND=0,2"), F("\nOK"));
            sendCommand(F("OK"), F("CLOSE"));
            Serial.println(F("Entering setup mode"));
            setupMode();
          }
        }
      }
      else
      {
        // Empty the buffer
        while (wifi.available())
        {
          wifi.read(); // Read the next character
        }
      }
    }
  #endif
  // Enable debug2 to communicate directly to the WiFi module over the serial console
  #ifdef debug2
    if (wifi.available())
    {
      while (wifi.available())
         {
           Serial.write(wifi.read()); // Read the next character.
         }
    }
  
     if (Serial.available())
     {
       delay(1000);
  
       String command = "";
  
       while (Serial.available()) // Read the command character by character
       {
         // read one character
         command += (char)Serial.read();
       }
       wifi.println(command); // Send the read character to the esp8266
     }
   #endif
}

// Executes a moved received by the bot
void executeMove(uint8_t movement, uint8_t magnitude, uint8_t outOfTurn)
{
if (movement <= 3)
  {
    // Standard movement
    if (magnitude > 0)
    {
      switch (movement)
      {
        case 0:
          // Left
          turn(1, magnitude);
          break;
        case 1:
          // Right
          turn(0, magnitude);
          break;
        case 2:
          // Forward
          driveForward(magnitude);
          break;
        case 3:
          // Backup
          driveBackward(magnitude);
          break;
      }
    }
    else
    {
      // Robot trying to move, but is blocked
      updateShiftRegister(206);
      delay(1000);    
    }
  }
  // Non-movment command
  else 
  {
    switch (movement)
    {
      case 4:
         // Damage update
         takeDamage(magnitude); 
         break;
    }
  }
  delay(1000);
  // Forces a response to the server, resets the WiFi on fail
  updateShiftRegister(numbers[playerNumber]);
  bool success = true;
  do
  {
    // Ensure connection isn't already open
    sendCommand(F("AT+CIPCLOSE=1"), F("\nOK"));
    success = true;
    // Open TCP connection to server
    String response = sendCommand(connection, F("\nOK"));
    #ifdef debug
      Serial.println(response);
    #endif
    if (response.indexOf(F("FAIL")) != -1 || response.indexOf(F("ERROR")) != -1)
    {
      #ifdef debug
        Serial.println(F("Could not open connection"));
      #endif
      success = false;
    }
    else
    {
      // This should really be a POST request, but GET is more reliable
      String message = "GET /Bot/Done?bot=";
      message = message + botNum + " HTTP/1.1\r\nHost: " + server + ":" + port + "\r\nConnection: close\r\n\r\n";
      String command = "AT+CIPSEND=1,";
      response = sendCommand(command + message.length(), F("\nOK"));
      #ifdef debug
        Serial.println(response);
      #endif
      if (response.indexOf(F("ERROR")) != -1)
      {
        Serial.println(F("Could not send message"));
        success = false;
      }
      else
      {
        // Notify server that bot has finished moving, check for acknowledgment
        response = sendCommand(message, F("AK\n"));
        #ifdef debug
          Serial.println(response);
        #endif
        if (response.indexOf(F("ERROR")) != -1)
        {
          #ifdef debug
            Serial.println(F("No AK response"));
          #endif
          success = false;
        }
      }
      if (!success)
      {
        // Something went wrong, try resetting the WiFi module
        #ifdef debug
          Serial.println(F("Resetting"));
        #endif
        delay(350);
        #ifdef debug
          Serial.println(sendCommand(F("AT+CIPMUX=1"), F("\nOK")));
          Serial.println(sendCommand(F("AT+CIPSERVER=1,8080"), F("\nOK")));
        #else
          sendCommand(F("AT+CIPMUX=1"), F("\nOK"));
          sendCommand(F("AT+CIPSERVER=1,8080"), F("\nOK"));
        #endif
      }
    }
  } while (!success);
}

//Shows the damage the bot has taken
void takeDamage(int damage)
{
  if (damage < 10 && damage >= 0)
  {
    updateShiftRegister(numbers[damage]);
  }
  delay(800);
}

// Sets up the robot, connecting to the Wi-Fi, informing the server of itself, and so on.
bool startup()
{
  // Empty buffer
  while (wifi.available())
  {
    wifi.read(); // Read the next character
  }
  
  // Stop server just in case
  Serial.println(sendCommand(F("AT+CIPSERVER=0"), F("\nOK")));

  // Initialize radio
  Serial.println(sendCommand(F("AT+CWMODE=1"), F("\nOK")));

  // Disable DHCP server
  Serial.println(sendCommand(F("AT+CWDHCP=1,1"), F("\nOK")));  
  
  // Restart the module to enable changes
  Serial.println(sendCommand(F("AT+RST"), F("\nOK")));
  delay(2000);
  
  // Join WiFi network
  Serial.println(sendCommand(F("AT+CWJAP=\"RoboRuckus\","), F("\nOK")));

  // Swap this with the above line for a protected network
  // Serial.println(sendCommand(F("AT+CWJAP=\"RoboRuckus\",\"passphrase\""), F("\nOK")));

  // Enable multiplexing (necessary for server operations)
  Serial.println(sendCommand(F("AT+CIPMUX=1"), F("\nOK")));

  // Get assigned IP address
  wifi.println(F("AT+CIPSTA?"));
  wifi.find("ip:\"");
  String client = wifi.readStringUntil('"');
  // Empty buffer
  while (wifi.available())
  {
    wifi.read(); // Read the next character
  }
  Serial.println("IP: " + client);

  // Connect to server
  Serial.println(sendCommand(connection, F("\nOK")));
  delay(200);

  // Inform server of bot
  String message = "GET /Bot/Index?ip=";
  message = message + client + "&name=" + robotName + " HTTP/1.1\r\nHost: " + server + ":" + port + "\r\nConnection: close\r\n\r\n";
  Serial.println(message);
  String command = "AT+CIPSEND=1,";
  Serial.println(sendCommand(command + message.length(), F("\nOK")));  
  String response = sendCommand(message, F("AK\n"));
  Serial.println(response);
  if (response.indexOf(F("ERROR")) != -1)
  {
    return false;
  }
  
  // Start server
  Serial.println(sendCommand(F("AT+CIPSERVER=1,8080"), F("\nOK")));

  return true;
}

// Configures the software access point
void softAPSetup()
{
  // Empty buffer
  while (wifi.available())
  {
    wifi.read(); // Read the next character
  }
  
  // Stop server just in case
  Serial.println(sendCommand(F("AT+CIPSERVER=0"), F("\nOK")));

  // Initialize radio
  Serial.println(sendCommand(F("AT+CWMODE=2"), F("\nOK")));
  
  // Restart the module to enable changes
  Serial.println(sendCommand(F("AT+RST"), F("\nOK")));
  delay(3000);
  
  // Create WiFi network softAP
  Serial.println(sendCommand(F("AT+CWSAP=\"RuckusSetup\",\"Ruckus_C0nf\",5,3"), F("\nOK")));
  
  // Set the softAP IP address
   Serial.println(sendCommand(F("AT+CIPAP=\"192.168.3.1\""), F("\nOK")));
   
  // Enable DHCP server
  Serial.println(sendCommand(F("AT+CWDHCP=0,1"), F("\nOK")));
  
  // Enable multiplexing (necessary for server operations)
  Serial.println(sendCommand(F("AT+CIPMUX=1"), F("\nOK")));

  // Start server
  Serial.println(sendCommand(F("AT+CIPSERVER=1,8080"), F("\nOK")));

  // Enter setup mode
  setupMode();
}

// Allows robot to be configured
void setupMode()
{
  // Dsiplay ready
  updateShiftRegister(numbers[0]);
  
  // Start listening for instructions
  bool quit = false;
  while (!quit) {
    if (wifi.available()) // Check if the WiFi has a message
    {
      if (wifi.find("+IPD,0,")) // Check if it's a message from the server
      {
        delay(10);
  
        // Read instruction
        wifi.find(":");
        String instruction_str = wifi.readStringUntil(':');
        uint8_t instruction = instruction_str.toInt();
        Serial.print("Instruction received: ");
        Serial.println(instruction);
  
        // Read data
        String data = "";
        while (wifi.available())
        {
          data += (char)wifi.read();
        }       

        // Process instruction
        bool success = false;
        uint8_t newInt = 0;
        float newFloat = 0.0;
        char buff[32];
        String curStatus = "";
        String command = "AT+CIPSEND=0,";
        Serial.print("Data: ");
        Serial.println(data);
        switch(instruction) 
        {
          case 0:
            newInt = (uint8_t)data.toInt();
            if (newInt != 0)
            {
              leftForwardSpeed = newInt;
              success = true;
            }
            break;
          case 1:
            newInt = (uint8_t)data.toInt();
            if (newInt != 0)
            {
              leftBackwardSpeed = newInt;
              success = true;
            }
            break;
          case 2:
            newInt = (uint8_t)data.toInt();
            if (newInt != 0)
            {
              rightForwardSpeed = newInt;
              success = true;
            }
            break;
          case 3:
            newInt = (uint8_t)data.toInt();
            if (newInt != 0)
            {
              rightBackwardSpeed = newInt;
              success = true;
            }
            break;
          case 4:
            data.toCharArray(buff, sizeof(buff));
            newFloat = atof(buff);
            if (newFloat != 0.0)
            {
              Z_threshold = (int16_t)newFloat;
              success = true;
            }
            break;
          case 5:
            newInt = (uint8_t)data.toInt();
            if (newInt != 0)
            {
              turnBoost = newInt;
              success = true;
            }
            break;
          case 6:
            newInt = (uint8_t)data.toInt();
            if (newInt != 0)
            {
              drift_threshold = newInt;
              success = true;
            }
            break;
          case 7:
            data.toCharArray(buff, sizeof(buff));
            newFloat = atof(buff);
            if (newFloat != 0.0)
            {
              turn_drift_threshold = newFloat;
              success = true;
            }
            break;
          case 8:
            data.toCharArray(buff, sizeof(buff));
            newFloat = atof(buff);
            if (newFloat != 0.0)
            {
              turnFactor = newFloat;
              success = true;
            }
            break;
          case 9:
            if (data != "" && data.length() < 100)
            {
              robotName = data.trim();
              success = true;
            }
            break;
          case 10:
            curStatus = curStatus + leftForwardSpeed + "," + rightForwardSpeed + "," + rightBackwardSpeed + "," + leftBackwardSpeed + "," + Z_threshold + "," + turnBoost + "," + drift_threshold + "," + turn_drift_threshold + "," + turnFactor + "," + robotName + "\n";
            command = command + curStatus.length();
            Serial.println(sendCommand(command, F("\nOK")));
            Serial.println(sendCommand(curStatus, F("CLOSE")));
            break;
          case 11:
            delay(50);
            Serial.println(sendCommand(F("AT+CIPSEND=0,2"), F("\nOK")));
            Serial.println(sendCommand(F("OK"), F("CLOSE")));
            speedTest();
            break;
          case 12:
            delay(50);
            Serial.println(sendCommand(F("AT+CIPSEND=0,2"), F("\nOK")));
            Serial.println(sendCommand(F("OK"), F("CLOSE")));
            navTest();
            break;
          case 13:
            quit = true;
            success = true;
            break;
        }

        // Respond and close the connection
        if (instruction < 10 || instruction > 12)
        {
          if (success)
          {
            Serial.println(sendCommand(F("AT+CIPSEND=0,2"), F("\nOK")));
            Serial.println(sendCommand(F("OK"), F("CLOSE")));
          }
          else
          {
            Serial.println(sendCommand(F("AT+CIPSEND=0,5"), F("\nOK")));
            Serial.println(sendCommand(F("ER"), F("CLOSE")));
          }
        }
        else
        {
          Serial.println(sendCommand(F("AT+CIPSERVER=1,8080"), F("\nOK"))); // May be unnecessary
        }
        // Empty the buffer, just in case
        while (wifi.available())
        {
          wifi.read(); // Read the next character
        }
      }
      else
      {
        // Empty the buffer
        while (wifi.available())
        {
          wifi.read(); // Read the next character
        }
      }
    }
  }
  saveParameters();
  updateShiftRegister(16);
}


// Has the robot drive forward and backward to see if it drives staight
// and at a good speed.
void speedTest()
{
  left.write(leftForwardSpeed);
  right.write(rightForwardSpeed);
  delay(3000);
  left.write(90);
  right.write(90);
  delay(500);
  left.write(leftBackwardSpeed);
  right.write(rightBackwardSpeed);
  delay(3000);
  left.write(90);
  right.write(90);
}

// Has the robot drive a pattern on the bord to test its navigation
void navTest()
{
  driveForward(2);
  delay(1000);
  driveBackward(1);
  delay(1000);
  turn(0, 1);
  delay(1000);
  turn(1, 1);
  delay(1000);
  turn(0, 2);
}


/* Send command to WiFi module
 * command is the command to send (blank for read data back)
 * EoT is the End of Tranmission string that indicates
 * to stop reading. ERROR will always terminate.
 * If EoT is an empty string, the command will be sent
 * but the method won't wait for a response.
 * The method will timeout after ~8 seconds of not finding EoT or ERROR
 */
String sendCommand(String command, String EoT)
{
  String response = "";
  if (command != F(""))
  {
    wifi.println(command);
    delay(5);
  }
  if (EoT != F(""))
  {
    int i = 0;
    while (response.indexOf(F("ERROR")) == -1 && response.indexOf(EoT) == -1 && i < 8000)
    {
      while (wifi.available())
      {
        response += (char)wifi.read();
      }
      i++;
      delay(1);
    }
    if (i == 8000)
    {
      return F("ERROR");
    }
  }
  return response;
}

// Updates shift register with byte
void updateShiftRegister(byte data)
{
  digitalWrite(latchPin, LOW);
  shiftOut(dataPin, clockPin, LSBFIRST, data);
  digitalWrite(latchPin, HIGH);
}


// Saves all robot settings to the EEPROM 
void saveParameters()
{
  Serial.println("Saving EEPROM");
  EEPROM.update(100, leftForwardSpeed);
  EEPROM.update(101, rightForwardSpeed);
  EEPROM.update(102, rightBackwardSpeed);
  EEPROM.update(103, leftBackwardSpeed);
  EEPROM.put(104, Z_threshold);
  EEPROM.update(106, turnBoost);
  EEPROM.update(107, drift_threshold);
  EEPROM.put(108, turn_drift_threshold);
  EEPROM.put(112, turnFactor);

  // Save the robot name
  int str_len = robotName.length() + 1;
  char char_array[str_len];
  robotName.toCharArray(char_array, str_len);
  for (int i = 0; i < str_len; i++)
  {
    EEPROM.update(i, char_array[i]);
  }
  EEPROM.update(str_len + 1, 0x00);
}


// Loads the robot's name from the EEPROM
String loadName()
{
   String strBuffer = "";
   char char_buffer = '\0';
   int i = 0;
   do
   {
      char_buffer = EEPROM.read(i);
      strBuffer = strBuffer + char_buffer;
      i++;
   } while (char_buffer != 0x00 && char_buffer != '\0' && i < 100);
   strBuffer.trim();
   // I don't know why, but if the string isn't copied to a char[] and back, it won't work properly
   int str_len = strBuffer.length() + 1;
   char char_array[str_len];
   strBuffer.toCharArray(char_array, str_len);
   String result(char_array);
   return result;
}


