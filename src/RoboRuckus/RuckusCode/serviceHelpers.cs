namespace RoboRuckus.RuckusCode
{
    /// <summary>
    /// A group of static environmental interfaces for reference from other parts of code.
    /// </summary>
    public static class serviceHelpers
    {
        private static volatile string _rootPath;
        private static volatile playerSignals _signals;
        
        /// <summary>
        /// Name of the game log file.
        /// </summary>
        public static string logfile = "gamelog.txt";

        /// <summary>
        /// Used to enable logging of game state and player moves.
        /// </summary>
        public static bool logging = false;

        /// <summary>
        /// A single instance of the player signals class.
        /// </summary>
        public static playerSignals signals
        {
            get
            {
                return _signals;
            }
            set
            {
                if (_signals == null)
                {
                    _signals = value;
                }
            }
        }

        /// <summary>
        /// The application's root path.
        /// </summary>
        public static string rootPath
        {
            get
            {
                return _rootPath;
            }
            set
            {
                if (_rootPath == null)
                {
                    _rootPath = value;
                }
            }
        }
    }
}