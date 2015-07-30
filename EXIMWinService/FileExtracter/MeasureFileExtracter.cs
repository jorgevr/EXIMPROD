using System;
using EXIMWinService.Model;
using EXIMWinService.WebApi;
using System.Globalization;
using EXIMWinService.Configuration;
using EXIMWinService.Services;
using System.Text.RegularExpressions;
using log4net;
using System.Collections.Generic;


namespace EXIMWinService.FileExtracter
{
    public interface IMeasureFileExtracter
    {
        Measure ProcessLine(DateTime ProdDate, double measure, ApiPlant plant);
        String getPlantName(string filePath);
    }

    public class MeasureFileExtracter : IMeasureFileExtracter
    {
        private const char LINE_SEPARATOR = ' ';
        private CultureInfo CULTURE = new CultureInfo("en-US");

        private const int LINE_LOCALDATE_POSITION = 0;
        private const int LINE_VALUE_POSITION = 3;
        private static IConfigurationProvider _confProvider;

        public MeasureFileExtracter(IConfigurationProvider configProvider, IPlantService plantService)
        {
            _confProvider = configProvider;
        }
  

        //public Measure ProcessLine(string line, string fileName, ApiPlant plant)
        public Measure ProcessLine(DateTime ProdDate, double measure, ApiPlant plant)
        {
            var source = _confProvider.GetMeasureSourceFor1HResolution();
            var datavariable = _confProvider.GetDataVariable();
            var resolution = _confProvider.GetResolution();
            var utcDateTime = extractUtcDateFromFileLine(ProdDate, plant.TimeZone);
            var utcDate = string.Format("{0:yyyyMMdd}", utcDateTime.Date);
            var utcHour = utcDateTime.Hour;
            var utcMinute = utcDateTime.Minute;
            var utcSecond = utcDateTime.Second;
            // convert from MW to KW
            var multiplier = _confProvider.GetMeasureValueMultiplier();
            var value = measure * multiplier;
            var percentage = 1;
            var reliability = 0;

            return new Measure(plant.Id, source, datavariable, utcDate, utcHour, utcMinute, utcSecond, value, percentage, reliability, resolution);
        }

        public string getPlantName(string filePath)
        {
            //string fName = filePath.Split('/')[1];
            string fName = filePath;

            string pat = "PotentialEA-";

            string sInput = Regex.Replace(fName, pat, "");
            int index2 = sInput.IndexOf("_");
            string Output = getPlantIdfromEXIMPlantName(sInput.Substring(0, index2));

            return Output;
        }

        private string getPlantIdfromEXIMPlantName(String input)
        {
            switch (input)
            {
                case "Pestera":
                    return "PEENEX01";

                default:
                    return "unknown";
            }
        }

        private DateTime extractUtcDateFromFileLine(DateTime prodDate, string plantTimeZone)
        {

            // jvr modify as to offset 3 hours
            var EXIMToGnarumOffset = _confProvider.GetEXIMToGnarumOffsetHours();
            var localDateTime = prodDate.AddHours(EXIMToGnarumOffset);
            DateTime gnarumTime = localDateTime.AddHours(-(TimeZoneInfo.FindSystemTimeZoneById(plantTimeZone).BaseUtcOffset.Hours));
            //DateTime gnarumTime = TimeZoneInfo.ConvertTime(localDateTime, timeZone, TimeZoneInfo.Utc);

            //var localDateTimer = TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZoneInfo.FindSystemTimeZoneById(plantTimeZone));
            //var localDateTimer = localDateTime.AddHours(-2);
            //var localDateTimer = TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZoneInfo.FindSystemTimeZoneById(plantTimeZone));

            //return localDateTimer;
            return gnarumTime;

            // return TimeZoneInfo.ConvertTimeToUtc(localDateTime.AddHours(ENEXToGnarumOffset), TimeZoneInfo.FindSystemTimeZoneById(plantTimeZone));
        }

    }
}
