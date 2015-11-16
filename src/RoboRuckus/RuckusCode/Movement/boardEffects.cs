using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace RoboRuckus.RuckusCode.Movement
{
    public static class boardEffects
    {
        /// <summary>
        /// Execute turn table moves
        /// </summary>
        public static void executeTurnTables()
        {
            lock (gameStatus.locker)
            {
                playerSignals.Instance.showMessage("Turntables Rotating");
                Robot[] bots = gameStatus.robots.Where(r => gameStatus.gameBoard.turntables.Any(t => (t.coord[0] == r.x_pos && t.coord[1] == r.y_pos))).ToArray();
                foreach (Robot _bot in bots)
                {
                    turntable table = gameStatus.gameBoard.turntables.Single(t => (t.coord[0] == _bot.x_pos && t.coord[1] == _bot.y_pos));
                    moveModel movement = new moveModel
                    {
                        card = new Hubs.cardModel
                        {
                            direction = table.dir,
                            cardNumber = 1,
                            priority = 1,
                            magnitude = 1
                        },
                        bot = _bot
                    };
                    moveCalculator.processMoveOrder(moveCalculator.calculateMove(movement)[0]);
                }
            }
        }

        /// <summary>
        /// Fires all lasers
        /// </summary>
        /// <returns>True if a bot was hit</returns>
        public static bool fireLasers()
        {
            lock (gameStatus.locker)
            {
                playerSignals.Instance.showMessage("Firing lasers!", "laser");
                bool botHit = boardEffects.fireBotLasers();
                bool boardHit = boardEffects.fireBoardLasers();
                return botHit || boardHit;
            }
        }

        /// <summary>
        /// Gets an array of all robots currently on a wrench space
        /// </summary>
        /// <returns>An array of robots on a wrench space</returns>
        public static Robot[] wrenches()
        {
            return gameStatus.robots.Where(r => !r.controllingPlayer.dead && (gameStatus.gameBoard.wrenches.Any(w => w[0] == r.x_pos && w[1] == r.y_pos))).ToArray();
        }

        /// <summary>
        /// Determines if a given coordinate contains a bit
        /// </summary>
        /// <param name="coordinate">The [x, y] coordinate to check</param>
        /// <returns>True if the coordinate contains a bit</returns>
        public static bool onPit(int[] coordinate)
        {
            return gameStatus.gameBoard.pits.Any(p => (p[0] == coordinate[0] && p[1] == coordinate[1]));
        }

        /// <summary>
        /// Finds a wall if it exists between two coordinates, inclusive.
        /// </summary>
        /// <param name="fromCord">The coordinate to start searching from</param>
        /// <param name="toCord">The coordinate to stop searching at</param>
        /// <param name="direction">The direction along which to search</param>
        /// <returns>The furthest coordinate along the direction something can move before hitting the wall, or null if no wall was found</returns>
        public static int[] findWall(int[] fromCord, int[] toCord, Robot.orientation direction)
        {
            int[] wall = null;
            switch (direction)
            {
                case Robot.orientation.X:
                    int[][] found = gameStatus.gameBoard.walls.OrderBy(w => w[0][0]).ThenBy(w => w[1][0]).FirstOrDefault(w => ((w[0][0] >= fromCord[0] && w[0][0] <= toCord[0]) && (w[1][0] >= fromCord[0] && w[1][0] <= toCord[0]) && (w[0][1] == fromCord[1] && w[1][1] == fromCord[1])));
                    if (found != null)
                    {
                        wall = new int[] { found[0][0] < found[1][0] ? found[0][0] : found[1][0], found[0][1] };
                    }
                    break;
                case Robot.orientation.Y:
                    found = gameStatus.gameBoard.walls.OrderBy(w => w[0][1]).ThenBy(w => w[1][1]).FirstOrDefault(w => ((w[0][1] >= fromCord[1] && w[0][1] <= toCord[1]) && (w[1][1] >= fromCord[1] && w[1][1] <= toCord[1]) && (w[0][0] == fromCord[0] && w[1][0] == fromCord[0])));
                    if (found != null)
                    {
                        wall = new int[] { found[0][1] < found[1][1] ? found[0][1] : found[1][1], found[0][0] };
                    }
                    break;
                case Robot.orientation.NEG_X:
                    found = gameStatus.gameBoard.walls.OrderByDescending(w => w[0][0]).ThenByDescending(w => w[1][0]).FirstOrDefault(w => ((w[0][0] <= fromCord[0] && w[0][0] >= toCord[0]) && (w[1][0] <= fromCord[0] && w[1][0] >= toCord[0]) && (w[0][1] == fromCord[1] && w[1][1] == fromCord[1])));
                    if (found != null)
                    {
                        wall = new int[] { found[0][0] > found[1][0] ? found[0][0] : found[1][0], found[0][1] };
                    }
                    break;
                case Robot.orientation.NEG_Y:
                    found = gameStatus.gameBoard.walls.OrderByDescending(w => w[0][1]).ThenByDescending(w => w[1][1]).FirstOrDefault(w => ((w[0][1] <= fromCord[1] && w[0][1] >= toCord[1]) && (w[1][1] <= fromCord[1] && w[1][1] >= toCord[1]) && (w[0][0] == fromCord[0] && w[1][0] == fromCord[0])));
                    if (found != null)
                    {
                        wall = new int[] { found[0][1] > found[1][1] ? found[0][1] : found[1][1], found[0][0] };
                    }
                    break;
            }
            return wall;
        }

        /// <summary>
        /// Fires robot lasers and calculates damage
        /// </summary>
        /// <returns>Wheter a robot was hit</returns>
        private static bool fireBotLasers()
        {
            lock (gameStatus.locker)
            {
                bool hit = false;
                Timer watchDog;
                foreach (player shooter in gameStatus.players)
                {
                    if (!shooter.shutdown && !shooter.dead)
                    {
                        Robot bot = shooter.playerRobot;
                        int shot = LoS(new int[] { bot.x_pos, bot.y_pos }, bot.currentDirection, botNumber: bot.robotNum);
                        if (shot != -1)
                        {
                            hit = true;
                            gameStatus.robots[shot].damage++;

                            // Start watch dog to skip bots that don't respond in 5 seconds
                            bool timeout = false;
                            watchDog = new Timer(delegate { Console.WriteLine("Bot didn't acknowledge damage value update"); timeout = true; }, null, 5000, Timeout.Infinite);

                            // Wait for bot to acknowledge receipt of updated value
                            SpinWait.SpinUntil(() => botSignals.sendDamage(shot, gameStatus.robots[shot].damage) == "OK" || timeout);

                            // Dispose the watch dog
                            watchDog.Dispose();
                        }
                    }
                }
                return hit;
            }
        }

        /// <summary>
        /// Fires the board lasers
        /// </summary>
        /// <returns>True if a bot was hit</returns>
        private static bool fireBoardLasers()
        {
            lock (gameStatus.locker)
            {
                bool hit = false;
                Timer watchDog;
                foreach (laser shooter in gameStatus.gameBoard.lasers)
                {
                    int shot = LoS(shooter.start, shooter.facing, shooter.end);
                    if (shot != -1)
                    {
                        hit = true;
                        gameStatus.robots[shot].damage += shooter.strength;

                        // Start watch dog to skip bots that don't respond in 5 seconds
                        bool timeout = false;
                        watchDog = new Timer(delegate { Console.WriteLine("Bot didn't acknowledge damage value update"); timeout = true; }, null, 5000, Timeout.Infinite);

                        // Wait for bot to acknowledge receipt of updated value
                        SpinWait.SpinUntil(() => botSignals.sendDamage(shot, gameStatus.robots[shot].damage) == "OK" || timeout);

                        // Dispose the watch dog
                        watchDog.Dispose();
                    }
                }
                return hit;
            }
        }

        /// <summary>
        /// Finds if a there is a bot in line of sight between two coordinates inclusive. The toCord should be in the direction of the facing.
        /// </summary>
        /// <param name="fromCord">{ X, Y } The coordinate to start looking for a LoS on a bot</param>
        /// <param name="toCord">{ X, Y } Optionally, a coordinate to stop looking (defaults to edge of board)</param>
        /// <param name="botNumber">If a bot is looking for another bot, ensures it doesn't find itself</param>
        /// <returns>The bot number, or -1 for no result</returns>
        private static int LoS(int[] fromCord, Robot.orientation facing, int[] toCord = null, int botNumber = -1)
        {
            Robot bot = null;
            switch (facing)
            {
                case Robot.orientation.X:
                    toCord = toCord == null ? new int[] { gameStatus.boardSizeX, fromCord[1] } : toCord;
                    bot = gameStatus.robots.OrderBy(r => r.x_pos).FirstOrDefault(r => (!r.controllingPlayer.dead && r.robotNum != botNumber && r.x_pos >= fromCord[0] && r.x_pos <= toCord[0] && r.y_pos == fromCord[1]));
                    break;
                case Robot.orientation.Y:
                    toCord = toCord == null ? new int[] { fromCord[0], gameStatus.boardSizeY } : toCord;
                    bot = gameStatus.robots.OrderBy(r => r.y_pos).FirstOrDefault(r => (!r.controllingPlayer.dead && r.robotNum != botNumber && r.y_pos >= fromCord[1] && r.y_pos <= toCord[1] && r.x_pos == fromCord[0]));
                    break;
                case Robot.orientation.NEG_X:
                    toCord = toCord == null ? new int[] { 0, fromCord[1] } : toCord;
                    bot = gameStatus.robots.OrderByDescending(r => r.x_pos).FirstOrDefault(r => (!r.controllingPlayer.dead && r.robotNum != botNumber && r.x_pos <= fromCord[0] && r.x_pos >= toCord[0] && r.y_pos == fromCord[1]));
                    break;
                case Robot.orientation.NEG_Y:
                    toCord = toCord == null ? new int[] { fromCord[0], 0 } : toCord;
                    bot = gameStatus.robots.OrderByDescending(r => r.y_pos).FirstOrDefault(r => (!r.controllingPlayer.dead && r.robotNum != botNumber && r.y_pos <= fromCord[1] && r.y_pos >= toCord[1] && r.x_pos == fromCord[0]));
                    break;
            }

            // Check to see if a robot was found and, if so, if there's an obstacle blocking LoS.
            if (bot == null || boardEffects.findWall(fromCord, new int[] { bot.x_pos, bot.y_pos }, facing) != null)
            {
                // Obstacle is blocking, no bot in LoS
                return -1;
            }
            return bot.robotNum;
        }
    }
}
