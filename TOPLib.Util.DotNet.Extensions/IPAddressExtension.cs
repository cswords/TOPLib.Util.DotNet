using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TOPLib.Util.DotNet.Extensions
{
    public static class IPAddressExtension
    {

        public static bool IsRemote(this IPAddress address)
        {
            return !address.IsLoopback() & !address.IsLanIP();
        }

        public static bool IsLoopback(this IPAddress address)
        {
            return IPAddress.IsLoopback(address);
        }

        public static bool IsLanIP(this IPAddress ipaddress)
        {
            if (ipaddress.AddressFamily == AddressFamily.InterNetwork)
            {
                String[] straryIPAddress = ipaddress.ToString().Split(new String[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                int[] iaryIPAddress = new int[] { int.Parse(straryIPAddress[0]), int.Parse(straryIPAddress[1]), int.Parse(straryIPAddress[2]), int.Parse(straryIPAddress[3]) };
                if (
                    iaryIPAddress[0] == 10
                    || (iaryIPAddress[0] == 192 && iaryIPAddress[1] == 168)
                    || (iaryIPAddress[0] == 172 && (iaryIPAddress[1] >= 16 && iaryIPAddress[1] <= 31))
                    )
                {
                    return true;
                }
                else
                {
                    // IP Address is "probably" public. This doesn't catch some VPN ranges like OpenVPN and Hamachi.
                    return false;
                }
            }
            else if (ipaddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return true;
            }
            else
                return false;
        }

        public static bool IsLocalLanIP(this IPAddress address)
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var iface in interfaces)
            {
                var properties = iface.GetIPProperties();
                foreach (var ifAddr in properties.UnicastAddresses)
                {
                    if (ifAddr.IPv4Mask != null
                        && ifAddr.Address.AddressFamily == AddressFamily.InterNetwork
                        && ifAddr.Address.CheckMask(ifAddr.IPv4Mask, address)
                        )
                        return true;
                }
            }
            return false;
        }

        public static bool IsLanIP(this IPAddress address, out IPAddress serverAddress)
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var iface in interfaces)
            {
                var properties = iface.GetIPProperties();
                foreach (var ifAddr in properties.UnicastAddresses)
                {
                    if (ifAddr.IPv4Mask != null &&
                        ifAddr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        ifAddr.Address.CheckMask(ifAddr.IPv4Mask, address))
                    {
                        serverAddress = ifAddr.Address;
                        return true;
                    }
                }
            }
            serverAddress = null;
            return false;
        }

        public static bool CheckMask(this IPAddress address, IPAddress mask, IPAddress target)
        {
            if (mask == null)
                return false;

            var ba = address.GetAddressBytes();
            var bm = mask.GetAddressBytes();
            var bb = target.GetAddressBytes();

            if (ba.Length != bm.Length || bm.Length != bb.Length)
                return false;

            for (var i = 0; i < ba.Length; i++)
            {
                int m = bm[i];

                int a = ba[i] & m;
                int b = bb[i] & m;

                if (a != b)
                    return false;
            }

            return true;
        }

    }
}
