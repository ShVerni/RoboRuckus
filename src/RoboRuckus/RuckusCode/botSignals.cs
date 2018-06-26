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
        /// Contains all the configuration options for a robot ins setup mode.
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

        /// <summary>
        /// Sends a movement command to a robot
        /// </summary>
        /// <param name="order">The order to send</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>The response from the bot</returns>
        public static string sendMoveCommand(orderModel order, int port = 8080)
        {
            return sendDataToRobot(order.botNumber, order.ToString(), port);
        }

        /// <summary>
        /// Sends a damage value to a bot
        /// </summary>
        /// <param name="botNumber">The bot to send the value to</param>
        /// <param name="damage">The damage value</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>The response from the bot</returns>
        public static string sendDamage(int botNumber, sbyte damage, int port = 8080)
        {
            return sendDataToRobot(botNumber, "4" + damage.ToString() + "0", port);
        }

        /// <summary>
        /// Assigns a player number to a bot
        /// </summary>
        /// <param name="botNumber">The bot to assign the player to</param>
        /// <param name="playerNumber">The player to assign</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool sendPlayerAssignment(int botNumber, int playerNumber, int port = 8080)
        {
            return sendDataToRobot(botNumber, "0:" + playerNumber.ToString() + botNumber.ToString() + "\n", port) == "OK";
        }

        /// <summary>
        /// Sends a reset order to a bot
        /// </summary>
        /// <param name="botNumber">The bot to reset</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool sendReset(int botNumber, int port = 8080)
        {
            return sendDataToRobot(botNumber, "002", port) == "OK";
        }

        /// <summary>
        /// Puts an unassigned robot into setup mode
        /// </summary>
        /// <param name="botNumber">The bot to enter setup mode</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool enterSetupMode(int botNumber, int port = 8080)
        {
            return sendDataToRobot(botNumber, "1:" + "\n", port) == "OK";
        }

        /// <summary>
        /// Updates an integer value configuration parameter to a robot in setup mode
        /// also sends movement test and the quit commands.
        /// </summary>
        /// <param name="botNumber">The robot to send the parameter to</param>
        /// <param name="parameter">The parameter to update</param>
        /// <param name="value">The new value</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool sendConfigParameter(int botNumber, configParameters parameter, int value, int port = 8080)
        {
            if (parameter == configParameters.robotName || parameter == configParameters.Z_threshold || parameter == configParameters.turn_drift_threshold || parameter == configParameters.turnFactor)
            {
                throw new ArgumentException("Must be a parameter that takes an integer value", "parameter");
            }
            return sendDataToRobot(botNumber, ((int)parameter).ToString() + ":" + value.ToString(), port) == "OK";
        }

        // <summary>
        /// Updates a float value configuration parameter to a robot in setup mode
        /// </summary>
        /// <param name="botNumber">The robot to send the parameter to</param>
        /// <param name="parameter">The parameter to update</param>
        /// <param name="value">The new value</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool sendConfigParameter(int botNumber, configParameters parameter, float value, int port = 8080)
        {
            if(parameter == configParameters.robotName || (parameter != configParameters.Z_threshold && parameter != configParameters.turn_drift_threshold && parameter != configParameters.turnFactor))
            {
                throw new ArgumentException("Must be a parameter that takes a float value", "parameter");
            }
            return sendDataToRobot(botNumber, ((int)parameter).ToString() + ":" + value.ToString(), port) == "OK";
        }

        // <summary>
        /// Updates a string value configuration parameter to a robot in setup mode (only robotName)
        /// </summary>
        /// <param name="botNumber">The robot to send the parameter to</param>
        /// <param name="parameter">The parameter to update</param>
        /// <param name="value">The new value</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>True on a successful response (OK) from the bot</returns>
        public static bool sendConfigParameter(int botNumber, configParameters parameter, string value, int port = 8080)
        {
            if (parameter != configParameters.robotName)
            { 
                throw new ArgumentException("Must be the robot name for a string value", "parameter");
            }
            return sendDataToRobot(botNumber, ((int)parameter).ToString() + ":" + value.ToString(), port) == "OK";
        }

        /// <summary>
        /// Gets the current configuration settings from a robot in setup mode
        /// </summary>
        /// <param name="botNumber">The robot to get the settings from</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>A comma separated list of values</returns>
        public static string getRobotSettings(int botNumber, int port = 8080)
        {
            return sendDataToRobot(botNumber, "10" + ":", port);
        }

        /// <summary>
        /// Sends data to a robot
        /// </summary>
        /// <param name="botNumber">The robot to send the data to</param>
        /// <param name="data">The data to send</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>The response from the robot or an empty string on failure</returns>
        private static string sendDataToRobot(int botNumber, string data, int port)
        {
            byte[] responseBuffer = new byte[256];
            string response = "";
            Robot bot = gameStatus.robots[botNumber];
            if (gameStatus.botless)
            {
                bot.moving.Set();
                return "OK";
            }
            using (Socket socketConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Byte[] bytesToSend = Encoding.ASCII.GetBytes(data);
                socketConnection.SendTimeout = 1010;
                socketConnection.ReceiveTimeout = 1010;
                try
                {
                    // Connect to bot
                    IAsyncResult result = socketConnection.BeginConnect(bot.robotAddress, port, null, null);
                    if (result.AsyncWaitHandle.WaitOne(500, true) && socketConnection.Connected)
                    {
                        socketConnection.Send(bytesToSend, bytesToSend.Length, 0);

                        // Wait for all data to be available
                        int responseTimeout = 250;
                        if(gameStatus.tuneRobots)
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
            }
            return response.TrimStart(); ; 
        }
    }
}