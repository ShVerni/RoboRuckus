using System;
using System.Threading;
using System.Linq;
using Microsoft.AspNet.SignalR.Hubs;
using System.Security.Cryptography;
using RoboRuckus.RuckusCode.Movement;

namespace RoboRuckus.RuckusCode
{
    /// <summary>
    /// Controls inputs from users.
    /// All public methods in this class sould be wrapped in a lock on the same object
    /// since there is only one game state multiple players could try to modify.
    /// </summary>
    public class playerSignals
    {
        private readonly static Lazy<playerSignals> _instance = new Lazy<playerSignals>(() => new playerSignals());
        private readonly RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        // There can be no more than 256 cards in the deck
        private byte numberOfCards;
        // Prevents the player timer from being started multiple times
        private bool timerStarted = false;

        /// <summary>
        /// Singleton instance for thread saftey
        /// </summary>
        public static playerSignals Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        /// <summary>
        /// Players connected to player hub
        /// </summary>
        private static IHubConnectionContext<dynamic> Clients;

        /// <summary>
        /// Constructor
        /// </summary>
        private playerSignals()
        {
            Clients = serviceHelpers.playerHubContext.Clients;
            numberOfCards = (byte)gameStatus.movementCards.Length;
        }

        /// <summary>
        /// Processes a player's move, if all players have submitted
        /// their moves, executes those moves.
        /// </summary>
        /// <param name="caller">The player client submitting the move</param>
        /// <param name="cards">The cards submitted for their move</param>
        public void submitMove(Player caller, Hubs.cardModel[] cards)
        {
            lock (gameStatus.locker)
            {
                if (!gameStatus.winner)
                {
                    if (caller.move == null)
                    {
                        caller.move = cards;
                    }

                    // Check to see if timer needs to be started
                    if (!timerStarted && checkTimer())
                    {
                        return;
                    }
                    // Checks if all players have submitted their moves
                    else if (gameStatus.players.Count(p => (p.move != null || p.dead || p.shutdown)) < gameStatus.numPlayersInGame)
                    {
                        return;
                    }
                    timerStarted = false;

                    // Execute player moves                  
                    moveCalculator.executeRegisters();

                    // Reset for next round
                    if (!gameStatus.winner)
                    {
                        nextRound();
                    }
                }
            }
            // Checks is a timer needs to be started right away
            Thread.Sleep(2000);
            checkTimer();
        }

        /// <summary>
        /// Sends a message to the player screens
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="sound">An optional sound effect to play</param>
        public void showMessage(string message, string sound = "")
        {
            lock (gameStatus.locker)
            {
                Clients.All.displayMessage(message, sound);
            }
        }

        /// <summary>
        /// Used to deal cards to all the players
        /// </summary>
        public void dealPlayers()
        {
            lock (gameStatus.locker)
            {
                Clients.All.requestdeal();
            }
        }

        /// <summary>
        /// Sends the current move being executed to the players
        /// </summary>
        /// <param name="move">The move model being executed</param>
        public void displayMove(moveModel move, int register)
        {
            lock (gameStatus.locker)
            {
                string card = gameStatus.movementCards[move.card.cardNumber];
                string robot = move.bot.robotName;
                Clients.All.showMove(card, robot, register + 1);
            }
        }

        /// <summary>
        /// Deals cards to a player
        /// </summary>
        /// <param name="caller">The player client requesting a deal</param>
        /// <returns>The cards dealt to the player client</returns>
        public byte[] dealPlayer(Player caller)
        {
            lock (gameStatus.locker)
            {
                if (!caller.dead && !gameStatus.winner)
                {
                    // Check if the player has already been dealt
                    if (caller.cards == null)
                    {
                        byte[] cards;
                        if (caller.shutdown)
                        {
                            cards = new byte[0];
                        }
                        else
                        {
                            cards = new byte[9 - caller.playerRobot.damage];
                            // Draw some cards
                            for (int i = 0; i < cards.Length; i++)
                            {
                                cards[i] = drawCard();
                            }
                        }
                        // Assign cards to player
                        caller.cards = cards;
                        return cards;
                    }
                    else
                    {
                        return caller.cards;
                    }
                }
                else
                {
                    return new byte[0];
                }
            }
        }

        /// <summary>
        /// Updates the health of every player
        /// </summary>
        public void updateHealth()
        {
            lock (gameStatus.locker)
            {
                bool first = true;
                string result = "[";
                foreach (Player inGame in gameStatus.players)
                {
                    if (!first)
                    {
                        result += ",";
                    }
                    first = false;
                    result += inGame.playerRobot.damage.ToString();
                }
                result += "]";
                Clients.All.UpdateHealth(result);
            }
        }

