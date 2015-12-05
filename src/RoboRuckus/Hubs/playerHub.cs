using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Hubs;
using RoboRuckus.RuckusCode;

namespace RoboRuckus.Hubs
{
    [HubName("playerHub")]
    public class playerHub : Hub
    {
        // The shared player signals instance
        private readonly playerSignals _signals;

        /// <summary>
        /// Constructs the player hub
        /// </summary>
        /// <param name="manager">The player hub connection manager</param>
        public playerHub(IConnectionManager manager)
        {
            // Make sure the player hub context is avaiable when needed.
            serviceHelpers.playerHubContext = manager.GetHubContext<playerHub>();
            // Create or get the playerSignals instance
            _signals = playerSignals.Instance;
        }

        /// <summary>
        /// Lets a player client request a hand of cards to be dealt to them
        /// </summary>
        /// <param name="playerNum">The player number making the request</param>
        public void dealMe(int playerNum)
        {
            if (!gameStatus.players[playerNum - 1].dead)
            {
                Player caller = gameStatus.players[playerNum - 1];

                // Build the locked cards string
                bool first = true;
                string locked = "[";
                foreach (byte card in caller.lockedCards)
                {
                    string currentCard = gameStatus.movementCards[card];
                    if (!first)
                    {
                        locked += ",";
                    }
                    first = false;
                    locked += currentCard.Insert(currentCard.LastIndexOf("}") - 1, ",\"cardNumber\": " + card.ToString());
                }
                locked += "]";

                // Build the dealt cards string
                first = true;
                byte[] cards = _signals.dealPlayer(caller);
                string dealtCards = "[";
                foreach (byte card in cards)
                {
                    string currentCard = gameStatus.movementCards[card];
                    if (!first)
                    {
                        dealtCards += ",";
                    }
                    first = false;
                    dealtCards += currentCard.Insert(currentCard.LastIndexOf("}") - 1, ",\"cardNumber\": " + card.ToString());
                }
                dealtCards += "]";

                Clients.Caller.deal(dealtCards, locked);
            }
        }

        /// <summary>
        /// Let's a player reques their health
        /// </summary>
        /// <param name="playerNum"></param>
        /// <returns>The damae the robot has</returns>
        public int getHealth(int playerNum)
        {
            return gameStatus.players[playerNum - 1].playerRobot.damage;
        }

        /// <summary>
        /// Allows a player client to send the selected cards to the server
        /// </summary>
        /// <param name="playerNum">The player number sending the cards</param>
        /// <param name="cards">The cards being sent</param>
        public void sendCards(int playerNum, cardModel[] cards, bool shutdown)
        {
            Player sender = gameStatus.players[playerNum - 1];
            if (!sender.dead)
            {
                sender.willShutdown = shutdown;
                _signals.submitMove(sender, cards);
            }      
        }
    }

    /// <summary>
    /// A convenient way to organize card data
    /// </summary>
    public class cardModel
    {
        public string direction;
        public int priority;
        public int magnitude;
        public byte cardNumber;

        /// <summary>
        /// A string representaion of a card that matches
        /// the one generated and sent to the player cleints.
        /// </summary>
        /// <returns>The string representation of a card</returns>
        public override string ToString()
        {
            return "{ \"direction\": \"" + direction + "\", \"priority\": " + priority.ToString() + ", \"magnitude\": " + magnitude.ToString() + ", \"cardNumber\": " + cardNumber.ToString() + "}";
        }
    }
}