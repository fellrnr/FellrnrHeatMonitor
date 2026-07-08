namespace FellrnrHeatMonitor.Services;

public sealed class FellrnrHeatCalculator
{
    public enum SodiumLosses
    {
        None,
        Adapted,
        Unadapted
    }

    public string SodiumMessage { get; }
    public string ResultMessage { get; }
    public string TempColor { get; }
    public double FeelsLikeTemp { get; }

    private double _neilsonScalingOffset = 20.0;
    private double _neilsonScalingFactor = 15.0 / 35.0;

    public FellrnrHeatCalculator(
        double airTemp,
        double relHumidity,
        double airVelocity,
        double cyclingPower,
        double heightCM,
        double weightKg,
        string tempUnits,
        SodiumLosses sodiumLosses)
    {
        TempColor = "#000000";
        var convectionRadiationHeatLost = ConvectionRadiationHeatLost(airTemp, relHumidity, airVelocity, heightCM, weightKg);
        var maxHeatLossEvaporation = MaxHeatLossEvaporation(airTemp, relHumidity, airVelocity, heightCM, weightKg);
        var cyclingHeatProducedWatts = CyclingHeatProducedWatts(cyclingPower);
        var excessHeatLostViaSweat = cyclingHeatProducedWatts - convectionRadiationHeatLost;

        if (sodiumLosses != SodiumLosses.None)
        {
            var sweatRateLitersHour = SweatRateFromEvaporationLiter(excessHeatLostViaSweat);
            var sweatRateGramsHour = sweatRateLitersHour * 1000.0;
            var sweatRateMgHour = sweatRateGramsHour * 1000.0;
            var sweatRateMgMin = sweatRateMgHour / 60.0;
            var surfaceAreaMetersSquared = CalculateSurfaceArea(heightCM, weightKg);
            var surfaceAreaCmSquared = surfaceAreaMetersSquared * 100.0 * 100.0;
            var sweatRateMgCmMin = sweatRateMgMin / surfaceAreaCmSquared;
            double sodiumMmolLiter;
            if (sodiumLosses == SodiumLosses.Adapted)
            {
                sodiumMmolLiter = sweatRateMgCmMin * 21.093 + 11.561;
            }
            else
            {
                sodiumMmolLiter = sweatRateMgCmMin * 30.02 + 16.544;
            }

            var totalSodiumMmol = sodiumMmolLiter * sweatRateLitersHour;
            var totalSodiumGrams = totalSodiumMmol / 44.0;
            SodiumMessage = $" {Math.Round(sweatRateLitersHour, 1)} l/hr, Na {Math.Round(totalSodiumGrams, 1)}g/hr";
        }
        else
        {
            SodiumMessage = string.Empty;
        }

        if (excessHeatLostViaSweat > maxHeatLossEvaporation)
        {
            var excessWatts = excessHeatLostViaSweat - maxHeatLossEvaporation;
            var excessKjPerHour = excessWatts / 0.27777778;
            var excessKjPerMin = excessKjPerHour / 60.0;
            var heatCapacityBody = 3.5 * weightKg;
            var minutesPerDegreeCelcius = heatCapacityBody / excessKjPerMin;
            var terminalTempRiseTime = Math.Round(3 * minutesPerDegreeCelcius, 0);
            string msg;
            if (terminalTempRiseTime < 60)
            {
                msg = $"{terminalTempRiseTime} min";
            }
            else
            {
                msg = $"{Math.Round(terminalTempRiseTime / 60.0, 1)} hour";
            }

            ResultMessage = $"Too Hot, Terminal in ~{msg}";
            FeelsLikeTemp = 99.0;
            TempColor = "#FF0000";
            return;
        }

        var runningPercentMaxSweat = excessHeatLostViaSweat / maxHeatLossEvaporation * 100.0;
        var heatProducedWalking = HeatProducedWalking(weightKg);
        const double walkingHumidity = 30.0;
        var lastPercentMaxSweat = -1.0;

        for (var nextGuess = airTemp; nextGuess < 100; nextGuess++)
        {
            var nextConvectionRadiationHeatLost = ConvectionRadiationHeatLost(nextGuess, walkingHumidity, 1.5, heightCM, weightKg);
            var nextTotalExcessHeatWalking = heatProducedWalking - nextConvectionRadiationHeatLost;
            var nextMaxHeatLossEvaporation = MaxHeatLossEvaporation(nextGuess, walkingHumidity, 1.5, heightCM, weightKg, false, false);
            var nextPercentMaxSweat = nextTotalExcessHeatWalking / nextMaxHeatLossEvaporation * 100.0;

            var found = (excessHeatLostViaSweat <= 0 && nextTotalExcessHeatWalking > excessHeatLostViaSweat) ||
                        (excessHeatLostViaSweat > 0 && nextPercentMaxSweat > runningPercentMaxSweat && runningPercentMaxSweat > lastPercentMaxSweat);
            if (found)
            {
                string retval;
                if (tempUnits == "Centigrade")
                {
                    retval = $"{Math.Round(nextGuess, 1)}c";
                }
                else
                {
                    retval = $"{Math.Round(C2F(nextGuess), 1)}f {Math.Round(nextGuess, 1)}c";
                }

                if (excessHeatLostViaSweat > 0)
                {
                    retval += $" ({Math.Round(runningPercentMaxSweat, 0)}% max)";
                }
                else
                {
                    retval += $" ({Math.Round(-excessHeatLostViaSweat, 0)}w lost)";
                }

                retval += SodiumMessage;

                int red;
                int green;
                int blue;
                if (excessHeatLostViaSweat <= 0)
                {
                    red = 0;
                    green = 0;
                    blue = 255;
                }
                else
                {
                    red = (int)Math.Round(255 * runningPercentMaxSweat / 100.0);
                    green = 255 - red;
                    blue = 0;
                }

                red = Math.Clamp(red, 0, 255);
                green = Math.Clamp(green, 0, 255);
                blue = Math.Clamp(blue, 0, 255);
                TempColor = $"#{red:X2}{green:X2}{blue:X2}";
                ResultMessage = retval;
                FeelsLikeTemp = nextGuess;
                return;
            }

            lastPercentMaxSweat = nextPercentMaxSweat;
        }

        ResultMessage = "Unknown";
        FeelsLikeTemp = airTemp;
    }

