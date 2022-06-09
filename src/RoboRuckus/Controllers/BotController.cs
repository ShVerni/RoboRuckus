using Microsoft.AspNetCore.Mvc;
using System.Net;
using RoboRuckus.RuckusCode;
using System.Threading;
using System.Linq;

namespace RoboRuckus.Controllers
{
    public class BotController : Controller
    {
        /// <summary>
        /// A bot calls this action to be added to the game as an available robot
        /// </summary>
        /// <param name="ip">The IP address of the robot</param>
        /// <param name="name">The name of the robot</param>
        /// <param name="lateralMovement">If the robot supports lateral (side-to-side) movement</param>
        /// <returns>An AK acknowledging the accepted robot</returns>
        [HttpGet]
        public IActionResult Index(string ip, string name, bool lateralMovement = false)
        {
            IPAddress botIP = IPAddress.Parse(ip);
            botSignals.addBot(botIP, name, lateralMovement);
            // Server can sometimes respond faster than a robot is ready
            Thread.Sleep(150);
            // Send acknowledgment to bot
            return Content("AK\n", "text/plain");
        }

        /// <summary>
        /// A bot calls this action when it's completed a move
        /// </summary>
        /// <param name="bot">The bot number</param>
        /// <returns>A plain text string with an acknowledgment</returns>
        [HttpGet]
        public IActionResult Done(int bot)
        {
            botSignals.Done(bot);
            // Server can sometimes respond faster than a robot is ready
            Thread.Sleep(150);
            // Send acknowledgment to bot
            return Content("AK\n", "text/plain");
        } 
        
    }
}