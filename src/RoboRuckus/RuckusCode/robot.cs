using System.Threading;
using System.Net;

namespace RoboRuckus.RuckusCode
{
    public class Robot
    {
        public int x_pos = -1;
        public int y_pos = -1;
        // There shouldn't be more that 256 robots in the game.
        public byte robotNum;

        /// <summary>
        /// The axis/direction along wich the bot is currently facing
        /// </summary>
        public orientation currentDirection = orientation.Y;
        public IPAddress robotAddress;
        // TODO: Implement robot names
        public string robotName;
        public player controllingPlayer = null;
        public EventWaitHandle moving = new ManualResetEvent(false);

        public enum orientation
        {
            X = 0,
            Y = 1,
            NEG_X = 2,
            NEG_Y = 3
        }

        // There shouldn't be more thant 256 points of damage a bot can take
        private  byte _damage = 0;
        // Only one thread should be issuing orders and/or modifying damage at a time
        public byte damage
        {
            get
            {
                return _damage;
            }
            set
            {
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
                        if (value >= 10)
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
                                        card = playerSignals.Instance.drawCard();
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