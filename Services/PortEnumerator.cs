using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using ComServer.Models;

namespace ComServer.Services;

/// <summary>
/// 枚举本机串口，并尽量附上硬件名称（例如 “USB-SERIAL CH340”）。
/// 硬件名通过系统自带的 SetupAPI 读取，不需要引入 <c>System.Management</c> 依赖。
/// </summary>
public static class PortEnumerator
{
    private const uint DigcfPresent = 0x02;
    private const uint SpdrpFriendlyName = 0x0C;

    /// <summary>端口类设备的类 GUID（GUID_DEVCLASS_PORTS），即设备管理器里的“端口 (COM 和 LPT)”。</summary>
    private static readonly Guid PortsClassGuid = new("4d36e978-e325-11ce-bfc1-08002be10318");

    private static readonly IntPtr InvalidHandle = new(-1);

    /// <summary>
    /// 返回串口列表，按自然序排列（COM2 排在 COM10 前面）。
    /// 任何一步失败都退化为“只有串口名”，不会抛异常导致界面无法启动。
    /// </summary>
    public static IReadOnlyList<SerialPortInfo> GetPorts()
    {
        var names = GetPortNames();
        if (names.Count == 0)
        {
            return [];
        }

        var descriptions = TryGetDescriptions();

        return [.. names.Select(name => new SerialPortInfo(
            name,
            descriptions.TryGetValue(name, out var description) ? description : null))];
    }

    private static List<string> GetPortNames()
    {
        string[] raw;
        try
        {
            raw = SerialPort.GetPortNames();
        }
        catch
        {
            return [];
        }

        return [.. raw
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(NumericSuffix)
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>读取 “COM3 → USB-SERIAL CH340” 的映射；失败时返回空表。</summary>
    private static Dictionary<string, string> TryGetDescriptions()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 按“端口”类 GUID 枚举当前在位的设备（传枚举器名需要额外带 DIGCF_ALLCLASSES，否则会直接失败）。
        var classGuid = PortsClassGuid;
        var deviceSet = SetupDiGetClassDevs(ref classGuid, null, IntPtr.Zero, DigcfPresent);
        if (deviceSet == InvalidHandle)
        {
            return map;
        }

        try
        {
            var info = new SpDevInfoData { cbSize = Marshal.SizeOf<SpDevInfoData>() };
            var buffer = new byte[1024];

            for (uint index = 0; SetupDiEnumDeviceInfo(deviceSet, index, ref info); index++)
            {
                if (!SetupDiGetDeviceRegistryProperty(deviceSet, ref info, SpdrpFriendlyName, out _, buffer, (uint)buffer.Length, out var size)
                    || size < 4)
                {
                    continue;
                }

                // 形如 "USB-SERIAL CH340 (COM3)"。
                var friendly = Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0');
                var open = friendly.LastIndexOf('(');
                var close = friendly.LastIndexOf(')');

                if (open < 0 || close <= open)
                {
                    continue;
                }

                var portName = friendly[(open + 1)..close].Trim();
                var description = friendly[..open].Trim();

                if (portName.Length > 0)
                {
                    map[portName] = description.Length > 0 ? description : portName;
                }
            }
        }
        catch
        {
            // 忽略：退化为只显示串口名。
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(deviceSet);
        }

        return map;
    }

    private static int NumericSuffix(string name)
    {
        var start = name.Length;
        while (start > 0 && char.IsDigit(name[start - 1]))
        {
            start--;
        }

        return start < name.Length && int.TryParse(name.AsSpan(start), out var value)
            ? value
            : int.MaxValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SpDevInfoData
    {
        public int cbSize;
        public Guid ClassGuid;
        public int DevInst;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, string? enumerator, IntPtr hwndParent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, uint memberIndex, ref SpDevInfoData deviceInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        byte[]? propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);
}
