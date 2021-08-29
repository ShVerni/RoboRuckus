using System.Threading;
using System.Net;

namespace RoboRuckus.RuckusCode
{
    public class Robot
    {
        public int x_pos = -1;
        public int y_pos = -1;
        // The number of flags the robot has touched
        public int flags = 0;
        // There shouldn't be more that 256 robots in the game.
        public byte robotNum;

        /// <summary>
        /// Encodes possible bot communication methods 
        /// </summary>
        public enum communicationModes
        {
            IP = 0,
            Bluetooth = 1
        }

        /// <summary>
        /// The bot's current mode of communication
        /// </summary>
        public communicationModes mode;

        /// <summary>
        /// The axis/direction along which the bot is currently facing
        /// </summary>
        public orientation currentDirection = orientation.Y;
        public IPAddress robotAddress;
        public string robotName;
        public Player controllingPlayer = null;
        public EventWaitHandle moving = new ManualResetEvent(false);

        /// <summary>
        /// Robot's last check-point location
        /// </summary>
        public int[] lastLocation;

        /// <summary>
        /// Encodes the cardinal directions a robot can be oriented towards
        /// </summary>
        public enum orientation
        {
            X = 0,
            Y = 1,
            NEG_X = 2,
            NEG_Y = 3
        }

        // There shouldn't be more thant 127 points of damage a bot can take, it's signed to allow damage subtraction
        private  sbyte _damage = 0;

        /// <summary>
        /// Gets and sets the robot's damage, locking or unlocking
        /// cards and destroying the robot as needed
        /// </summary>
        public sbyte damage
        {
            get
            {
                return _damage;
            }
            set
            {
                // Only one thread should be issuing orders and/or modifying damage at a time
                lock (gameStatus.locker)
                {
                    if (value < 0)
                    {
                        _damage = 0;
                    }
                    else if (value > 10)
                    {
                        _damage = 10;
                    }
                    else
                    {
                        _damage = value;
                    }
                    // Check for a need to modify locked cards
                    if (_damage > 4)
                    {
                        // Bot is dead!
                        if (_damage >= 10)
                        {
                            controllingPlayer.dead = true;
                        }
                        else
                        {
                            if (controllingPlayer.lockedCards.Count > _damage - 4)
                            {
                                // Unlock cards if necessary
                                while (controllingPlayer.lockedCards.Count > _damage - 4)
                                {
                                    // Unlock the most recently locked card
                                    gameStatus.lockedCards.Remove(controllingPlayer.lockedCards[controllingPlayer.lockedCards.Count - 1]);
                                    controllingPlayer.lockedCards.Remove(controllingPlayer.lockedCards[controllingPlayer.lockedCards.Count - 1]);
                                }
                            }
                            else
                            {
                                // Lock cards if necessary
                                for (int i = controllingPlayer.lockedCards.Count; i < _damage - 4; i++)
                                {
                                    byte card;
                                    // Is the player shutdown and taking damage? (Ouch!)
                                    if (controllingPlayer.shutdown)
                                    {
                                        card = serviceHelpers.signals.drawCard();
                                    }
                                    else
                                    {
                                        card = controllingPlayer.move[4 - i].cardNumber;
                                    }

                                    // Check if the card is already locked, to be safe
                                    if (!gameStatus.lockedCards.Contains(card))
                                    {
                                        gameStatus.lockedCards.Add(card);
                                        controllingPlayer.lockedCards.Add(card);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Remove locked cards, just in case
                        if (controllingPlayer.lockedCards.Count != 0)
                        {
                            foreach (byte card in controllingPlayer.lockedCards)
                            {
                                gameStatus.lockedCards.Remove(card);
                            }
                            controllingPlayer.lockedCards.Clear();
                        }
                    }
                }
            }
        }
    }
}