        /// <summary>
        /// Resets the game to the initial state
        /// <param name="resetAll">If 0 reset game with current players, if 1 reset game to initial state</param>
        /// </summary>
        public void resetGame(int resetAll)
        {
            lock (gameStatus.locker)
            {
                Timer watchDog;
                gameStatus.winner = false;
                gameStatus.lockedCards.Clear();
                gameStatus.playersNeedEntering = false;
                if (resetAll == 0)
                {
                    foreach (Player p in gameStatus.players)
                    {
                        p.dead = false;
                        p.lockedCards.Clear();
                        p.move = null;
                        p.cards = null;
                        p.lives = 3;
                        p.shutdown = false;
                        p.willShutdown = false;
                    }
                }
                else
                {
                    gameStatus.players.Clear();
                    gameStatus.gameReady = false;
                    gameStatus.numPlayersInGame = 0;
                }
                foreach (Robot r in gameStatus.robots)
                {
                    r.y_pos = -1;
                    r.x_pos = -1;
                    r.damage = 0;
                    r.flags = 0;
                    if (resetAll == 1)
                    {
                        // Start watch dog to skip bots that don't respond in 5 seconds
                        bool timeout = false;
                        watchDog = new Timer(delegate { Console.WriteLine("Bot didn't acknowledge reset order"); timeout = true; }, null, 5000, Timeout.Infinite);

                        // Wait for bot to acknowledge receipt of order
                        SpinWait.SpinUntil(() => botSignals.sendReset(r.robotNum) || timeout);

                        // Dispose the watch dog
                        watchDog.Dispose();

                        r.controllingPlayer = null;
                        gameStatus.robotPen.Add(r);
                    }
                }
                if (resetAll == 1)
                {
                    gameStatus.robots.Clear();
                }
                Clients.All.Reset(resetAll);
            }
        }

        /// <summary>
        /// Draws a random available card
        /// </summary>
        /// <returns>The card drawn</returns>
        public byte drawCard()
        {
            lock (gameStatus.locker)
            {
                if (numberOfCards <= 0)
                    throw new ArgumentOutOfRangeException("numberOfCards");

                byte drawn;
                do
                {
                    // Create a byte array to hold the random value. 
                    byte[] randomNumber = new byte[1];
                    do
                    {
                        rng.GetBytes(randomNumber);
                    }
                    while (randomNumber[0] >= numberOfCards);
                    drawn = randomNumber[0];
                }
                while (gameStatus.deltCards.Contains(drawn) || gameStatus.lockedCards.Contains(drawn));
                gameStatus.deltCards.Add(drawn);
                return drawn;
            }
        }

        /// <summary>
        /// Resets the bots and game for the next round
        /// </summary>
        private void nextRound()
        {
            // Reset players
            foreach (Player inGame in gameStatus.players)
            {
                // Remove player's cards
                inGame.cards = null;
                inGame.move = null;
                // Check if player is shutting down
                if (inGame.willShutdown && !inGame.dead)
                {
                    inGame.shutdown = true;
                    inGame.playerRobot.damage = 0;
                    inGame.willShutdown = false;
                }
                else
                {
                    // Bring any shutdown players back online
                    inGame.shutdown = false;
                }
            }
            // Clear dealt cards
            gameStatus.deltCards.Clear();

            // Check for winner
            if (!gameStatus.winner)
            {
                // Check if there are dead players with lives left who need to re-enter the game
                if (gameStatus.players.Any(p => (p.dead && p.lives > 0)))
                {
                    gameStatus.playersNeedEntering = true;
                    showMessage("Dead robots re-entering floor, please be patient.");
                }
                else
                {
                    showMessage("");
                    // Check if all players are shutdown or out of the game
                    if (gameStatus.players.All(p => p.shutdown || p.lives <= 0))
                    {
                        // Clear dealt cards
                        Clients.All.deal(new byte[0], new byte[0]);

                        // Alert players to what's happening
                        showMessage("All active players are shutdown, next round starting now.");
                        Thread.Sleep(3000);

                        // Skip directly to executing the movement registers                 
                        moveCalculator.executeRegisters();

                        // Reset for next round
                        if (!gameStatus.winner)
                        {
                            nextRound();
                        }
                        return;
                    }
                    dealPlayers();
                }
            }
        }

        /// <summary>
        /// Checks to see if a player timer needs to be started, and starts one if needed.
        /// </summary>
        /// <returns>Whether a timer was started</returns>
        private bool checkTimer()
        {
            // Makes sure there is more than one living player in the game, and checks if there is only one player who hasn't submitted thier program.
            if (gameStatus.playerTimer && gameStatus.players.Count(p => !p.dead) > 1 && gameStatus.players.Count(p => (p.move != null || p.dead || p.shutdown)) == (gameStatus.numPlayersInGame - 1))
            {
                timerStarted = true;
                Clients.All.startTimer();
                return true;
            }
            return false;
        }
    }
}
