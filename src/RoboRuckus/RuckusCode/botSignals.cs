using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using RoboRuckus.RuckusCode.Movement;

namespace RoboRuckus.RuckusCode
{
    public static class botSignals
    {
        /// <summary>
        /// Contains all the configuration options for a robot in setup mode.
        /// </summary>
        public enum configParameters
        {
            leftForwardSpeed,
            leftBackwardSpeed,
            rightForwardSpeed,
            rightBackwardSpeed,
            Z_threshold,
            turnBoost,
            drift_threshold,
            turn_drift_threshold,
            turnFactor,
            robotName,
            speedTest = 11,
            navTest = 12,
            quit = 13
        }

        // Used for thread control
        private static object _locker = new object();
        private static int _port = 8080;

        /// <summary>
        /// Sends a movement command to a robot
        /// </summary>
        /// <param name="order">The order to send</param>
        /// <returns>The response from the bot</returns>
        public static string sendMoveCommand(orderModel order)
        {
            return sendDataToRobot(order.botNumber, order.ToString());
        }

        /// <summary>
        /// Sends a damage value to a bot
        /// </summary>
        /// <param name="botNumber">The bot to send the value to</param>
        /// <param name="damage">The damage value</param>
        /// <returns>The response from the bot</returns>
        public static string sendDamage(int botNumber, sbyte damage)
        {
            return sendDataToRobot(botNumber, "4" + damage.ToString() + "0");
        }

        /// <summary>
        /// Assigns a player number to a bot
        /// </summary>
        /// <param name="botNumber">The bot to assign the player to</param>
        /// <param name="playerNumber">The player to assign</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool sendPlayerAssignment(int botNumber, int playerNumber)
        {
            return sendDataToRobot(botNumber, "0:" + playerNumber.ToString() + botNumber.ToString() + "\n") == "OK";
        }

        /// <summary>
        /// Sends a reset order to a bot
        /// </summary>
        /// <param name="botNumber">The bot to reset</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool sendReset(int botNumber)
        {
            return sendDataToRobot(botNumber, "002") == "OK";
        }

        /// <summary>
        /// Puts an unassigned robot into setup mode
        /// </summary>
        /// <param name="botNumber">The bot to enter setup mode</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool enterSetupMode(int botNumber, int port = 8080)
        {
            return sendDataToRobot(botNumber, "1:" + "\n") == "OK";
        }

        /// <summary>
        /// Updates an integer value configuration parameter to a robot in setup mode
        /// also sends movement test and the quit commands.
        /// </summary>
        /// <param name="botNumber">The robot to send the parameter to</param>
        /// <param name="parameter">The parameter to update</param>
        /// <param name="value">The new value</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool sendConfigParameter(int botNumber, configParameters parameter, int value)
        {
            if (parameter == configParameters.robotName || parameter == configParameters.Z_threshold || parameter == configParameters.turn_drift_threshold || parameter == configParameters.turnFactor)
            {
                throw new ArgumentException("Must be a parameter that takes an integer value", "parameter");
            }
            return sendDataToRobot(botNumber, ((int)parameter).ToString() + ":" + value.ToString()) == "OK";
        }

        // <summary>
        /// Updates a float value configuration parameter to a robot in setup mode
        /// </summary>
        /// <param name="botNumber">The robot to send the parameter to</param>
        /// <param name="parameter">The parameter to update</param>
        /// <param name="value">The new value</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool sendConfigParameter(int botNumber, configParameters parameter, float value)
        {
            if(parameter == configParameters.robotName || (parameter != configParameters.Z_threshold && parameter != configParameters.turn_drift_threshold && parameter != configParameters.turnFactor))
            {
                throw new ArgumentException("Must be a parameter that takes a float value", "parameter");
            }
            return sendDataToRobot(botNumber, ((int)parameter).ToString() + ":" + value.ToString()) == "OK";
        }

        // <summary>
        /// Updates a string value configuration parameter to a robot in setup mode (only robotName)
        /// </summary>
        /// <param name="botNumber">The robot to send the parameter to</param>
        /// <param name="parameter">The parameter to update</param>
        /// <param name="value">The new value</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool sendConfigParameter(int botNumber, configParameters parameter, string value)
        {
            if (parameter != configParameters.robotName)
            { 
                throw new ArgumentException("Must be the robot name for a string value", "parameter");
            }
            return sendDataToRobot(botNumber, ((int)parameter).ToString() + ":" + value.ToString()) == "OK";
        }

