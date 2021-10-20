using Microsoft.AspNetCore.Mvc;
using System.Net;
using RoboRuckus.RuckusCode;
using System.Threading;

namespace RoboRuckus.Controllers
{
    public class BotController : Controller
    {
        /// <summary>
        /// A bot calls this action to be added to the game as an available robot
        /// </summary>
        /// <param name="ip">The IP address of the robot</param>
        /// <returns>An AK acknowledging the accpeted robot</returns>
        [HttpGet]
        public IActionResult Index(string ip, string name)
        {
            IPAddress botIP = IPAddress.Parse(ip);
            botSignals.addBot(botIP, name);
            // Server can sometimes respond faster than a robot is ready
            Thread.Sleep(150);
            // Send acknowledgement to bot
            return Content("AK\n", "text/plain");
        }

        /// <summary>
        /// A bot calls this action when it's completed a move
        /// </summary>
        /// <param name="bot">The bot number</param>
        /// <returns>A plain text string with an acknowledgement</returns>
        [HttpGet]
        public IActionResult Done(int bot)
        {
            botSignals.Done(bot);
            // Server can sometimes respond faster than a robot is ready
            Thread.Sleep(150);
            // Send acknowledgement to bot
            return Content("AK\n", "text/plain");
        } 
        
    }
}