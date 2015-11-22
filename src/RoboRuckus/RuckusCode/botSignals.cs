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
        /// Sends a movement command to a robot
        /// </summary>
        /// <param name="order">The order to send</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>The response from the bot</returns>
        public static string sendMoveCommand(orderModel order, int port = 8080)
        {
            return sendDataToRobot(order.botNumber, order.ToString());
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
            return sendDataToRobot(botNumber, playerNumber.ToString()) == "OK";
        }

        /// <summary>
        /// Sends data to a robot, expects a 2-byte response
        /// </summary>
        /// <param name="botNumber">The robot to send the data to</param>
        /// <param name="data">The data to send</param>
        /// <param name="port">The port the robot is listening on</param>
        /// <returns>The response from the robot or an empty string on failure</returns>
        private static string sendDataToRobot(int botNumber, string data, int port = 8080)
        {
            byte[] response = new byte[2];
            Robot bot = gameStatus.robots[botNumber];
            using (Socket socketConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Byte[] bytesToSend = Encoding.ASCII.GetBytes(data);
                try
                {
                    // Connect to bot
                    socketConnection.Connect(bot.robotAddress, port);
                    socketConnection.Send(bytesToSend, bytesToSend.Length, 0);

                    // Wait up to 1000 ms for all data to be available
                    if (SpinWait.SpinUntil(() => socketConnection.Available > 1, 1000))
                    {
                        Int32 bytesRead = socketConnection.Receive(response);
                    }
                    else
                    {
                        // Connection timed out
                        Console.WriteLine("Timed out");
                        Thread.Sleep(300);
                        return "";
                    }
                }
                catch (Exception e)
                {
                    // Socket exception, could not connect to bot
                    Console.WriteLine("{0} Exception caught.", e);
                    return "";
                }
            }
            return new String(Encoding.ASCII.GetChars(response));
        }
    }
}