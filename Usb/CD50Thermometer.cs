using System.IO.Ports;

namespace FellrnrHeatMonitor.Usb;

/// <summary>
/// Driver for the CD50 / Landtek 4-channel USB thermometer. The device speaks a
/// small binary serial protocol at 9600 baud.
/// </summary>
internal sealed class CD50Thermometer : IDisposable
{
    private readonly string _port;
    private readonly byte[] _host2DeviceHeader = { 0xAA, 0x55 };
    private readonly byte[] _device2HostHeader = { 0x55, 0xAA };
    private readonly byte[] _connectCommand = { 0x00, 0x03, 0x02 };
    private readonly byte[] _readCommand = { 0x01, 0x03, 0x03 };
    private SerialPort? _serialPort;

    public CD50Thermometer(string port = "COM7")
    {
        _port = port;
    }

    public bool Connected { get; private set; }

    public static string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames().OrderBy(p => p).ToArray();
    }

    public static string? FindAvailablePort()
    {
        foreach (var port in GetAvailablePorts())
        {
            try
            {
                using var thermometer = new CD50Thermometer(port);
                if (thermometer.Connect())
                {
                    return port;
                }
            }
            catch
            {
                // Keep scanning other COM ports.
            }
        }

        return null;
    }

    public bool Connect()
    {
        try
        {
            _serialPort = new SerialPort(_port, 9600, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            _serialPort.Open();
            var initMessage = BuildMessage(_connectCommand);
            _serialPort.Write(initMessage, 0, initMessage.Length);

            var response = new byte[9];
            var bytesRead = ReadBytes(9, response);
            if (bytesRead == 9 && response[0] == _device2HostHeader[0] && response[1] == _device2HostHeader[1] && VerifyChecksum(response, 8))
            {
                Connected = true;
                return true;
            }

            Disconnect();
            throw new InvalidOperationException($"Invalid connection response from {_port}; {bytesRead} bytes received.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            Connected = false;
            Disconnect();
            throw new InvalidOperationException($"Could not connect to {_port}: {ex.Message}", ex);
        }
    }

    public double[]? ReadTemperatures()
    {
        if (!Connected || _serialPort is null)
        {
            throw new InvalidOperationException("Not connected to the CD50 thermometer.");
        }

        try
        {
            var readMessage = BuildMessage(_readCommand);
            _serialPort.Write(readMessage, 0, readMessage.Length);

            var response = new byte[13];
            var bytesRead = ReadBytes(13, response);
            if (bytesRead != 13 || response[0] != _device2HostHeader[0] || response[1] != _device2HostHeader[1])
            {
                return null;
            }

            if (!VerifyChecksum(response, 12))
            {
                return null;
            }

            var temperatures = new double[4];
            for (var i = 0; i < 4; i++)
            {
                var tempRaw = (ushort)(response[4 + i * 2] | (response[5 + i * 2] << 8));
                temperatures[i] = tempRaw / 10.0;
            }

            return temperatures;
        }
        catch (TimeoutException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    public void Disconnect()
    {
        if (_serialPort is not null)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }
            finally
            {
                _serialPort.Dispose();
                _serialPort = null;
                Connected = false;
            }
        }
        else
        {
            Connected = false;
        }
    }

    public void Dispose()
    {
        Disconnect();
    }

    private byte[] BuildMessage(byte[] command)
    {
        var message = new byte[_host2DeviceHeader.Length + command.Length];
        Buffer.BlockCopy(_host2DeviceHeader, 0, message, 0, _host2DeviceHeader.Length);
        Buffer.BlockCopy(command, 0, message, _host2DeviceHeader.Length, command.Length);
        return message;
    }

    private int ReadBytes(int size, byte[] response)
    {
        if (_serialPort is null)
        {
            throw new InvalidOperationException("Serial port is not initialized.");
        }

        var bytesRead = 0;
        while (bytesRead < size)
        {
            var remaining = size - bytesRead;
            bytesRead += _serialPort.Read(response, bytesRead, remaining);
        }

        return bytesRead;
    }

    private static bool VerifyChecksum(byte[] response, int checksumOffset)
    {
        byte checksum = 0;
        for (var i = 0; i < checksumOffset; i++)
        {
            checksum = (byte)((checksum + response[i]) & 0xFF);
        }

        return checksum == response[checksumOffset];
    }
}
