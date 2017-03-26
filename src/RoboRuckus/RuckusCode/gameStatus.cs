using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json;

namespace RoboRuckus.RuckusCode
{
    public static class gameStatus
    {
        // Use launch switch "botless" to play game without physical bots
        public static bool noBots
        {
            get { return _noBots; }
            set
            {
                _noBots = value;
                if (value)
                {
                    addBot("0.0.0.0", "Botimus Prime");
                    addBot("0.0.0.1", "Protobot");
                    addBot("0.0.0.2", "Twirly Bot");
                    addBot("0.0.0.3", "Bot Waaay");
                    addBot("0.0.0.4", "Thunderbot");
                    addBot("0.0.0.5", "Fredbot");
                }
            }
        }
        private static bool _noBots = false;

        // Game state varaibles
        public static int numPlayers = 0;
        public static int numPlayersInGame = 0;
        public static bool gameReady = false;
        public static List<Robot> robotPen = new List<Robot>();
        public static List<Robot> robots = new List<Robot>();
        public static List<Player> players = new List<Player>();
        public static List<byte> deltCards = new List<byte>();
        public static List<byte> lockedCards = new List<byte>();
        public static string[] movementCards;
        public static Board gameBoard;
        public static List<Board> boards = new List<Board>();
        public static bool winner = false;
        public static bool playersNeedEntering = false;
        public static bool playerTimer = false;

        // Zero ordered board size
        public static int boardSizeX;
        public static int boardSizeY;

        // Static objects for global locks and thread control
        public static readonly object locker = new object();
        public static readonly object setupLocker = new object();

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
            string path = serviceHelpers.rootPath + _boardPath;
            foreach (string boardFile in Directory.GetFiles(path))
            {
                using (StreamReader file = File.OpenText(boardFile))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    boards.Add((Board)serializer.Deserialize(file, typeof(Board)));
                }
            }
            // Load the movement cards
            path = serviceHelpers.rootPath + _cardPath;
            using (StreamReader sr = new StreamReader(path))
            {
                string cards = sr.ReadToEnd();
                movementCards = Array.ConvertAll(cards.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries), p => p.Trim());
            }
        }

        /// <summary>
        /// Assigns a robot to a player
        /// </summary>
        /// <param name="player">The player being assigned to</param>
        /// <param name="robotName">The robot name to assign</param>
        /// <returns></returns>
        public static bool assignBot(int player, string robotName)
        {
            Player sender = players[player - 1];
            if (sender.playerRobot != null)
            {
                return true;
            }
            Robot bot = robotPen.FirstOrDefault(r => r.robotName == robotName);
            if (bot != null)
            {
                // Removes the bot from the pen and puts it in play
                robots.Add(bot);
                robotPen.Remove(bot);
                bot.robotNum = (byte)(robots.Count - 1);
                // Assign player to bot
                bot.controllingPlayer = sender;
                sender.playerRobot = bot;
                if (!_noBots)
                {
                    SpinWait.SpinUntil(() => botSignals.sendPlayerAssignment(bot.robotNum, sender.playerNumber + 1));
                }         
                return true;
            }
            return false;
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
                // Makes sure there are enough player slots and robots for another player to be added
                if (numPlayersInGame < numPlayers && robotPen.Count > 0)
                {
                    // Create new player with their chosen bot                    
                    Player newPlayer = new Player((byte)numPlayersInGame);
                    players.Add(newPlayer);

                    numPlayersInGame++;
                    return numPlayersInGame;
                }
                return 0;
            }
        }

        /// <summary>
        /// Adds a robot to the list of available robots
        /// </summary>
        /// <param name="botIP">The IP address of the robot</param>
        /// <returns>The bot number</returns>
        public static int addBot(string botIP, string name)
        {
            // Only one bot can be added at a time
            lock(robots)
            {
                // Check if robot is already in game
                IPAddress botAddress = IPAddress.Parse(botIP);
                Robot bot = robots.FirstOrDefault(r => r.robotName == name);
                if (bot != null)
                {
                   bot.robotAddress = botAddress;
                   return bot.robotNum | 0x10000 | (bot.controllingPlayer.playerNumber << 8);
                }
                bot = robotPen.FirstOrDefault(r => r.robotName == name);
                if (bot == null)
                {
                    // Add new robot to game
                    robotPen.Add(new Robot { robotAddress = botAddress, robotName = name });
                    return -1;
                }
                else
                {
                    bot.robotAddress = botAddress;
                    return bot.robotNum;
                }
            }
        }
    }
}