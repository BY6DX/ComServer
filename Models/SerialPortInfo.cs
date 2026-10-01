namespace ComServer.Models;

/// <summary>一个本机串口及其硬件描述。</summary>
/// <param name="PortName">串口名，例如 <c>COM3</c>。</param>
/// <param name="Description">硬件描述，例如 <c>USB-SERIAL CH340</c>；无法获取时为 <c>null</c>。</param>
public readonly record struct SerialPortInfo(string PortName, string? Description)
{
    /// <summary>下拉框中显示的文本：串口名 + 硬件名。</summary>
    public string Display => string.IsNullOrWhiteSpace(Description) ? PortName : $"{PortName} — {Description}";

    public override string ToString() => Display;
}
