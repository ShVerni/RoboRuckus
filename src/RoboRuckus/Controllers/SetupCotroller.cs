using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using RoboRuckus.RuckusCode;
using System.Collections.Generic;
using System.Linq;
using RoboRuckus.Models;
using Newtonsoft.Json;

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
            if (gameStatus.gameReady)
            {
                return RedirectToAction("Monitor");
            }
            // Get a list of currently loaded boards and add it to the model
            IEnumerable<SelectListItem> _boards = gameStatus.boards.OrderBy(b => b.name).Select(b => new SelectListItem
            {
                Text = b.name,
                Value = b.name
            });
            setupViewModel _model = new setupViewModel { boards = _boards };
            // Send the board sizes to the view as a JSON object
            string _sizes = "{";
            bool first = true;
            foreach (Board gameBorad in gameStatus.boards)
            {
                if (!first)
                {
                    _sizes += ",";
                }
                first = false;
                _sizes += "\"" + gameBorad.name + "\": [" + gameBorad.size[0].ToString() + ", " + gameBorad.size[1].ToString() + "]";
            }
            _sizes += "}";
            ViewBag.sizes = _sizes;
            return View(_model);
        }

        /// <summary>
        /// Sets up a game
        /// </summary>
        /// <param name="selBoard">The board selected to play on</param>
        /// <param name="flags">The position of placed flags</param>
        /// <param name="numberOfPlayers">The number of players</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult startGame(string selBoard, string flags, int numberOfPlayers = 0)
        {
            if (numberOfPlayers > 0)
            {
                gameStatus.numPlayers = numberOfPlayers;
                Board _board = gameStatus.boards.FirstOrDefault(b => b.name == selBoard);
                if (_board != null)
                {
                    gameStatus.gameBoard = _board;
                    if (flags != null && flags.Length > 1)
                    {                     
                        // Assign the flag coordinates, ordered according to the flag number
                        gameStatus.gameBoard.flags = JsonConvert.DeserializeObject<int[][]>(flags).OrderBy(f => f[0]).Select(f => new int[] { f[1], f[2] }).ToArray();
                    }
                    else
                    {
                        gameStatus.gameBoard.flags = new int[0][];
                    }
                    gameStatus.boardSizeX = _board.size[0];
                    gameStatus.boardSizeY = _board.size[1];
                    gameStatus.gameReady = true;
                }
                return RedirectToAction("Monitor");
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Monitors the game board status
        /// </summary>
        /// <returns>The view</returns>
        [HttpGet]
        public IActionResult Monitor()
        {
            if (gameStatus.gameBoard != null)
            {
                ViewBag.flags = JsonConvert.SerializeObject(gameStatus.gameBoard.flags);
                ViewBag.players = gameStatus.numPlayers;
                ViewBag.board_x = gameStatus.boardSizeX;
                ViewBag.board_y = gameStatus.boardSizeY;
                ViewBag.board = gameStatus.gameBoard.name.Replace(" ", "");
                return View();
            }
            else
            {
                return RedirectToAction("Index");
            }

        }

        /// <summary>
        /// Re-enters dead players in the game the game
        /// </summary>
        /// <param name="players">[[player number, bot X position, bot Y position, bot orientation],etc...]</param>
        /// <returns>An OK</returns>
        [HttpPost]
        public IActionResult enterPlayers(string players)
        {
            lock(gameStatus.locker)
            {
                int[][] entering = JsonConvert.DeserializeObject<int[][]>(players);
                foreach (int[] enter in entering)
                {
                    Player sender = gameStatus.players[enter[0]];
                    Robot bot = sender.playerRobot;
                    bot.x_pos = enter[1];
                    bot.y_pos = enter[2];
                    bot.damage = 0;
                    bot.currentDirection = (Robot.orientation)Enum.Parse(typeof(Robot.orientation), enter[3].ToString());
                    sender.dead = false;
                }
                playerSignals.Instance.dealPlayers();
                gameStatus.playersNeedEntering = false;
            }
            return Content("OK", "text/plain");
        }

        /// <summary>
        /// Gets the status of the game
        /// </summary>
        /// <returns>JSON object containing bot positions, orientations, damage, and flags. Also indicates if bots are re-entering the game and their last checkpoint locations</returns>
        [HttpGet]
        public IActionResult Status()
        {
            string result = "{\"players\": {";
            bool first = true;
            string entering = gameStatus.playersNeedEntering ? "1" : "0";
            Robot[] sorted = gameStatus.robots.OrderBy(r => r.controllingPlayer.playerNumber).ToArray();
            foreach (Robot active in sorted)
            {
                if (active.controllingPlayer != null)
                {
                    if (!first)
                    {
                        result += ",";
                    }
                    first = false;
                    string reenter = "0";
                    if (active.controllingPlayer.dead && active.controllingPlayer.lives > 0)
                    {
                        reenter = "1";
                    }
                    result += "\"" + active.controllingPlayer.playerNumber.ToString() + "\": {\"number\": " + active.controllingPlayer.playerNumber.ToString() + ",\"lives\":" + active.controllingPlayer.lives.ToString() + ",\"x\": " + active.x_pos.ToString() + ",\"y\": " + active.y_pos.ToString() + ",\"direction\": " + active.currentDirection.ToString("D") + ",\"damage\": " + active.damage.ToString() + ",\"flags\": " + active.flags.ToString() + ",\"totalFlags\": " + gameStatus.gameBoard.flags.Length.ToString() + ", \"reenter\": " + reenter + ", \"last_x\": " + active.lastLocation[0].ToString() + ", \"last_y\": " + active.lastLocation[1] + "}";
                }
            }
            result += "}, \"entering\": " + entering + "}";
            return Content(result, "application/json");
        }

        /// <summary>
        /// Resets the game to the initial state
        /// </summary>
        /// <param name="resetAll">
        /// 0 to reset current game with same players
        /// 1 to reset entire game to startup state (with same bots)
        /// </param>
        /// <returns>The string "Done"</returns>
        [HttpGet]
        public IActionResult Reset(int resetAll = 0)
        {
            playerSignals.Instance.resetGame(resetAll);
            return Content("Done", "text/plain");
        }
    }
}
