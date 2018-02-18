using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Windows.Devices.WiFi;
using Windows.Devices.Enumeration;
using Windows.Security.Credentials;
using Windows.Networking;
using RoboRuckus.UWP;

[assembly: Dependency(typeof(WifiConnector))]
namespace RoboRuckus.UWP
{
    public class WifiConnector : IWifiConnector
    {
        private WiFiAdapter _WiFiAdapter;

        public bool connect(string ssid, string psk)
        {
            bool result = Task.Run(() => _getWifiAdapter()).Result;
            return Task.Run(() => _connect(ssid, psk)).Result;
        }

        public bool remove(string ssid)
        {
            // Windows can't remove a Wi-Fi profile it seems
            return false;
        }

        private async Task<bool> _connect(string ssid, string psk)
        {
            await _WiFiAdapter.ScanAsync();
            WiFiAvailableNetwork _network = _WiFiAdapter.NetworkReport.AvailableNetworks.First(n => n.Ssid == ssid);
            PasswordCredential credential = new PasswordCredential();
            credential.Password = psk;

            Task< WiFiConnectionResult> connected = _WiFiAdapter.ConnectAsync(_network, WiFiReconnectionKind.Manual, credential).AsTask();

            WiFiConnectionResult result = null;
            if (connected != null)
            {
                result = await connected;
            }
            if (result != null && result.ConnectionStatus == WiFiConnectionStatus.Success)
            {
                return true;
            }
            else
            {
                return false;
            }                
        }

        private async Task<bool> _getWifiAdapter()
        {
            WiFiAccessStatus access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                throw new Exception("WiFiAccessStatus not allowed.");
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
                    throw new Exception("WiFi adapter not found.");
                }
            }
            return true;
        }
    }
}
