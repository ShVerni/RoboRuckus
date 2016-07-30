using Microsoft.AspNetCore.SignalR;

namespace RoboRuckus.RuckusCode
{
    /// <summary>
    /// A group of static enviromental interfaces for reference from other parts of code
    /// </summary>
    public static class serviceHelpers
    {
        private static volatile IHubContext _playerHubContext = null;
        private static volatile string _rootPath;

        /// <summary>
        /// The player hub context
        /// </summary>
        public static IHubContext playerHubContext
        {
            get
            {
                return _playerHubContext;
            }
            set
            {
                if (_playerHubContext == null)
                {
                    _playerHubContext = value;
                }
            }
        }

        /// <summary>
        /// The application's root path
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