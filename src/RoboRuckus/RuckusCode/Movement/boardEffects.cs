using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace RoboRuckus.RuckusCode.Movement
{
    /// <summary>
    /// Controls all board effects.
    /// Wrapping public methods in lock statements is probably overkill, but doesn't hurt, and might help.
    /// </summary>
    public static class boardEffects
    {
        /// <summary>
        /// Execute turntable moves
        /// </summary>
        public static void executeTurnTables()
        {
            lock (gameStatus.locker)
            {
                serviceHelpers.signals.showMessage("Turntables Rotating");
                // Find all bots on turntables
                Robot[] bots = gameStatus.robots.Where(r => gameStatus.gameBoard.turntables.Any(t => (t.location[0] == r.x_pos && t.location[1] == r.y_pos))).ToArray();
                foreach (Robot _bot in bots)
                {
                    // Determine in which direction to rotate that bot, and craft the movement
                    turntable table = gameStatus.gameBoard.turntables.Single(t => (t.location[0] == _bot.x_pos && t.location[1] == _bot.y_pos));
                    moveModel movement = new moveModel
                    {
                        card = new cardModel
                        {
                            direction = table.dir,
                            cardNumber = 1,
                            priority = 1,
                            magnitude = 1
                        },
                        bot = _bot
                    };
                    // Rotate the robot
                    moveCalculator.processMoveOrder(moveCalculator.calculateMove(movement)[0]);
                }
            }
        }

        /// <summary>
        /// Fires all lasers and adds the damage to robots
        /// </summary>
        /// <returns>True if a bot was hit with any laser</returns>
        public static bool fireLasers()
        {
            lock (gameStatus.locker)
            {
                Dictionary<int, sbyte> botsHit = new Dictionary<int, sbyte>();
                serviceHelpers.signals.showMessage("Firing lasers!", "laser");
                bool botHit = fireBotLasers(ref botsHit);
                bool boardHit = fireBoardLasers(ref botsHit);
                if (botHit || boardHit)
                {
                    Timer watchDog;
                    // Add damage to hit robots
                    foreach (KeyValuePair<int, sbyte> bot in botsHit)
                    {
                        gameStatus.robots[bot.Key].damage += bot.Value;
                        // Start watch dog to skip bots that don't respond in 5 seconds
                        bool timeout = false;
                        watchDog = new Timer(delegate { Console.WriteLine("Bot didn't acknowledge damage value update"); timeout = true; }, null, 5000, Timeout.Infinite);

                        // Wait for bot to acknowledge receipt of updated value
                        SpinWait.SpinUntil(() => botSignals.sendDamage(bot.Key, gameStatus.robots[bot.Key].damage) == "OK" || timeout);

                        // Dispose the watch dog
                        watchDog.Dispose();
                    }
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets an array of all robots currently on a wrench or flag space
        /// </summary>
        /// <returns>An array of robots on a wrench or flag space</returns>
        public static Robot[] wrenches()
        {
            lock (gameStatus.locker)
            {
                serviceHelpers.signals.showMessage("Wrenches and flags healing");
                return gameStatus.robots.Where(r => !r.controllingPlayer.dead && (gameStatus.gameBoard.wrenches.Any(w => w[0] == r.x_pos && w[1] == r.y_pos) || gameStatus.gameBoard.flags.Any(f => f[0] == r.x_pos && f[1] == r.y_pos))).ToArray();
            }
        }

        /// <summary>
        /// Gets a list of all robots currently on a flag space
        /// </summary>
        /// <returns>A list of value pairs of robots on a flag space, and the flag number</returns>
        public static List<int[]> flags()
        {
            lock (gameStatus.locker)
            {
                List<int[]> found = new List<int[]>();
                serviceHelpers.signals.showMessage("Touching flags");
                for (int i = 0, n = gameStatus.gameBoard.flags.Length; i < n; i++)
                {
                    int[] flag = gameStatus.gameBoard.flags[i];
                    Robot bot = gameStatus.robots.FirstOrDefault(r => r.x_pos == flag[0] && r.y_pos == flag[1] && !r.controllingPlayer.shutdown);
                    if (bot != null)
                    {
                        found.Add(new int[] { bot.robotNum, i });
                    }
                }
                return found;
            }
        }

        /// <summary>
        /// Determines if a given coordinate contains a pit
        /// </summary>
        /// <param name="coordinate">The [x, y] coordinate to check</param>
        /// <returns>True if the coordinate contains a pit</returns>
        public static bool onPit(int[] coordinate)
        {
            lock (gameStatus.locker)
            {
                return gameStatus.gameBoard.pits.Any(p => (p[0] == coordinate[0] && p[1] == coordinate[1]));
            }
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
            lock(gameStatus.locker)
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
        }

        /// <summary>
        /// Moves all robots on conveyors
        /// </summary>
        /// <param name="express">True indicates only express conveyors should move</param>
        public static void moveConveyors(bool express)
        {
            lock(gameStatus.locker)
            {
                Robot[] onConveyors;
                List<orderModel> orders = new List<orderModel>();
                // Check if express conveyor movement, then find all robots on the conveyors
                if (express)
                {
                    serviceHelpers.signals.showMessage("Express conveyors moving");
                    onConveyors = gameStatus.robots.Where(r => (gameStatus.gameBoard.expressConveyors.Any(c => (r.x_pos == c.location[0] && r.y_pos == c.location[1])))).ToArray();
                }
                else
                {
                    serviceHelpers.signals.showMessage("All conveyors moving");
                    onConveyors = gameStatus.robots.Where(r => (gameStatus.gameBoard.conveyors.Any(c => (r.x_pos == c.location[0] && r.y_pos == c.location[1]))) || (gameStatus.gameBoard.expressConveyors.Any(c => (r.x_pos == c.location[0] && r.y_pos == c.location[1])))).ToArray();
                }
                List<conveyorModel> moved = new List<conveyorModel>();

                // Check whether each robot will be able to move, or if it is blocked by another robot
                foreach (Robot moving in onConveyors)
                {
                    canMoveOnConveyor(moving, onConveyors, ref moved);
                }

                // Check to see if any robots are trying to move into the same space
                conveyorModel[] collisions = moved.Where(r => (moved.Any(q => (r != q && q.destination[0] == r.destination[0] && q.destination[1] == r.destination[1])))).ToArray();
                foreach (conveyorModel collided in collisions)
                {
                    moved.Remove(collided);
                }

                // Resolve the movement of each bot that is moving
                foreach (conveyorModel findMove in moved)
                {
                    Robot.orientation oldFacing = findMove.bot.currentDirection;
                    moveCalculator.resolveMove(findMove.bot, findMove.space.exit, 1, ref orders, false, true);
                    Robot.orientation desiredFacing = oldFacing;

                    // Check if a robot is being moved onto a space that contains a conveyor
                    conveyor entering = gameStatus.gameBoard.conveyors.FirstOrDefault(c => (c.location[0] == findMove.destination[0] && c.location[1] == findMove.destination[1]));
                    if (entering == null)
                    {
                        entering = gameStatus.gameBoard.expressConveyors.FirstOrDefault(c => (c.location[0] == findMove.destination[0] && c.location[1] == findMove.destination[1]));
                    }
                    if (entering != null)
                    {
                        // Determine if the conveyor space is a corner, and rotate the bot accordingly
                        switch (entering.entrance - entering.exit)
                        {
                            case 1:
                            case -3:
                                if (oldFacing == Robot.orientation.NEG_Y)
                                {
                                    desiredFacing = Robot.orientation.X;
                                }
                                else
                                {
                                    desiredFacing = oldFacing + 1;
                                }
                                break;
                            case -1:
                            case 3:
                                if (oldFacing == Robot.orientation.X)
                                {
                                    desiredFacing = Robot.orientation.NEG_Y;
                                }
                                else
                                {
                                    desiredFacing = oldFacing - 1;
                                }
                                break;
                        }
                    }
                    // Ensure the robot is facing in the correct directon after moving
                    switch (desiredFacing - findMove.bot.currentDirection)
                    {
                        case 3:
                        case -1:
                            orders.Add(new orderModel { botNumber = findMove.bot.robotNum, move = moveCalculator.movement.Right, magnitude = 1, outOfTurn = false });
                            findMove.bot.currentDirection = desiredFacing;
                            break;
                        case -3:
                        case 1:
                            orders.Add(new orderModel { botNumber = findMove.bot.robotNum, move = moveCalculator.movement.Left, magnitude = 1, outOfTurn = false });
                            findMove.bot.currentDirection = desiredFacing;
                            break;
                        case 2:
                        case -2:
                            Random rand = new Random();
                            orders.Add(new orderModel { botNumber = findMove.bot.robotNum, move = (moveCalculator.movement)rand.Next(0, 2), magnitude = 2, outOfTurn = false });
                            findMove.bot.currentDirection = desiredFacing;
                            break;

                    }
                }
                // Send move orders to bots
                foreach (orderModel order in orders)
                {
                    moveCalculator.processMoveOrder(order);
                }
            }
        }

        /// <summary>
        /// Helper method for conveyor movement. Checks to see if a robot on a conveyor is able to move
        /// </summary>
        /// <param name="moving">The robot that is moving</param>
        /// <param name="onCoveyors">An array of all robots that are on conveyors</param>
        /// <param name="movable">A reference to a list of robots on conveyors that are able to move</param>
        /// <returns>True if the robot can move</returns>
        private static bool canMoveOnConveyor(Robot moving, Robot[] onCoveyors, ref List<conveyorModel> movable)
        {
            // See if robot has already been cleared to move
            if (movable.Any(m => m.bot == moving))
            {
                return true;
            }
            int[] destination;
            // Get the conveyor space the robot is on
            conveyor space = gameStatus.gameBoard.conveyors.FirstOrDefault(c => (c.location[0] == moving.x_pos && c.location[1] == moving.y_pos));
            if (space == null)
            {
                space = gameStatus.gameBoard.expressConveyors.First(c => (c.location[0] == moving.x_pos && c.location[1] == moving.y_pos));
            }
            // Find the robot's destination
            switch (space.exit)
            {
                case Robot.orientation.X:
                    destination = new int[] { moving.x_pos + 1, moving.y_pos };
                    break;
                case Robot.orientation.Y:
                    destination = new int[] { moving.x_pos, moving.y_pos + 1 };
                    break;
                case Robot.orientation.NEG_X:
                    destination = new int[] { moving.x_pos - 1, moving.y_pos };
                    break;
                case Robot.orientation.NEG_Y:
                    destination = new int[] { moving.x_pos, moving.y_pos - 1 };
                    break;
                // This default should never execute
                default:
                    destination = new int[0];
                    break;
            }
            // See if there's a robot on the space the bot is trying to move to
            Robot inWay = gameStatus.robots.FirstOrDefault(r => (r.x_pos == destination[0] && r.y_pos == destination[1] && !r.controllingPlayer.dead));
            if (inWay != null)
            {
                // Check if the robot in the way is also on a conveyor
                if (onCoveyors.Contains(inWay))
                {
                    // Recursively check if the robot in the way is able to move
                    if (canMoveOnConveyor(inWay, onCoveyors, ref movable))
                    {
                        // The robot can move
                        movable.Add(
                            new conveyorModel
                            {
                                space = space,
                                destination = destination,
                                bot = moving
                            });
                        return true;
                    }
                }
                return false;
            }
            else
            {
                // No bot in the way, the robot can move              
                movable.Add(
                    new conveyorModel
                    {
                        space = space,
                        destination = destination,
                        bot = moving
                    });
                return true;
            }
        }

        /// <summary>
        /// Fires robot lasers and adds the damage to a dictionary
        /// </summary>
        /// <returns>True if a bot was hit</returns>
        private static bool fireBotLasers(ref Dictionary<int, sbyte> hit)
        {
            lock (gameStatus.locker)
            {
                // Find robots that were hit
                foreach (Player shooter in gameStatus.players)
                {
                    if (!shooter.shutdown && !shooter.dead)
                    {
                        Robot bot = shooter.playerRobot;
                        int shot = LoS(new int[] { bot.x_pos, bot.y_pos }, bot.currentDirection, botNumber: bot.robotNum);
                        if (shot != -1)
                        {
                            if (hit.ContainsKey(shot))
                            {
                                hit[shot]++;
                            }
                            else
                            {
                                hit.Add(shot, 1);
                            }
                        }
                    }
                }
                return hit.Count > 0;
            }
        }

        /// <summary>
        /// Fires the board lasers and adds the damage to a dictionary
        /// </summary>
        /// <returns>True if a bot was hit</returns>
        private static bool fireBoardLasers(ref Dictionary<int, sbyte> hit)
        {
            lock (gameStatus.locker)
            {
                foreach (laser shooter in gameStatus.gameBoard.lasers)
                {
                    int shot = LoS(shooter.start, shooter.facing, shooter.end);
                    if (shot != -1)
                    {
                        if (hit.ContainsKey(shot))
                        {
                            hit[shot] += shooter.strength;
                        }
                        else
                        {
                            hit.Add(shot, shooter.strength);
                        }
                    }
                }
                return hit.Count > 0;
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