        /// <summary>
        /// Gets the current configuration settings from a robot in setup mode
        /// </summary>
        /// <param name="botNumber">The robot to get the settings from</param>
        /// <returns>A comma separated list of values</returns>
        public static string getRobotSettings(int botNumber)
        {
            return sendDataToRobot(botNumber, "10" + ":");
        }

        /// <summary>
        /// Adds a bot using IP
        /// </summary>
        /// <param name="ip">The IP address of the robot</param>
        /// <returns>An AK acknowledging the accpeted robot</returns>
        public static bool addBot(string ip, string name)
        {
            // Lock used so player assignment is sent after this method exits
            lock (_locker)
            {
                int result = gameStatus.addBot(ip, name);
                // Check if bot is already in pen
                if (result != -1 && !gameStatus.tuneRobots)
                {
                    // Check if bot already has player assigned
                    if ((result & 0x10000) != 0)
                    {
                        // Get assigned player number
                        int player = (result & 0xffff) >> 8;
                        // Get assigned bot number
                        result &= 255;
                        // Set thread to assign player to bot
                        new Thread(() => alreadyAssigned(player + 1, result)).Start();
                    }
                }
                // Send confirmation
                return true;
            }
        }

        /// <summary>
        /// Signals a bot has completed its move
        /// </summary>
        /// <param name="bot">The bot number</param>
        public static void Done(int bot)
        {
            // Signal bot has finished moving.
            gameStatus.robots[bot].moving.Set();
        }

        /// <summary>
        /// Sends a player assignment to a bot which
        /// has already had a player assigned previously
        /// </summary>
        /// <param name="player">The player assigned</param>
        /// <param name="bot">The bot the player is assigned to</param>
        private static void alreadyAssigned(int player, int bot)
        {
            lock (_locker)
            {
                // Wait for the bot server to become ready
                Thread.Sleep(500);
                SpinWait.SpinUntil(() => sendPlayerAssignment(bot, player), 1000);
            }
        }

        /// <summary>
        /// Sends data to a robot
        /// </summary>
        /// <param name="botNumber">The robot to send the data to</param>
        /// <param name="data">The data to send</param>
        /// <returns>The response from the robot or an empty string on failure</returns>
        private static string sendDataToRobot(int botNumber, string data)
        {
            Robot bot = gameStatus.robots[botNumber];
            if (gameStatus.botless)
            {
                bot.moving.Set();
                return "OK";
            }
            switch (bot.mode)
            {
                case Robot.communicationModes.IP:
                    return sendDataToRobotIP(bot, data);
            }
            return "";
        }

        /// <summary>
        /// Sends data to a robot via IP
        /// </summary>
        /// <param name="bot">The robot to send the data to</param>
        /// <param name="data">The data to send</param>
        /// <returns>The response from the robot or an empty string on failure</returns>
        private static string sendDataToRobotIP (Robot bot, string data)
        {
            byte[] responseBuffer = new byte[256];
            string response = "";
            using (Socket socketConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Byte[] bytesToSend = Encoding.ASCII.GetBytes(data);
                socketConnection.SendTimeout = 1010;
                socketConnection.ReceiveTimeout = 1010;
                try
                {
                    // Connect to bot
                    IAsyncResult result = socketConnection.BeginConnect(bot.robotAddress, _port, null, null);
                    if (result.AsyncWaitHandle.WaitOne(500, true) && socketConnection.Connected)
                    {
                        socketConnection.Send(bytesToSend, bytesToSend.Length, 0);

                        // Wait for all data to be available
                        int responseTimeout = 250;
                        if (gameStatus.tuneRobots)
                        {
                            responseTimeout = 500;
                        }
                        if (SpinWait.SpinUntil(() => socketConnection.Available > 1, responseTimeout))
                        {
                            while (socketConnection.Available > 1)
                            {
                                Int32 bytesRead = socketConnection.Receive(responseBuffer);
                                response += Encoding.ASCII.GetString(responseBuffer).TrimEnd().Replace("\0", "");
                            }
                        }
                        else
                        {
                            // Connection timed out
                            Console.WriteLine("Response from robot " + bot.robotNum.ToString() + " timed out.");
                            Thread.Sleep(25);
                            return "";
                        }
                    }
                    else
                    {
                        socketConnection.Close();
                        throw new TimeoutException("Connection to robot " + bot.robotNum.ToString() + " timed out.");
                    }
                }
                catch (Exception e)
                {
                    // Socket exception, could not connect to bot
                    Console.WriteLine("{0} Exception caught.", e);
                    return "";
                }
                return response.TrimStart(); ;
            }
        }
    }
}