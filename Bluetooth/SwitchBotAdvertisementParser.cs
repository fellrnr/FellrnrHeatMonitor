using FellrnrHeatMonitor.Services;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace FellrnrHeatMonitor.Bluetooth;

internal static class SwitchBotAdvertisementParser
{
    private const ushort OldServiceUuid = 0x000d;
    private const ushort NewServiceUuid = 0xfd3d;

    public static bool TryParse(BluetoothLEAdvertisementReceivedEventArgs args, out SwitchBotMeterAdvertisement? result)
    {
        result = null;
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            MacAddressHelper.FormatBluetoothAddress(args.BluetoothAddress)
        };

        ParsedValues? parsedValues = null;
        int? outdoorBattery = null;

        foreach (var manufacturer in args.Advertisement.ManufacturerData)
        {
            var payloadOnly = ToArray(manufacturer.Data);
            if (payloadOnly.Length >= 6)
            {
                AddCandidate(candidates, MacAddressHelper.FormatBytes(payloadOnly, 0, reverse: false));
                AddCandidate(candidates, MacAddressHelper.FormatBytes(payloadOnly, 0, reverse: true));
            }

            var payloadWithCompany = new byte[payloadOnly.Length + 2];
            payloadWithCompany[0] = (byte)(manufacturer.CompanyId & 0xff);
            payloadWithCompany[1] = (byte)(manufacturer.CompanyId >> 8);
            System.Buffer.BlockCopy(payloadOnly, 0, payloadWithCompany, 2, payloadOnly.Length);
            if (TryParseOutdoorManufacturerData(payloadWithCompany, out var outdoorValues))
            {
                parsedValues = outdoorValues;
            }
        }

        foreach (var dataSection in args.Advertisement.DataSections)
        {
            if (dataSection.DataType != 0x16)
            {
                continue;
            }

            var data = ToArray(dataSection.Data);
            if (TryParseMeterServiceData(data, out var values))
            {
                parsedValues = values;
            }

            outdoorBattery ??= TryParseOutdoorBattery(data);
        }

        if (parsedValues is null)
        {
            return false;
        }

        result = new SwitchBotMeterAdvertisement
        {
            TemperatureC = parsedValues.TemperatureC,
            HumidityPercent = parsedValues.HumidityPercent,
            BatteryPercent = parsedValues.BatteryPercent ?? outdoorBattery,
            Rssi = args.RawSignalStrengthInDBm,
            Source = parsedValues.Source
        };
        result.MacCandidates.AddRange(candidates.Where(c => MacAddressHelper.TryNormalize(c, out _)));
        return true;
    }

    private static void AddCandidate(HashSet<string> candidates, string candidate)
    {
        if (MacAddressHelper.TryNormalize(candidate, out _))
        {
            candidates.Add(candidate);
        }
    }

    private static bool TryParseMeterServiceData(byte[] data, out ParsedValues? values)
    {
        values = null;
        foreach (var offset in GetPossibleServicePayloadOffsets(data).Distinct())
        {
            if (offset < 0 || offset >= data.Length)
            {
                continue;
            }

            if (TryParseMeterPayload(data.AsSpan(offset), out values))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<int> GetPossibleServicePayloadOffsets(byte[] data)
    {
        if (data.Length >= 8)
        {
            var uuidLittleEndian = (ushort)(data[0] | (data[1] << 8));
            var uuidBigEndian = (ushort)((data[0] << 8) | data[1]);
            if (IsKnownSwitchBotServiceUuid(uuidLittleEndian) || IsKnownSwitchBotServiceUuid(uuidBigEndian))
            {
                yield return 2;
            }
        }

        if (data.Length >= 6)
        {
            yield return 0;
        }
    }

    private static bool TryParseMeterPayload(ReadOnlySpan<byte> payload, out ParsedValues? values)
    {
        values = null;
        if (payload.Length < 6)
        {
            return false;
        }

        var deviceType = payload[0] & 0x7f;
        if (deviceType is not (0x54 or 0x69 or 0x74 or 0x77))
        {
            return false;
        }

        var decimalPart = payload[3] & 0x0f;
        var integerPart = payload[4] & 0x7f;
        var humidity = payload[5] & 0x7f;
        if (decimalPart > 9 || humidity > 100)
        {
            return false;
        }

        var sign = (payload[4] & 0x80) != 0 ? 1.0 : -1.0;
        var temperature = sign * (integerPart + decimalPart / 10.0);
        var isFahrenheitMode = (payload[5] & 0x80) != 0;
        if (isFahrenheitMode)
        {
            temperature = (temperature - 32.0) * 5.0 / 9.0;
        }

        var battery = payload[2] & 0x7f;
        values = new ParsedValues
        {
            TemperatureC = temperature,
            HumidityPercent = humidity,
            BatteryPercent = battery <= 100 ? battery : null,
            Source = "service-data"
        };
        return true;
    }

    private static bool TryParseOutdoorManufacturerData(byte[] dataWithCompanyId, out ParsedValues? values)
    {
        values = null;
        if (dataWithCompanyId.Length < 13)
        {
            return false;
        }

        var companyId = (ushort)(dataWithCompanyId[0] | (dataWithCompanyId[1] << 8));
        if (companyId is not (0x0969 or 0x0059))
        {
            return false;
        }

        var temperature = ((dataWithCompanyId[10] & 0x0f) * 0.1 + (dataWithCompanyId[11] & 0x7f)) *
                          ((dataWithCompanyId[11] & 0x80) != 0 ? 1.0 : -1.0);
        var humidity = dataWithCompanyId[12] & 0x7f;
        if (temperature < -50 || temperature > 100 || humidity > 100)
        {
            return false;
        }

        values = new ParsedValues
        {
            TemperatureC = temperature,
            HumidityPercent = humidity,
            BatteryPercent = null,
            Source = "manufacturer-data-outdoor"
        };
        return true;
    }

    private static int? TryParseOutdoorBattery(byte[] data)
    {
        if (data.Length < 5)
        {
            return null;
        }

        var uuidLittleEndian = (ushort)(data[0] | (data[1] << 8));
        var uuidBigEndian = (ushort)((data[0] << 8) | data[1]);
        if (!IsKnownSwitchBotServiceUuid(uuidLittleEndian) && !IsKnownSwitchBotServiceUuid(uuidBigEndian))
        {
            return null;
        }

        var last = data[^1] & 0x7f;
        return last <= 100 ? last : null;
    }

    private static bool IsKnownSwitchBotServiceUuid(ushort uuid)
    {
        return uuid == OldServiceUuid || uuid == NewServiceUuid;
    }

    private static byte[] ToArray(IBuffer buffer)
    {
        if (buffer.Length == 0)
        {
            return Array.Empty<byte>();
        }

        var data = new byte[(int)buffer.Length];
        using var reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(data);
        return data;
    }

    private sealed class ParsedValues
    {
        public double TemperatureC { get; init; }
        public int HumidityPercent { get; init; }
        public int? BatteryPercent { get; init; }
        public string Source { get; init; } = string.Empty;
    }
}
