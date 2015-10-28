using System.Collections.Generic;

namespace RoboRuckus.RuckusCode
{
    public class player
    {
        public byte playerNumber;
        public robot playerRobot;
        public byte[] cards = null;
        public Hubs.cardModel[] move = null;
        public List<byte> lockedCards = new List<byte>();

        public bool shutdown = false;
        public bool willShutdown = false;


        private bool _dead;
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

        public player(byte Number, robot Robot)
        {
            playerNumber = Number;
            playerRobot = Robot;
        }
    }
}