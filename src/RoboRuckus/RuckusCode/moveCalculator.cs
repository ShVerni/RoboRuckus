using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace RoboRuckus.RuckusCode
{
    public static class moveCalculator
    {
        public enum movement
        {
            Left = 0,
            Right = 1,
            Forward = 2,
            Backward = 3
        }

        /// <summary>
        /// Executes and resolves all the moves submitted by the players
        /// </summary>
        public static void executeMoves()
        {
            lock (gameStatus.locker)
            {
                List<orderModel> orders = new List<orderModel>();
                Timer watchDog;
                // Loop through each register
                for (int i = 0; i < 5; i++)
                {
                    int inGame = gameStatus.players.Count(p => (!p.dead && !p.shutdown));
                    moveModel[] register = new moveModel[inGame];
                    // Add the cards to the register
                    int reg = 0;
                    for (int j = 0; j < gameStatus.players.Count; j++)
                    {
                        if (!gameStatus.players[j].dead && !gameStatus.players[j].shutdown)
                        {
                            player mover = gameStatus.players[j];
                            register[reg] = new moveModel { card = mover.move[i], bot = mover.playerRobot };
                            reg++;
                        }
                    }
                    // Sort the register by card priority
                    register = register.OrderByDescending(order => order.card.priority).ToArray();
                    // Resolve a move for each card
                    foreach (moveModel move in register)
                    {
                        playerSignals.Instance.displayMove(move);
                        orders = calculateMove(move);
                        // Send each order to the appropriate robot
                        foreach (orderModel order in orders)
                        {
                            // For debugging/diagnostics
                            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

                            /* DISABLE CONTENT IN THIS LOOP FOR TESTING WITHOUT BOTS */
                            // Reset the bot's wait handle
                            gameStatus.robots[order.botNumber].moving.Reset();

                            // Start watch dog to skip bots that don't respond in 15 seconds
                            bool timeout = false;
                            watchDog = new Timer(delegate { Console.WriteLine("Bot didn't acknowledge move order"); timeout = true; }, null, 15000, Timeout.Infinite);

                            // Wait for bot to acknowledge receipt of orders
                            SpinWait.SpinUntil(() => botSignals.sendMoveCommand(order) == "OK" || timeout);

                            // Dispose the watch dog
                            watchDog.Dispose();

                            // Start a watchdog to skip bots that don't finish moving in 10 seconds (may need tweaking or removing)
                            watchDog = new Timer(delegate { Console.WriteLine("Bot didn't finish moving"); gameStatus.robots[order.botNumber].moving.Set(); }, null, 10000, Timeout.Infinite);

                            // Wait for bot to finish moving
                            gameStatus.robots[order.botNumber].moving.WaitOne();

                            // Dispose the watch dog
                            watchDog.Dispose();

                            // Let the bot become ready again (min: 150ms)
                            Thread.Sleep(250);

                            // For debugging/diagnostics
                            stopwatch.Stop();
                            Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds);
                        }
                    }
                    if (playerSignals.Instance.fireLasers())
                    {
                        Thread.Sleep(800);
                        playerSignals.Instance.updateHealth();
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        Thread.Sleep(800);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates and resolves all necessary moves for a robot and
        /// any other robots affected by the move.
        /// </summary>
        /// <param name="move">The movment model to resolve</param>
        /// <returns>A list of orders for robots</returns>
        private static List<orderModel> calculateMove(moveModel move)
        {
            Random rand = new Random();
            List<orderModel> orders = new List<orderModel>();
            // Check for the kind of movement, and resolve turns here
            switch (move.card.direction)
            {
                case ("forward"):
                    // Resolve complicated move here
                    resolveMove(move.bot, move.bot.currentDirection, move.card.magnitude, ref orders, false);
                    break;

                case ("left"):
                    // Change the robot's oreientation to what it will be after the move
                    if (move.bot.currentDirection == robot.orientation.NEG_Y)
                    {
                        move.bot.currentDirection = robot.orientation.X;
                    }
                    else
                    {
                        move.bot.currentDirection++;
                    }
                    orders.Add(new orderModel { botNumber = move.bot.robotNum, move = movement.Left, magnitude = 1, outOfTurn = false } );
                    break;

                case ("right"):
                    if (move.bot.currentDirection == robot.orientation.X)
                    {
                        move.bot.currentDirection = robot.orientation.NEG_Y;
                    }
                    else
                    {
                        move.bot.currentDirection--;
                    }
                    orders.Add(new orderModel { botNumber = move.bot.robotNum, move = movement.Right, magnitude = 1, outOfTurn = false });
                    break;

                case ("backup"):
                    robot.orientation opposite = robot.orientation.Y;
                    switch (move.bot.currentDirection)
                    {
                        case robot.orientation.X:
                            opposite = robot.orientation.NEG_X;
                            break;
                        case robot.orientation.NEG_X:
                            opposite = robot.orientation.X;
                            break;
                        case robot.orientation.Y:
                            opposite = robot.orientation.NEG_Y;
                            break;
                        case robot.orientation.NEG_Y:
                            opposite = robot.orientation.Y;
                            break;
                    }
                    resolveMove(move.bot, opposite, move.card.magnitude, ref orders, false);
                    break;

                case ("uturn"):
                    switch(move.bot.currentDirection)
                    {
                        case robot.orientation.X:
                            move.bot.currentDirection = robot.orientation.NEG_X;
                            break;
                        case robot.orientation.NEG_X:
                            move.bot.currentDirection = robot.orientation.X;
                            break;
                        case robot.orientation.Y:
                            move.bot.currentDirection = robot.orientation.NEG_Y;
                            break;
                        case robot.orientation.NEG_Y:
                            move.bot.currentDirection = robot.orientation.Y;
                            break;
                    }                    
                    orders.Add(new orderModel { botNumber = move.bot.robotNum, move = (movement)rand.Next(0, 2), magnitude = 2, outOfTurn = false });
                    break;
            }
            return orders;
        }

        /// <summary>
        /// Creates a movement program that resolves one bot's movement and its
        /// impact on any other bots on the board. Executes recursively.
        /// Currently, a robot cannot fall off the edge of the board, it acts as a wall.
        /// </summary>
        /// <param name="Bot">The bot being moved</param>
        /// <param name="direction">The directon the bot is moving.</param>
        /// <param name="magnitude">The number of spaces being moved</param>
        /// <param name="orders">A reference to the list of move orders to modify</param>
        /// <param name="outOfTurn">True if it's not the turn of the bot whose move is being resolved</param>
        /// <returns>The total number of spaces the bot will actually be moving (i.e. is able to move)</returns>
        private static int resolveMove(robot Bot, robot.orientation direction, int magnitude, ref List<orderModel> orders, bool OutOfTurn)
        {
            int newCordX = -1;
            int newCordY = -1;
            int[] destination = new int[2];
            // Check for edge or obstacles (NOTE: Robots currently cannot fall off edge of board)
            for (int i = 1; i <= magnitude; i++)
            {
                switch (direction)
                {
                    case robot.orientation.X:
                        newCordX = Bot.x_pos + i;
                        newCordY = Bot.y_pos;
                        destination = new int[] { newCordX, newCordY };
                        break;
                    case robot.orientation.Y:
                        newCordY = Bot.y_pos + i;
                        newCordX = Bot.x_pos;
                        destination = new int[] { newCordX, newCordY };
                        break;
                    case robot.orientation.NEG_X:
                        newCordX = Bot.x_pos - i;
                        newCordY = Bot.y_pos;
                        destination = new int[] { newCordX, newCordY };
                        break;
                    case robot.orientation.NEG_Y:
                        newCordY = Bot.y_pos - i;
                        newCordX = Bot.x_pos;
                        destination = new int[] { newCordX, newCordY };
                        break;
                }
                if (newCordX > gameStatus.boardSizeX || newCordY > gameStatus.boardSizeY || newCordX < 0 || newCordY < 0 ||isObstacle(new int[] { Bot.x_pos, Bot.y_pos }, destination))
                {
                    magnitude= i - 1;
                    break;
                }
            }
            int total = magnitude;
            // Check if there's no movement
            if (magnitude == 0)
            {               
                orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Forward, magnitude = 0, outOfTurn = OutOfTurn });
                return 0;
            }
            else
            {
                // Check for, and resolve movements of, other robots in the way
                int remaining = magnitude;
                robot botFound = null;
                for (int i = 1; i <= magnitude; i++)
                {
                    switch (direction)
                    {
                        case robot.orientation.X:
                            destination = new int[] { Bot.x_pos + i, Bot.y_pos };
                            break;
                        case robot.orientation.Y:
                            destination = new int[] { Bot.x_pos, newCordY = Bot.y_pos + i };
                            break;
                        case robot.orientation.NEG_X:
                            destination = new int[] { newCordX = Bot.x_pos - i, Bot.y_pos };
                            break;
                        case robot.orientation.NEG_Y:
                            destination = new int[] { Bot.x_pos, newCordY = Bot.y_pos - i };
                            break;
                    }
                    // Check for any other bots on that space
                    botFound = gameStatus.robots.FirstOrDefault(r => (r.robotNum != Bot.robotNum && r.x_pos == destination[0] && r.y_pos == destination[1]));
                    if (botFound != null)
                    {
                        break;
                    }
                    remaining--;
                }

                bool rotated = false;
                total = magnitude - remaining;
                if (magnitude - remaining > 0)
                {
                    // Rotate bot to appropraaite direction if necessary, then move
                    switch (direction - Bot.currentDirection)
                    {
                        case 0:
                            orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Forward, magnitude = magnitude - remaining, outOfTurn = OutOfTurn });
                            break;
                        case -2:
                        case 2:
                            orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Backward, magnitude = magnitude - remaining, outOfTurn = OutOfTurn });
                            break;
                        case 3:
                        case -1:
                            orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Right, magnitude = 1, outOfTurn = OutOfTurn });
                            orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Forward, magnitude = magnitude - remaining, outOfTurn = OutOfTurn });
                            rotated = true;
                            break;
                        case -3:
                        case 1:
                            orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Left, magnitude = 1, outOfTurn = OutOfTurn });
                            orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Forward, magnitude = magnitude - remaining, outOfTurn = OutOfTurn });
                            rotated = true;
                            break;
                    }
                }
                if (botFound != null)
                {
                    int otherMoved = resolveMove(botFound, direction, remaining, ref orders, true);
                    if (otherMoved > 0)
                    {
                        // Bot didn't move but now may need to rotate.
                        if (magnitude - remaining == 0)
                        {
                            switch (direction - Bot.currentDirection)
                            {
                                case -1:
                                    orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Right, magnitude = 1, outOfTurn = OutOfTurn });
                                    rotated = true;
                                    break;
                                case 1:
                                    orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Left, magnitude = 1, outOfTurn = OutOfTurn });
                                    rotated = true;
                                    break;
                            }
                        }
                        // Check if we're moving forward or backward.
                        switch (direction - Bot.currentDirection)
                        {
                            case -2:
                            case 2:
                                orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Backward, magnitude = otherMoved, outOfTurn = OutOfTurn });
                                break;
                            default:
                                orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Forward, magnitude = otherMoved, outOfTurn = OutOfTurn });
                                break;
                        }
                        total += otherMoved;
                    }
                }

                // Undo any rotation from out-of-turn bots
                if (rotated)
                {
                    switch (direction - Bot.currentDirection)
                    {
                        case 3:
                        case -1:
                            orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Left, magnitude = 1, outOfTurn = OutOfTurn });
                            break;
                        case -3:
                        case 1:
                            orders.Add(new orderModel { botNumber = Bot.robotNum, move = movement.Right, magnitude = 1, outOfTurn = OutOfTurn });
                            break;
                    }
                }

                // Update robot's coordinates
                if (total > 0)
                {
                    switch (direction)
                    {
                        case robot.orientation.X:
                            Bot.x_pos += total;
                            break;
                        case robot.orientation.NEG_X:
                            Bot.x_pos -= total;
                            break;
                        case robot.orientation.Y:
                            Bot.y_pos += total;
                            break;
                        case robot.orientation.NEG_Y:
                            Bot.y_pos -= total;
                            break;
                    }
                }
                return total;
            }
        }

        /// <summary>
        /// Checks for obstacles on the baord that block movement and LoS
        /// TODO: Not implemented yet, implement this.
        /// </summary>
        /// <param name="fromCord">{ X, Y } The coordinate the bot will be moving from</param>
        /// <param name="toCord">{ X, Y } The coordinate the bot will be moving to</param>
        /// <returns>True if there's a non-bot obstacle between those two spaces</returns>
        private static bool isObstacle(int[] fromCord, int[] toCord)
        {
            return false;
        }

        /// <summary>
        /// Finds if a robot is within line of sight between two coordinates. The fromCord should start at the occupied square and the toCord should be in the direction faced.
        /// </summary>
        /// <param name="fromCord">{ X, Y } The coordinate to start looking for a LoS on a bot</param>
        /// <param name="toCord">{ X, Y } Optionally, a coordinate to stop looking (defaults to edge of board)</param>
        /// <param name="botNumber">If a bot is looking for another bot, ensures it doesn't find itself</param>
        /// <returns>The bot number, or -1 for no result</returns>
        public static int botLoS(int[] fromCord, robot.orientation facing, int[] toCord = null, int botNumber = -1)
        {
            robot bot = null;
            switch (facing)
            {
                case robot.orientation.X:
                    toCord = toCord == null ? new int[] { gameStatus.boardSizeX, gameStatus.boardSizeY } : toCord;
                    bot = gameStatus.robots.OrderBy(r => r.x_pos).FirstOrDefault(r => (r.robotNum != botNumber && r.x_pos >= fromCord[0] && r.x_pos <= toCord[0] && r.y_pos == fromCord[1]));
                    break;
                case robot.orientation.Y:
                    toCord = toCord == null ? new int[] { gameStatus.boardSizeX, gameStatus.boardSizeY } : toCord;
                    bot = gameStatus.robots.OrderBy(r => r.y_pos).FirstOrDefault(r => (r.robotNum != botNumber && r.y_pos >= fromCord[1] && r.y_pos <= toCord[1] && r.x_pos == fromCord[0]));
                    break;
                case robot.orientation.NEG_X:
                    toCord = toCord == null ? new int[] { 0, 0 } : toCord;
                    bot = gameStatus.robots.OrderByDescending(r => r.x_pos).FirstOrDefault(r => (r.robotNum != botNumber && r.x_pos <= fromCord[0] && r.x_pos >= toCord[0] && r.y_pos == fromCord[1]));
                    break;
                case robot.orientation.NEG_Y:
                    toCord = toCord == null ? new int[] { 0, 0 } : toCord;
                    bot = gameStatus.robots.OrderByDescending(r => r.y_pos).FirstOrDefault(r => (r.robotNum != botNumber && r.y_pos <= fromCord[1] && r.y_pos >= toCord[1] && r.x_pos == fromCord[0]));
                    break;
            }

            // Check to see if a robot was found, and if so if there's an obstacle blocking LoS.
            if (bot == null || isObstacle(fromCord, new int[] { bot.x_pos, bot.y_pos }))
            {
                // Obstacle is blocking, no bot in LoS
                return -1;
            }
            return bot.robotNum;
        }
    }

    /// <summary>
    /// A convenient way to pair a movement card with a robot
    /// </summary>
    public class moveModel
    {
        public Hubs.cardModel card;
        public robot bot;
    }

    /// <summary>
    /// A convenient representation of a movement order for a robot
    /// </summary>
    public class orderModel
    {
        public int botNumber;
        public moveCalculator.movement move;
        public int magnitude;
        public bool outOfTurn;

        /// <summary>
        /// Creates a string represnetation of an orderModel that contains an order
        /// which can be sent to the robots.
        /// </summary>
        /// <returns>The string representation of an order</returns>
        public override string ToString()
        {
            return ((int)move).ToString() + magnitude.ToString() + (outOfTurn ? "1" : "0");
        }
    }
}