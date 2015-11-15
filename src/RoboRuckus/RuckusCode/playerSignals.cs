using System;
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
                }
                // Execute player moves
                moveCalculator.executeRegisters();
                // Reset for next round
                nextRound();
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
        /// 
        /// </summary>
        private void nextRound()
        {
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
            showMessage("");

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
                foreach (Robot r in gameStatus.robots)
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
