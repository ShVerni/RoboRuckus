﻿using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RoboRuckus.RuckusCode;
using System.Collections.Generic;
using System.Linq;
using RoboRuckus.Models;
using Newtonsoft.Json;
using System.Threading;

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
            else if (gameStatus.tuneRobots)
            {
                return RedirectToAction("Tuning");
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
        /// <returns>Redirects to the appropriate action</returns>
        [HttpPost]
        public IActionResult setupGame(string selBoard, string flags, int numberOfPlayers = 0)
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
        /// Starts the game and has the first hand of cards dealt to players.
        /// </summary>
        /// <param name="status">Status code for start command (not yet used)</param>
        /// <returns>The string "Done"</returns>
        [HttpGet]
        public IActionResult startGame(int status)
        {
            gameStatus.gameStarted = true;
            serviceHelpers.signals.dealPlayers();
            return Content("Done", "text/plain");
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
                ViewBag.timer = gameStatus.playerTimer;
                ViewBag.started = gameStatus.gameStarted;
                return View();
            }
            if (gameStatus.tuneRobots)
            {
                return RedirectToAction("Tuning");
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
        /// <returns>The string "OK"</returns>
        [HttpPost]
        public IActionResult enterPlayers(string players)
        {
            lock(gameStatus.locker)
            {
                lock (gameStatus.setupLocker)
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
                    serviceHelpers.signals.dealPlayers();
                    gameStatus.playersNeedEntering = false;
                }
                return Content("OK", "text/plain");
            }
        }

        /// <summary>
        /// Allows the game master to manage players
        /// </summary>
        /// <param name="player">The player number to manage</param>
        /// <returns>The view</returns>
        public IActionResult Manage(int player = 0)
        {
            lock (gameStatus.locker)
            {

                lock (gameStatus.setupLocker)
                {
                    if (player != 0 && player <= gameStatus.players.Count())
                    {
                        Player caller = gameStatus.players[player - 1];

                        ViewBag.board_x = gameStatus.boardSizeX;
                        ViewBag.board_y = gameStatus.boardSizeY;
                        ViewBag.player = player;
                        ViewBag.dir = (int)caller.playerRobot.currentDirection;
                        ViewBag.bots = JsonConvert.SerializeObject(gameStatus.robotPen.Select(r => r.robotName).ToArray()).Replace("\"", "&quot;");
                        return View(new manageViewModel(caller));
                    }
                    return Content("Error", "text/plain");
                }
            }
        }
        /// <summary>
        /// Updates the status of a player
        /// </summary>
        /// <param name="lives">Ther number of lives the player has</param>
        /// <param name="damage">The damage the robot has</param>
        /// <param name="botX">The robot's x position</param>
        /// <param name="botY">The robot's y position</param>
        /// <param name="botDir">The robots orientation</param>
        /// <param name="botName">The robot the player is using</param>
        /// <param name="flags">The flags they've touched</param>
        /// <param name="player">The player being updated</param>
        /// <returns>The view</returns>
        public IActionResult updatePlayer(int lives, sbyte damage, int botX, int botY, int botDir, string botName, int flags, int player = 0)
        {
            lock (gameStatus.locker)
            {
                if (player != 0 && player <= gameStatus.players.Count())
                {
                    lock (gameStatus.setupLocker)
                    {
                        Player caller = gameStatus.players[player - 1];
                        Robot bot = caller.playerRobot;
                        caller.lives = lives;
                        if (botName != "" && bot.robotName != botName && gameStatus.robotPen.Exists(r => r.robotName == botName))
                        {
                            // Get new bot
                            Robot newBot = gameStatus.robotPen.FirstOrDefault(r => r.robotName == botName);
                            gameStatus.robotPen.Remove(newBot);

                            // Clear old bot
                            bot.y_pos = -1;
                            bot.x_pos = -1;
                            bot.damage = 0;
                            bot.flags = 0;
                            bot.controllingPlayer = null;
                           
                            // Wait for bot to acknowledge receipt of order
                            botSignals.sendReset(bot.robotNum);

                            // Setup new bot
                            newBot.robotNum = bot.robotNum;
                            newBot.lastLocation = bot.lastLocation;
                            gameStatus.robots[newBot.robotNum] = newBot;
                            gameStatus.robotPen.Add(bot);                            

                            bot = newBot;
                            bot.controllingPlayer = caller;
                            caller.playerRobot = bot;
                            SpinWait.SpinUntil(() => botSignals.sendPlayerAssignment(bot.robotNum, player));
                        }
                        // Assign updates
                        bot.damage = damage;
                        bot.x_pos = botX;
                        bot.y_pos = botY;
                        bot.currentDirection = (Robot.orientation)botDir;
                        bot.flags = flags;
                    }
                    serviceHelpers.signals.updateHealth();
                    return View();
                }
            }
            return Content("<h1>Error</h1>", "text/HTML");
        }

        /// <summary>
        /// Used to tune the robot settings
        /// </summary>
        /// <returns>The view</returns>
        public IActionResult Tuning()
        {
            lock (gameStatus.locker)
            {
                lock (gameStatus.setupLocker)
                {
                    // Check if physical bots are being used.
                    if (gameStatus.botless)
                    {
                        return RedirectToAction("Index");                        
                    }
                    // Check if game is started
                    if (!gameStatus.gameStarted)
                    {
                        // Check if already tuning robots
                        if (!gameStatus.tuneRobots)
                        {
                            // Collect all the robots
                            foreach (Robot r in gameStatus.robots)
                            {
                                r.y_pos = -1;
                                r.x_pos = -1;
                                r.damage = 0;
                                r.flags = 0;
                                gameStatus.robotPen.Add(r);

                            }
                            gameStatus.robots.Clear();
                            byte i = 0;
                            // Assign a number to each bot
                            foreach (Robot bot in gameStatus.robotPen)
                            {
                                bot.robotNum = i;
                                // Add dummy player to bot
                                bot.controllingPlayer = new Player(i);
                                i++;
                                gameStatus.robots.Add(bot);
                            }
                            gameStatus.robotPen.Clear();
                            gameStatus.tuneRobots = true;
                        }
                        // Build the JSON string of all the robots
                        string bots = "[";
                        bool first = true;
                        foreach (Robot bot in gameStatus.robots)
                        {
                            if (!first)
                            {
                                bots += ",";
                            }
                            else
                            {
                                first = false;
                            }
                            bots += "{\"number\":" + bot.robotNum + ", \"name\":" + "\"" + bot.robotName + "\"}";
                        }
                        bots += "]";
                        ViewBag.Robots = bots;
                        return View();
                    }
                    else
                    {
                        return RedirectToAction("Monitor");
                    }
                }
            }
        }


        /// <summary>
        /// Sends a configuration instruction to a robot in setup mode
        /// </summary>
        /// <param name="choice">The action/parameter to set</param>
        /// <param name="value">The value to set</param>
        /// <returns>Thre response from the robot</returns>
        [HttpGet]
        public IActionResult botConfig(int bot, int choice, string value)
        {
            string result = "ER";

            // Put robot in setup mode
            if (choice == -1)
            {
                int i = -1;
                bool success = false;
                Console.WriteLine("Entering setup mode");
                do
                {
                    success = botSignals.enterSetupMode(bot);
                    Thread.Sleep(50);
                    i++;
                } while (!success && i < 5);
                if (success)
                {
                    result = "OK";
                }
            }
            // Get robot's status
            else if (choice == 10)
            {
                Console.WriteLine("Getting status");
                int i = -1;
                do
                {
                    result = botSignals.getRobotSettings(bot);
                    Thread.Sleep(50);
                    i++;
                } while (result == "ER" && i < 5);
                Console.WriteLine(result);
            }
            // Run a speed test
            else if (choice == 11)
            {
                string[] values = value.Split(',');
                int j = 0;
                int i = -1;
                bool success = false;
                foreach (string param in values)
                {
                    i = -1;
                    success = false;
                    do
                    {
                        success = botSignals.sendConfigParameter(bot, (botSignals.configParameters)j, int.Parse(param));
                        Thread.Sleep(50);
                        i++;
                    } while (!success && i < 5);                    
                    j++;
                }
                i = -1;
                success = false;
                do
                {
                    success = botSignals.sendConfigParameter(bot, botSignals.configParameters.speedTest, 0);
                    Thread.Sleep(50);
                    i++;
                } while (!success && i < 5);
                if (success)
                {
                    result = "OK";
                }
            }
            // Run a navigation test
            else if (choice == 12)
            {
                string[] values = value.Split(',');
                int j = 4;
                int i = -1;
                bool success = false;
                foreach (string param in values)
                {
                    i = -1;
                    success = false;
                    do
                    {
                        if (j == 4 || j == 7 || j == 8)
                        {
                            success = botSignals.sendConfigParameter(bot, (botSignals.configParameters)j, float.Parse(param));
                        }
                        else
                        {
                            success = botSignals.sendConfigParameter(bot, (botSignals.configParameters)j, int.Parse(param));
                        }
                        Thread.Sleep(50);
                        i++;
                    } while (!success && i < 5);
                    j++;
                }
                i = -1;
                success = false;
                do
                {
                    success = botSignals.sendConfigParameter(bot, botSignals.configParameters.navTest, 0);
                    Thread.Sleep(50);
                    i++;
                } while (!success && i < 5);
                if (success)
                {
                    result = "OK";
                }
            }
            // Quit
            else if (choice == 13)
            {
                string[] values = value.Split(',');
                int j = 0;
                int i = -1;
                bool success = false;
                foreach (string param in values)
                {
                    i = -1;
                    success = false;
                    do
                    {
                        if (j == 4 || j == 7 || j == 8)
                        {
                            success = botSignals.sendConfigParameter(bot, (botSignals.configParameters)j, float.Parse(param));
                        }
                        else if (j == 9)
                        {
                            success = botSignals.sendConfigParameter(bot, (botSignals.configParameters)j, param);
                        }
                        else
                        {
                            success = botSignals.sendConfigParameter(bot, (botSignals.configParameters)j, int.Parse(param));
                        }
                        Thread.Sleep(50);
                        i++;
                    } while (!success && i < 5);
                    j++;
                }
                i = -1;
                success = false;
                do
                {
                    success = botSignals.sendConfigParameter(bot, botSignals.configParameters.quit, 0);
                    Thread.Sleep(50);
                    i++;
                } while (!success && i < 5);
                if (success)
                {
                    result = "OK";
                }
            }
            return Content(result, "text/plain");
        }


        /// <summary>
        /// Exits the bot tuning mode
        /// </summary>
        /// <returns>A redirect to the setup index page</returns>
        public IActionResult finishBotConfig()
        {
            lock(gameStatus.locker)
            {
                lock(gameStatus.setupLocker)
                {
                    if (gameStatus.tuneRobots)
                    {
                        foreach (Robot r in gameStatus.robots)
                        {
                            r.y_pos = -1;
                            r.x_pos = -1;
                            r.damage = 0;
                            r.flags = 0;
                            r.controllingPlayer = null;
                            gameStatus.robotPen.Add(r);
                        }
                        gameStatus.robots.Clear();
                        gameStatus.tuneRobots = false;
                    }
                }
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Gets the status of the game
        /// </summary>
        /// <returns>JSON object containing bot positions, orientations, damage, and flags. Also indicates if bots are re-entering the game and their last checkpoint locations</returns>
        [HttpGet]
        public IActionResult Status()
        {
            lock (gameStatus.setupLocker)
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

                        result += "\"" + active.controllingPlayer.playerNumber.ToString() + "\": {\"number\": " + active.controllingPlayer.playerNumber.ToString() + ", \"lives\":" + active.controllingPlayer.lives.ToString() + ", \"x\": " + active.x_pos.ToString() + ", \"y\": " + active.y_pos.ToString() + ", \"direction\": " + active.currentDirection.ToString("D") + ", \"damage\": " + active.damage.ToString() + ", \"flags\": " + active.flags.ToString() + ", \"totalFlags\": " + gameStatus.gameBoard.flags.Length.ToString() + ", \"reenter\": " + reenter + ", \"last_x\": " + active.lastLocation[0].ToString() + ", \"last_y\": " + active.lastLocation[1] + ", \"name\": \"" + active.robotName + "\"}";
                    }
                }
                result += "}, \"entering\": " + entering + "}";
                return Content(result, "application/json");
            }
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
            serviceHelpers.signals.resetGame(resetAll);
            return Content("Done", "text/plain");
        }

        /// <summary>
        /// Toggles the timer state
        /// </summary>
        /// <param name="timerEnable"></param>
        /// <returns>The string "OK"</returns>
        [HttpGet]
        public IActionResult Timer(bool timerEnable)
        {
            gameStatus.playerTimer = timerEnable;
            return Content("OK", "text/plain");
        }
    }
}
