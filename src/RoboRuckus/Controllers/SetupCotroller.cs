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
                    // Assign the flag coordinates ordered according to the flag number
                    gameStatus.gameBoard.flags = JsonConvert.DeserializeObject<int[][]>(flags).OrderBy(f => f[0]).Select(f => new int[] { f[1], f[2] }).ToArray();
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
        /// Gets the status of the game
        /// </summary>
        /// <returns>JSON object containing bot positions, orientations, damage, and flags</returns>
        [HttpGet]
        public IActionResult Status()
        {
            string result = "[";
            bool first = true;
            foreach (player active in gameStatus.players)
            {
                if (!first)
                {
                    result += ",";                   
                }
                first = false;
                result += "{\"number\": " + active.playerNumber.ToString() + ",\"x\": " + active.playerRobot.x_pos.ToString() + ",\"y\": " + active.playerRobot.y_pos.ToString() + ",\"direction\": " + active.playerRobot.currentDirection.ToString("D") + ",\"damage\": " + active.playerRobot.damage.ToString() + ",\"flags\": " + active.playerRobot.flags.ToString() + ",\"totalFlags\": " + gameStatus.gameBoard.flags.Length.ToString() + "}";
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
