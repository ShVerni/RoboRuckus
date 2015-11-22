using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;

namespace RoboRuckus.RuckusCode
{
    public static class gameStatus
    {
        // Game state varaibles
        public static int numPlayers = 0;
        private static int numPlayersInGame = 0;
        public static bool gameReady = false;
        public static List<Robot> robots = new List<Robot>();
        public static List<player> players = new List<player>();
        public static List<byte> deltCards = new List<byte>();
        public static List<byte> lockedCards = new List<byte>();
        public static string[] movementCards;
        public static Board gameBoard;
        public static List<Board> boards = new List<Board>();
        public static bool winner = false;

        // Zero ordered board size
        public static int boardSizeX;
        public static int boardSizeY;

        // Static object for global locks and thread control
        public static readonly object locker = new object();

        // Path to configuration files
        private static readonly string _cardPath = System.IO.Path.DirectorySeparatorChar + "GameConfig" + System.IO.Path.DirectorySeparatorChar + "movementCards.txt";

        // Path to the game board file
        private static readonly string _boardPath = System.IO.Path.DirectorySeparatorChar + "GameConfig" + System.IO.Path.DirectorySeparatorChar + "Boards";

        /// <summary>
        /// Sets up some global settings and the game enviroment
        /// </summary>
        static gameStatus()
        {
            // Load the game boards
            string path = serviceHelpers.appEnvironment.ApplicationBasePath + _boardPath;
            foreach (string boardFile in Directory.GetFiles(path))
            {
                using (StreamReader file = File.OpenText(boardFile))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    boards.Add((Board)serializer.Deserialize(file, typeof(Board)));
                }
            }
            // Load the movement cards
            path = serviceHelpers.appEnvironment.ApplicationBasePath + _cardPath;
            using (StreamReader sr = new StreamReader(path))
            {
                string cards = sr.ReadToEnd();
                movementCards = Array.ConvertAll(cards.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries), p => p.Trim());
            }
        }

        /// <summary>
        /// Adds a player to the game if possible
        /// </summary>
        /// <returns>Returns the player number if successful, 0 otherwise</returns>
        public static int addPlayer()
        {
            // Only one player can be added at a time, and a player cannot be added during a turn
            lock(locker)
            {
                if (numPlayersInGame < numPlayers && numPlayersInGame < robots.Count)
                {
                    // Create new player with their assigned bot
                    // TODO: Allow players to choose their bot
                    player newPlayer = new player((byte)numPlayersInGame, robots[numPlayersInGame]);
                    players.Add(newPlayer);

                    // Assign player to bot
                    robots[numPlayersInGame].controllingPlayer = newPlayer;
                    SpinWait.SpinUntil(() => botSignals.sendPlayerAssignment(numPlayersInGame, numPlayersInGame + 1));

                    numPlayersInGame++;
                    return numPlayersInGame;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Adds a robot to the list of available robots
        /// </summary>
        /// <param name="botIP">The IP address of the robot</param>
        /// <returns>The bot number</returns>
        public static int addBot(string botIP)
        {
            // Only one bot can be added at a time
            lock(robots)
            {
                // Check if robot is already in game
                int botNum;
                IPAddress botAddress = IPAddress.Parse(botIP);
                Robot bot = robots.FirstOrDefault(r => r.robotAddress.Equals(botAddress));
                if (bot == null)
                {
                    // Add new robot to game
                    botNum = robots.Count;
                    robots.Add(new Robot { robotNum = (byte)botNum, robotAddress = botAddress });
                    // TODO: Make robot location setable at startup
                    robots[botNum].x_pos = -1;
                    robots[botNum].y_pos = -1;      
                }
                else
                {
                    // Bot is already in game, return bot's info
                    botNum = bot.robotNum;
                    if (bot.controllingPlayer != null)
                    {
                        botNum = botNum | 0x10000 | (bot.controllingPlayer.playerNumber << 8);
                    }
                }
                return botNum;
            }
        }
    }
}