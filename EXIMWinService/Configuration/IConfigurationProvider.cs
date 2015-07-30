using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EXIMWinService.Configuration
{
    public interface IConfigurationProvider
    {
        void Refresh();
        string GetMeasuresLoaderCronExpression();
        string GetDirectoryPathToWatch();
        string GetConnnectionString();
        string GetFilenamePattern();
        string GetFtpRegExpression();
        string GetFtpAddress();
        string GetFtpUsername();
        string GetFtpPassword();
        string GetFtpRemotePath();
        string GetFtpProcessedFilesPath();
        string GetPlantName();
        string GetProcessedFileTextToAppend();
        string GetWebApiPlantBaseUri();
        string GetWebApiMeasurePutUri();
        string GetWebApiPlantPowerUri();
        double GetEXIMToGnarumOffsetHours();
        string GetMeasureSourceFor1HResolution();
        double GetMeasureValueMultiplier();
        string GetDataVariable();
        string GetHourlyCronExpression();
        char GetHourlyPlantsSeparator();
        string GetHourlyPlantsString();
        string GetHourlyDataVariable();
        string GetResolution();
        string GetProcessMeasuresFilesListPath();
        int GetHourlyNumberOfHoursToProcess();

    }
}
