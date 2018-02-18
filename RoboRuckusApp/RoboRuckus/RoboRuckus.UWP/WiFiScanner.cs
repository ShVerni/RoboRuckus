using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Windows.Devices.WiFi;
using Windows.Devices.Enumeration;
using RoboRuckus.UWP;

[assembly: Dependency(typeof(WiFiScanner))]
namespace RoboRuckus.UWP
{
    class WiFiScanner : IWifiScanner
    {
        public bool done
        {
            get
            {
                return _done;
            }
        }
        public List<string> WiFiNetworks
        {
            get
            {
                return _WiFiNetworks;
            }
        }

        private bool _done = false;
        private List<string> _WiFiNetworks = new List<string>();
        private WiFiAdapter _WiFiAdapter;

        public void getNetworks()
        {
            bool result = Task.Run(() => _getNetworks()).Result;
            _done = true;
        }

        private async Task<bool> _getNetworks()
        {
            WiFiAccessStatus access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                _done = true;
                return false;
            }
            else
            {
                DeviceInformationCollection adapterResults = await DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
                if (adapterResults.Count() >= 1)
                {
                    _WiFiAdapter = await WiFiAdapter.FromIdAsync(adapterResults[0].Id);
                }
                else
                {
                    _done = true;
                    return false;
                }
            }
            await _WiFiAdapter.ScanAsync();
            foreach (WiFiAvailableNetwork availableNetwork in _WiFiAdapter.NetworkReport.AvailableNetworks)
            {
                _WiFiNetworks.Add(availableNetwork.Ssid);
            }
            return true;
        }

    }
}
