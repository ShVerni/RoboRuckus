using System.Threading;
using Microsoft.AspNet.Mvc;
using RoboRuckus.RuckusCode;

namespace RoboRuckus.Controllers
{
    public class BotController : Controller
    {
        private object _locker = new object();

        /// <summary>
        /// A bot calls this action to be added to the game as an available robot
        /// </summary>
        /// <param name="ip">The IP address of the robot</param>
        /// <returns>A plain text string containing the bot's number in this format: "Bot number:#"</returns>
        [HttpGet]
        public IActionResult Index(string ip)
        {
            // Lock used so player assignment is sent after this method exits
            lock (_locker)
            {
                int result = gameStatus.addBot(ip);
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
                // Send bot number to bot
                return Content("Bot number:" + result.ToString() + "\n", "text/plain");
            }
        }

        /// <summary>
        /// A bot calls this action when it's completed a move
        /// </summary>
        /// <param name="bot">The bot number</param>
        /// <returns>A plain text string with an acknowledgement</returns>
        [HttpGet]
        public IActionResult Done(int bot)
        {
            // Signal bot has finished moving.
            gameStatus.robots[bot].moving.Set();
            // Send acknowledgement to bot
            return Content("AK\n", "text/plain");
        } 
        
        /// <summary>
        /// Sends a player assignment to a bot which
        /// has already had a player assigned previously
        /// </summary>
        /// <param name="player">The player assigned</param>
        /// <param name="bot">The bot the player is assigned to</param>
        private void alreadyAssigned(int player, int bot)
        {
            lock(_locker)
            {
                // Wait for the bot server to become ready
                Thread.Sleep(500);
                SpinWait.SpinUntil(() => botSignals.sendPlayerAssignment(bot, player), 1000);
            }
        }       
    }
}