using Microsoft.AspNetCore.SignalR;
using RoboRuckus.RuckusCode;

namespace RoboRuckus.Hubs
{
    public class playerHub : Hub
    {
        /// <summary>
        /// Constructs the player hub
        /// </summary>
        public playerHub(IHubContext<playerHub> context)
        {
            // Create instance of player signal class passing hub context (there's probably a better way to use DI)
            serviceHelpers.signals = new playerSignals(context);
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
                byte[] cards = serviceHelpers.signals.dealPlayer(caller);
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

                Clients.Caller.SendAsync("deal", dealtCards, locked);
            }
        }

        /// <summary>
        /// Let's a player request their health
        /// </summary>
        /// <param name="playerNum"></param>
        /// <returns>The damage the robot has</returns>
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
                serviceHelpers.signals.submitMove(sender, cards);
            }
        }
    }
}