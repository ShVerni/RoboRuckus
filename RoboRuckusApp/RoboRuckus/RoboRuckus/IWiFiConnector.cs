using System;
using System.Collections.Generic;
using System.Text;

namespace RoboRuckus
{
    public interface IWifiConnector
    {
        bool connect(string ssid, string psk);

        bool remove(string ssid);
    }
}
