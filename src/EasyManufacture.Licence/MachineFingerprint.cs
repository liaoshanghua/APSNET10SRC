using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace EasyManufacture.Licence;

/// <summary>读取 CPU / MAC（WMI 反射调用；WMI 不可用时 NetworkInterface 兜底 MAC）。</summary>
internal static class MachineFingerprint
{
    public static string GetCpuId() =>
        WmiHardwareProbe.QueryFirst("Win32_Processor", "ProcessorId") ?? string.Empty;

    public static string GetMacAddress()
    {
        var mac = WmiHardwareProbe.QueryFirstWql(
            "SELECT MacAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=True AND MacAddress IS NOT NULL",
            "MacAddress");
        if (!string.IsNullOrEmpty(mac))
            return mac;

        return GetMacFromNetworkInterfaces();
    }

    /// <summary>格式与 WMI 一致：AA:BB:CC:DD:EE:FF</summary>
    private static string GetMacFromNetworkInterfaces()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;

            var bytes = ni.GetPhysicalAddress().GetAddressBytes();
            if (bytes.Length == 0 || bytes.All(b => b == 0))
                continue;

            var desc = ni.Description ?? "";
            if (desc.Contains("Virtual", StringComparison.OrdinalIgnoreCase)
                || desc.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase)
                || desc.Contains("VMware", StringComparison.OrdinalIgnoreCase)
                || desc.Contains("VPN", StringComparison.OrdinalIgnoreCase))
                continue;

            if (ni.GetIPProperties().UnicastAddresses.Any(a => a.Address.AddressFamily == AddressFamily.InterNetwork))
                return FormatMac(bytes);
        }

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up
                || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            var bytes = ni.GetPhysicalAddress().GetAddressBytes();
            if (bytes.Length > 0 && !bytes.All(b => b == 0))
                return FormatMac(bytes);
        }

        return string.Empty;
    }

    private static string FormatMac(byte[] bytes) =>
        string.Join(":", bytes.Select(b => b.ToString("X2")));

    public static string GetDiskId() =>
        WmiHardwareProbe.QueryFirst("Win32_DiskDrive", "Model") ?? string.Empty;

    public static string GetIpAddress()
    {
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork
                        && !System.Net.IPAddress.IsLoopback(ua.Address))
                        return ua.Address.ToString();
                }
            }
        }
        catch { }

        return "unknown";
    }

    public static string GetSystemType() =>
        WmiHardwareProbe.QueryFirst("Win32_ComputerSystem", "SystemType")
        ?? RuntimeInformation.OSArchitecture.ToString();

    public static string GetTotalPhysicalMemory()
    {
        var mem = WmiHardwareProbe.QueryFirst("Win32_ComputerSystem", "TotalPhysicalMemory");
        if (long.TryParse(mem, out var bytes) && bytes > 0)
            return (bytes / (1024.0 * 1024 * 1024)).ToString("G2") + "GB";
        return mem ?? string.Empty;
    }
}
