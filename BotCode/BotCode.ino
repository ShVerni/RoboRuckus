#include <Servo.h>
#include <i2c_t3.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_HMC5883_U.h>
#include <Adafruit_L3GD20_U.h>

// Movement parameters and wheel speeds
uint8_t const leftForwardSpeed = 95;
uint8_t const rightForwardSpeed = 86;
uint8_t const rightBackwardSpeed = 99;
uint8_t const leftBackwardSpeed = 81;
// Forward and backward movement 
uint8_t const Z_offset = 45;
uint8_t const turnBoost = 3;
uint8_t const drift_threshold = 2;
float const turn_drift_threshold = 0.1;
// Turning
float const turnFactor = 1.45;

// Robot name, use URL encoding characters if needed  
String robotName = "Beta%20Bot";

/* Use to tune wheel speeds and above movment parameters */
//#define setup1
//#define setup2
//#define debug
//#define debug2

/*
 * LED pinout -> Shift register byte:
 * BL	128
 * BM	64
 * BR	32
 * M	8
 * TL	4
 * TM	2
 * TR	1
 * DP	16
*/

uint8_t const piezo = 5;
uint8_t const latchPin = 7;
uint8_t const clockPin = 8;
uint8_t const dataPin = 3;

Adafruit_HMC5883_Unified mag = Adafruit_HMC5883_Unified(31, 1);
Adafruit_HMC5883_Unified mag2 = Adafruit_HMC5883_Unified(32, 0);
Adafruit_L3GD20_Unified gyro = Adafruit_L3GD20_Unified(20);

uint8_t const numbers[] = {231, 33, 203, 107, 45, 110, 238, 35, 239, 111, 16};

uint8_t playerNumber = 0;

Servo left;
Servo right;

String server = "192.168.3.1";
String port = "8082";
String botNum;
String connection = "AT+CIPSTART=1,\"TCP\",\"" + server + "\"," + port;

bool started = false;

IntervalTimer timeout;

#define wifi Serial2

void setup()
{
  delay(1000);
  left.attach(4);
  right.attach(6);
  right.write(90);
  left.write(90);

  pinMode(latchPin, OUTPUT);
  pinMode(dataPin, OUTPUT);
  pinMode(clockPin, OUTPUT);
  
  Serial.begin(115200);
  
  pinMode(13, OUTPUT);
  digitalWrite(13, LOW);
  if (!mag.begin())
  {
    Serial.println(F("mag not detected"));
    while(true);
  }
  if (!mag2.begin())
  {
    Serial.println(F("mag2 not detected"));
    while(true);
  }
  if (!gyro.begin())
  {
    Serial.println(F("gyro not detected"));
    while(true);
  }

  /*
  * Your WiFi radio's baud rate might be different, default baud is
  * likely 9600, 57600, or 115200. Can be changed with AT+CIOBAUD=<baud>
  * When using software serial, maximum reliable baud is 9600
  */
  wifi.begin(115200);
  updateShiftRegister(16);
  
  #ifdef setup1
    delay(1000);
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
    while(true);
  #endif
  
  #ifdef setup2
    delay(1000);
    driveForward(2);
    delay(1000);
    driveBackward(1);
    delay(1000);
    turn(0, 1);
    delay(1000);
    turn(1, 1);
    delay(1000);
    turn(0, 2);
    while(true);
  #endif
  
  #ifndef debug2
  while (!startup())
  {
    updateShiftRegister(206); // Letter E
    delay(1000);
  }
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
  #ifdef debug2
  if (wifi.available())
  {
    while (wifi.available())
       {
         Serial.write(wifi.read()); // read the next character.
       }
  }

   if (Serial.available())
   {
     delay(1000);

     String command = "";

     while (Serial.available()) // read the command character by character
     {
       // read one character
       command += (char)Serial.read();
     }
     wifi.println(command); // send the read character to the esp8266
   }
   #endif
}

// Executes a moved recieved by the bot
void executeMove(uint8_t movement, uint8_t magnitude, uint8_t outOfTurn)
{
if (movement <= 3)
  {
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
      tone(piezo, 250, 1000);
      updateShiftRegister(206);
      delay(1000);    
    }
  }
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
    success = true;
    String response = sendCommand(connection, F("\nOK"));
    #ifdef debug
      Serial.println(response);
    #endif
    if (response.indexOf(F("FAIL")) != -1 || response.indexOf(F("ERROR")) != -1)
    {
      #ifdef debug
        Serial.println(F("Could not oppen connection"));
      #endif
      success = false;
    }
    else
    {
      // This should really be a POST request, but GET is more reliable
      String message = "GET /Bot/Done?bot=" + botNum + " HTTP/1.1\r\nHost: " + server + ":" + port + "\r\nConnection: close\r\n\r\n";
      response = sendCommand("AT+CIPSEND=1," + (String)message.length(), F("\nOK"));
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
        // Rest the WiFi module
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
  tone(piezo, 250, 400);
  if (damage < 10 && damage >= 0)
  {
    updateShiftRegister(numbers[damage]);
  }
  delay(800);
  tone(piezo, 250, 400);
  delay(200);    
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
  //Join WiFi network
  Serial.println(sendCommand(F("AT+CWJAP=\"RoboRuckus\","), F("\nOK")));

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
  delay(100);

  // Inform server of bot
  String message = "GET /Bot/Index?ip=" + client + "&name=" + robotName + " HTTP/1.1\r\nHost: " + server + ":" + port + "\r\nConnection: close\r\n\r\n";
  Serial.println(sendCommand("AT+CIPSEND=1," + (String)message.length(), F("\nOK")) + message);  
  String response = sendCommand(message, F("AK\n"));
  if (response.indexOf(F("ERROR")) != -1)
  {
    return false;
  }
  
  // Start server
  Serial.println(sendCommand(F("AT+CIPSERVER=1,8080"), F("\nOK")));

  return true;
}

/* Send command to WiFi module
 * command is the command to send (blank for read data back)
 * EoT is the End of Tranmission string that indicates
 * to stop reading. ERROR will always terminate.
 * If EoT is an empty string, the command will be sent
 * but the method won't wait for a response.
 */
String sendCommand(String command, String EoT)
{
  String response;
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