    public void SetNeilsonScalingFactor(double factor)
    {
        _neilsonScalingFactor = factor;
    }

    public double CalculateSkinTemp(double airTemp, double relHumidity, double airVelocity, bool forRunning = true)
    {
        if (airTemp < 29)
        {
            return CalculateSkinTempMehnert(airTemp, relHumidity, airVelocity);
        }

        return CalculateSkinTempNeilson(airTemp);
    }

    public double CalculateSkinTempMehnert(double airTemp, double relHumidity, double airVelocity)
    {
        var vaporPressure = CalculateVaporPressure(airTemp, relHumidity);
        var radiantTemp = airTemp;
        const double rectalTemp = 37.0;
        return 7.19 + 0.064 * airTemp + 0.061 * radiantTemp + 0.198 * vaporPressure - 0.348 * airVelocity + 0.616 * rectalTemp;
    }

    public double CalculateSkinTempNeilson(double airTemp)
    {
        return _neilsonScalingOffset + airTemp * _neilsonScalingFactor;
    }

    public double CalculateSurfaceArea(double heightCM, double weightKg)
    {
        return Math.Sqrt(heightCM * weightKg / 3600.0);
    }

    public double CalculateSaturatedVaporPressure(double airTemp, bool forceNew = false)
    {
        var oldValue = 1.004205845 * (6.1121 * Math.Exp(17.502 * airTemp / (240.97 + airTemp))) / 10.0;
        var k = airTemp + 273.15;
        var newValue = (Math.Exp(77.3450 + (0.0057 * k - (7235.0 / k))) / Math.Pow(k, 8.2)) / 1000.0;
        return forceNew ? newValue : oldValue;
    }

    public double CalculateVaporPressure(double airTemp, double relHumidity, bool forceNew = false)
    {
        var saturated = CalculateSaturatedVaporPressure(airTemp, forceNew);
        return saturated * relHumidity / 100.0;
    }

    public double HeatExchangeConvection(double airTemp, double relHumidity, double airVelocity, double heightCM, double weightKg)
    {
        var skinTemp = CalculateSkinTemp(airTemp, relHumidity, airVelocity);
        var surfaceArea = CalculateSurfaceArea(heightCM, weightKg);
        return (skinTemp - airTemp) * Math.Sqrt(airVelocity) * surfaceArea * 8.3;
    }

    public double HeatTransferRadiant(double airTemp, double relHumidity, double airVelocity, double heightCM, double weightKg)
    {
        var skinTemp = CalculateSkinTemp(airTemp, relHumidity, airVelocity);
        var surfaceArea = CalculateSurfaceArea(heightCM, weightKg);
        return (skinTemp - airTemp) * surfaceArea * 5.2;
    }

    public double MaxHeatLossEvaporation(double airTemp, double relHumidity, double airVelocity, double heightCM, double weightKg, bool forceNew = false, bool forRunning = true)
    {
        var skinTemp = CalculateSkinTemp(airTemp, relHumidity, airVelocity, forRunning);
        var saturatedVaporPressure = CalculateSaturatedVaporPressure(skinTemp, forceNew);
        var vaporPressure = CalculateVaporPressure(airTemp, relHumidity, forceNew);
        var surfaceArea = CalculateSurfaceArea(heightCM, weightKg);
        return (saturatedVaporPressure - vaporPressure) * Math.Sqrt(airVelocity) * surfaceArea * 124.0;
    }

    public double TotalMaxHeatLost(double airTemp, double relHumidity, double airVelocity, double heightCM, double weightKg, bool forRunning = true)
    {
        return HeatExchangeConvection(airTemp, relHumidity, airVelocity, heightCM, weightKg) +
               HeatTransferRadiant(airTemp, relHumidity, airVelocity, heightCM, weightKg) +
               MaxHeatLossEvaporation(airTemp, relHumidity, airVelocity, heightCM, weightKg, false, forRunning);
    }

    public double ConvectionRadiationHeatLost(double airTemp, double relHumidity, double airVelocity, double heightCM, double weightKg)
    {
        return HeatExchangeConvection(airTemp, relHumidity, airVelocity, heightCM, weightKg) +
               HeatTransferRadiant(airTemp, relHumidity, airVelocity, heightCM, weightKg);
    }

    public double SweatRateFromEvaporationLiter(double lossEvaporation)
    {
        return lossEvaporation / 625;
    }

    public double F2C(double f)
    {
        return (5.0 / 9.0) * (f - 32.0);
    }

    public double C2F(double c)
    {
        return (9.0 / 5.0) * c + 32.0;
    }

    public double RunningHeatProducedWatts(double runningVelocity, double weightKg)
    {
        return runningVelocity * weightKg * 4;
    }

    public double CyclingHeatProducedWatts(double cyclingPower)
    {
        return cyclingPower * 4;
    }

    public double HeatProducedWalking(double weightKg)
    {
        return weightKg * 3.41;
    }
}
