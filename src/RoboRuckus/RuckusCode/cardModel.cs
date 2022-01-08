namespace RoboRuckus.RuckusCode
{
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
        /// A string representation of a card that matches
        /// the one generated and sent to the player clients.
        /// </summary>
        /// <returns>The string representation of a card</returns>
        public override string ToString()
        {
            return "{ \"direction\": \"" + direction + "\", \"priority\": " + priority.ToString() + ", \"magnitude\": " + magnitude.ToString() + ", \"cardNumber\": " + cardNumber.ToString() + "}";
        }
    }
}
