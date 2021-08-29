using Microsoft.AspNetCore.Mvc;
using RoboRuckus.RuckusCode;

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
            botSignals.addBot(ip, name);
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
            // Send acknowledgement to bot
            return Content("AK\n", "text/plain");
        } 
        
    }
}