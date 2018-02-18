using System.Collections.Generic;

namespace RoboRuckus
{
    public interface IWifiScanner
    {
        bool done { get; }
        List<string> WiFiNetworks { get; }

        void getNetworks();
    }
}
