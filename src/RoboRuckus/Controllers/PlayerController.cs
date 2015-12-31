using Microsoft.AspNet.Mvc;
using Newtonsoft.Json;
using RoboRuckus.RuckusCode;
using System.Linq;

namespace RoboRuckus.Controllers
{
    public class PlayerController : Controller
    {
        /// <summary>
        /// Handles when a player connects. If the player is already
        /// in game, sends them their status, otherwise attempts to add
        /// them to the game.
        /// </summary>
        /// <param name="player">The player number, if they have one</param>
        /// <returns>The view or action context</returns>
        public IActionResult Index(int player = 0)
        {            
            if (gameStatus.gameReady)
            {
                // See if player is already in game
                if (player > gameStatus.numPlayers || player == 0 || player > gameStatus.players.Count)
                {
                    return RedirectToAction("playerSetup", new { player = player });
                }
                // Player is in game, return view
                ViewBag.player = player;
                ViewBag.damage = gameStatus.players[player - 1].playerRobot.damage;
                return View();
            }
            else
            {
                // Game is not set up
                return View("~/Views/Player/settingUp.cshtml");
            }
        }

        /// <summary>
        /// Set's up a player
        /// </summary>
        /// <param name="player">The player number</param>
        /// <returns>The view</returns>
        public IActionResult playerSetup(int player = 0, int reset = 0)
        {
            int playerNumber;
            // Attempt to add player to game
            if (player == 0)
            {
                playerNumber = gameStatus.addPlayer();
            }
            else
            {
                if (gameStatus.players.Any(p => p.playerNumber == (player - 1)))
                {
                    playerNumber = player;
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            // Check if game is full
            if (playerNumber == 0)
            {
                return View("~/Views/Player/Full.cshtml");
            }
            else
            {
                // Success!
                ViewBag.flags = JsonConvert.SerializeObject(gameStatus.gameBoard.flags);
                ViewBag.board_x = gameStatus.boardSizeX;
                ViewBag.board_y = gameStatus.boardSizeY;
                ViewBag.player = playerNumber;
                ViewBag.board = gameStatus.gameBoard.name.Replace(" ", "");
                ViewBag.reset = reset;
                if (reset == 1)
                {
                    ViewBag.botName = gameStatus.players[player - 1].playerRobot.robotName;
                }
                return View();
            }
        }

        /// <summary>
        /// Let's a player setup their parameters
        /// </summary>
        /// <param name="player">The player number</param>
        /// <param name="botName">The chosen robot name</param>
        /// <param name="botX">The player's bot's x position</param>
        /// <param name="botY">The player's bot's y position</param>
        /// <param name="botDir">The player's bot's direction</param>
        /// <returns>The view</returns>
        [HttpPost]
        public IActionResult setupPlayer(int player, string botName, int botX, int botY, int botDir)
        {
            lock (gameStatus.locker)
            {
                // Check if robot was already assigned
                if(!gameStatus.assignBot(player, botName))
                {
                    return RedirectToAction("playerSetup", new { player = player });
                }
                // Check it robot's coordinates are taken
                if (gameStatus.robots.Any(r => (r.x_pos == botX && r.y_pos == botY)))
                {
                    return RedirectToAction("playerSetup", new { player = player });
                }
                else
                {
                    Player sender = gameStatus.players[player - 1];
                    sender.playerRobot.x_pos = botX;
                    sender.playerRobot.y_pos = botY;
                    sender.playerRobot.lastLocation = new int[] { botX, botY };
                    sender.playerRobot.currentDirection = (Robot.orientation)botDir;
                    return RedirectToAction("Index", new { player = player });
                }
            }
        }

        /// <summary>
        /// Gets the status of the player setup
        /// </summary>
        /// <returns>JSON object containing bot positions, orientations, and available robot names</returns>
        [HttpGet]
        public IActionResult Status()
        {
            string result = "{\"robots\": {";
            bool first = true;
            foreach (Robot active in gameStatus.robots)
            {
                if (active.controllingPlayer != null)
                {
                    if (!first)
                    {
                        result += ",";
                    }
                    first = false;
                    result += "\"" + active.controllingPlayer.playerNumber.ToString() + "\": {\"number\": " + active.controllingPlayer.playerNumber.ToString() + ",\"x\": " + active.x_pos.ToString() + ",\"y\": " + active.y_pos.ToString() + ",\"direction\": " + active.currentDirection.ToString("D") + "}";
                }
            }
            result += "}, \"botNames\": " + JsonConvert.SerializeObject(gameStatus.robotPen.Select(r => r.robotName).ToArray()) + "}";
            return Content(result, "application/json");
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}