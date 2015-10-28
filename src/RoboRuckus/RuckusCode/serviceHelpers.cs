using Microsoft.AspNet.SignalR;
using Microsoft.Dnx.Runtime;

namespace RoboRuckus.RuckusCode
{
    /// <summary>
    /// A group of static enviromental interfaces for reference from other parts of code
    /// </summary>
    public static class serviceHelpers
    {
        private static volatile IHubContext _playerHubContext = null;
        private static volatile IApplicationEnvironment _appEnviroment = null;

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
        /// The application enviroment
        /// </summary>
        public static IApplicationEnvironment appEnviroment
        {
            get
            {
                return _appEnviroment;
            }
            set
            {
                if (_appEnviroment == null)
                {
                    _appEnviroment = value;
                }
            }
        }
    }
}