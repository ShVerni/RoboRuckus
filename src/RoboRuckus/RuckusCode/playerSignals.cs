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
        // There can be no more than 256 card in the deck
        private byte numberOfCards;

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
                        // Checks if all players have submitted their moves
                        if (gameStatus.players.Any(p => (p.move == null && !p.dead && !p.shutdown)))
                        {
                            return;
                        }
                    }
                    // Execute player moves
                    moveCalculator.executeRegisters();
                    // Reset for next round
                    if (!gameStatus.winner)
                    {
                        nextRound();
                    }
                }
            }
        }

        /// <summary>
        /// Sends a message to the player screens
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="sound">An optional sound effect to play</param>
        public void showMessage(string message, string sound = "")
        {
            Clients.All.displayMessage(message, sound);
        }

        /// <summary>
        /// Resets the bots and game for the next round
        /// </summary>
        private void nextRound()
        {
            foreach (Player inGame in gameStatus.players)
            {
                inGame.cards = null;
                inGame.move = null;
                if (inGame.willShutdown && !inGame.dead)
                {
                    inGame.shutdown = true;
                    inGame.playerRobot.damage = 0;
                    inGame.willShutdown = false;
                }
                else
                {
                    inGame.shutdown = false;
                }
            }
            gameStatus.deltCards.Clear();
            showMessage("");

            // Check for winner
            if (!gameStatus.winner)
            {
                // Check if there are dead players with lives left who need to re-enter the game
                if (gameStatus.players.Any(p => (p.dead && p.lives > 0)))
                {
                    gameStatus.playersNeedEntering = true;

                }
                else
                {
                    // Check if all players are shutdown or out of the game
                    if (gameStatus.players.All(p => p.shutdown || p.lives <= 0))
                    {
                        gameStatus.players.Select(p => p.shutdown = false);
                        foreach (Player inGame in gameStatus.players)
                        {
                            inGame.shutdown = false;
                            inGame.playerRobot.damage = 0;
                        }
                    }
                    dealPlayers();
                }
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
        public void displayMove(moveModel move)
        {
            lock (gameStatus.locker)
            {
                string card = gameStatus.movementCards[move.card.cardNumber];
                string robot = move.bot.robotName;
                Clients.All.showMove(card, robot);
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
        /// <param name="resetAll">IF 0 reset game with current player, if 1 reset game to initial state</param>
        /// </summary>
        public void resetGame(int resetAll)
        {
            lock(gameStatus.locker)
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
    }
}
