using Microsoft.AspNet.Mvc;
using RoboRuckus.RuckusCode;

namespace RoboRuckus.Controllers
{
    public class SetupController : Controller
    {
        /// <summary>
        /// Allows a user to set up and initialize the game
        /// </summary>
        /// <returns>The setup index view</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Sets up the game
        /// </summary>
        /// <param name="numberOfPlayers">The number of players in the game</param>
        /// <returns>The startGame view</returns>
        [HttpPost]
        public IActionResult startGame(int numberOfPlayers = 0)
        {
            if (numberOfPlayers > 0)
            {
                gameStatus.numPlayers = numberOfPlayers;
                gameStatus.gameReady = true;
            }
            return RedirectToAction("Monitor");
        }

        /// <summary>
        /// Monitors the game board status
        /// </summary>
        /// <returns>The view</returns>
        [HttpGet]
        public IActionResult Monitor()
        {
            ViewBag.players = gameStatus.numPlayers;
            ViewBag.board_x = gameStatus.boardSizeX;
            ViewBag.board_y = gameStatus.boardSizeY;
            return View();
        }

        /// <summary>
        /// Gets the status of the game
        /// </summary>
        /// <returns>JSON object containing bot positions and orientations</returns>
        [HttpGet]
        public IActionResult Status()
        {
            string result = "[";
            bool first = true;
            foreach (player active in gameStatus.players)
            {
                if (first)
                {
                    result += "{\"number\": " + active.playerNumber.ToString() + ",\"x\": " + active.playerRobot.x_pos + ",\"y\": " + active.playerRobot.y_pos + ",\"direction\": " + active.playerRobot.currentDirection.ToString("D") + ",\"damage\": " + active.playerRobot.damage + "}";
                    first = false;
                }
                else
                {
                    result += ",{\"number\": " + active.playerNumber.ToString() + ",\"x\": " + active.playerRobot.x_pos + ",\"y\": " + active.playerRobot.y_pos + ",\"direction\": " + active.playerRobot.currentDirection.ToString("D") + ",\"damage\": " + active.playerRobot.damage + "}";
                }
            }
            result += "]";
            return Content(result);
        }

        /// <summary>
        /// Resets the game to the initial state
        /// </summary>
        /// <returns>The string "Done"</returns>
        [HttpGet]
        public IActionResult Reset()
        {
            playerSignals.Instance.resetGame();
            return Content("Done");
        }
    }
}
