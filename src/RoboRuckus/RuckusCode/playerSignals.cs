using System;
using System.Threading;
using System.Linq;
using Microsoft.AspNet.SignalR.Hubs;
using System.Security.Cryptography;

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
        public void submitMove(player caller, Hubs.cardModel[] cards)
        {
            lock (gameStatus.locker)
            {
                if (caller.move == null)
                {
                    caller.move = cards;
                    // Checks if all players have submitted their moves
                    if (gameStatus.players.Any(p => (p.move == null && !p.dead && !p.shutdown)))
                    {
                        return;
                    }
                    moveCalculator.executeMoves();

                    // Reset for next round
                    foreach (player inGame in gameStatus.players)
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
                    Clients.All.displayMessage("", "");

                    // Check if all players are shutdown
                    if (gameStatus.players.All(p => p.shutdown))
                    {
                        gameStatus.players.Select(p => p.shutdown = false);
                        foreach (player inGame in gameStatus.players)
                        {
                            inGame.shutdown = false;
                            inGame.playerRobot.damage = 0;
                        }
                    }

                    // Have player clients request a deal
                    Clients.All.requestdeal();
                }
            }
        }

        /// <summary>
        /// Fires robot lasers and calculates damage
        /// </summary>
        /// <returns>Wheter a robot was hit</returns>
        public bool fireLasers()
        {
            bool hit = false;
            lock (gameStatus.locker)
            {
                Clients.All.displayMessage("Firing lasers!", "laser");
                Timer watchDog;
                foreach (player shooter in gameStatus.players)
                {
                    if (!shooter.shutdown && !shooter.dead)
                    {
                        robot bot = shooter.playerRobot;
                        int shot = moveCalculator.botLoS(new int[] { bot.x_pos, bot.y_pos }, bot.currentDirection, botNumber: bot.robotNum);
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
            }
            return hit;
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
                int playerNumber = move.bot.controllingPlayer.playerNumber + 1;
                Clients.All.showMove(card, playerNumber);
            }
        }

        /// <summary>
        /// Deals cards to a player
        /// </summary>
        /// <param name="caller">The player client requesting a deal</param>
        /// <returns>The cards dealt to the player client</returns>
        public byte[] dealPlayer(player caller)
        {
            lock (gameStatus.locker)
            {
                if (!caller.dead)
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
                foreach (player inGame in gameStatus.players)
                {
                    if (first)
                    {
                        first = false;
                        result += inGame.playerRobot.damage.ToString();
                    }
                    else
                    {
                        result += ", " + inGame.playerRobot.damage.ToString();
                    }
                }
                result += "]";
                Clients.All.UpdateHealth(result);
            }
        }

        /// <summary>
        /// Resets the game to the initial state
        /// </summary>
        public void resetGame()
        {
            lock(gameStatus.locker)
            {
                gameStatus.lockedCards.Clear();
                foreach (player p in gameStatus.players)
                {
                    p.dead = false;
                    p.lockedCards.Clear();
                    p.move = null;
                    p.cards = null;
                }
                foreach (robot r in gameStatus.robots)
                {
                    r.y_pos = -1;
                    r.x_pos = -1;
                    r.damage = 0;
                }
                Clients.All.Reset();
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
