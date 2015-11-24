using System.Collections.Generic;

namespace RoboRuckus.RuckusCode
{
    public class player
    {
        /// <summary>
        /// Zero ordered player number
        /// </summary>
        public byte playerNumber;

        public Robot playerRobot = null;

        /// <summary>
        /// The cards currently dealt to the player
        /// </summary>
        public byte[] cards = null;

        /// <summary>
        /// The player's submitted move
        /// </summary>
        public Hubs.cardModel[] move = null;

        /// <summary>
        /// The player's currently locked cards
        /// </summary>
        public List<byte> lockedCards = new List<byte>();

        /// <summary>
        /// True if player is currently shut down
        /// </summary>
        public bool shutdown = false;

        /// <summary>
        /// True if player is shutting down this turn
        /// </summary>
        public bool willShutdown = false;

        private bool _dead;

        /// <summary>
        /// Show's whether a player is dead, and if they are it removes
        /// that player and their bot from the current game.
        /// </summary>
        public bool dead
        {
            get
            {
                return _dead;
            }
            set
            {
                lock(gameStatus.locker)
                {
                    if (value == true)
                    {
                        foreach (byte card in lockedCards)
                        {
                            gameStatus.lockedCards.Remove(card);
                        }
                        lockedCards.Clear();
                        playerRobot.x_pos = -1;
                        playerRobot.y_pos = -1;
                    }
                    _dead = value;
                }
            }
        }

        /// <summary>
        /// Player constructor. A player number is required.
        /// </summary>
        /// <param name="Number">The player number</param>
        public player(byte Number)
        {
            playerNumber = Number;
        }
    }
}