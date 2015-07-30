using System;
using System.Collections.Generic;
using System.Linq;
using EXIMWinService.Configuration;
using EXIMWinService.FileExtracter;
using EXIMWinService.Ftp;
using EXIMWinService.Model;
using EXIMWinService.Services;
using log4net;
using Quartz;
using System.IO;
using System.Data;
using NPOI.SS.UserModel;
using System.Globalization;

namespace EXIMWinService.Quartz
{
    [DisallowConcurrentExecution]
    public class MeasuresLoaderJob : IJob
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(MeasuresLoaderJob));

        private static IConfigurationProvider _configProvider;
        private static IMeasureService _measureService;
        private static IPlantPowerService _plantPowerService;
        private static IPlantService _plantService;
        private static IMeasureFileExtracter _fileExtracter;
        private static IPlantPowerFileExtracter _filePlantPowerExtracter;
        private static IFtpClient _ftpClient;

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.Info("FileProcesserJob started");
                execute(context);
                _logger.Info("FileProcesserJob ended");
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Exception while executing FileProcesserJob: {0}\nStackTrace: \n{1}\n", e.Message, e.StackTrace);
            }
        }

        private void execute(IJobExecutionContext context)
        {
            //jvr breAKpoint
            prepareContextData(context);
            refreshConfigSettings();
            string directoryPathToWatch = _configProvider.GetDirectoryPathToWatch();
            if (!Directory.Exists(directoryPathToWatch))
            {
                string error = string.Format("Directory '{0}' not found", directoryPathToWatch);
                _logger.Fatal(error);
                return;
            }
            _logger.Info(string.Format("Directory '{0}' found", directoryPathToWatch));
            //var files = Directory.GetFiles(directoryPathToWatch, _configProvider.GetFilenamePattern()).ToList();
            var files = Directory.GetFiles(directoryPathToWatch).ToList();
            if (!files.Any())
            {
                _logger.Warn("There are no files to process.");
                return;
            }
            _logger.Info(String.Format("Checking current files"));
            processFiles(files);
            _logger.Info(String.Format("Done checking current files"));

        }

        /* private ApiPlant getPlant()
         {
             return _plantService.GetPlant(_configProvider.GetHourlyPlantsString());
         }*/

        private IList<String> getNoProcessedFtpFiles()
        {
            IList<String> noProcessedFilesList = new List<String>();
            var allFilePathsList = getAllFtpFiles();

            return allFilePathsList;
        }

        private IList<String> getProcessedMeasuresFilesList()
        {
            var processFileListPath = _configProvider.GetProcessMeasuresFilesListPath();
            return readProcessedfilesFromTxt(processFileListPath);

        }

        private IList<string> readProcessedfilesFromTxt(string processFileListPath)
        {
            List<String> fileList = new List<String>();
            using (StreamReader sr = new StreamReader(processFileListPath))
            {
                while (!sr.EndOfStream)
                {
                    string file = sr.ReadLine();
                    fileList.Add(file);
                }
                return fileList.OrderBy(x => x).ToList();
            }
        }

        private static void updateCurrentFileListToTxt(string processFileListPath, string updateFile)
        {
            string tempfile = Path.GetTempFileName();
            StreamWriter writer = new StreamWriter(tempfile);
            StreamReader reader = new StreamReader(processFileListPath);
            writer.WriteLine(new FileInfo(updateFile).Name);
            while (!reader.EndOfStream)
                writer.WriteLine(reader.ReadLine());
            writer.Close();
            reader.Close();
            File.Copy(tempfile, processFileListPath, true);
        }

        private IList<String> getAllFtpFiles()
        {
            var remotePath = _configProvider.GetFtpRemotePath();
            var regExpression = _configProvider.GetFtpRegExpression();
            _logger.InfoFormat("ftp path: {0} and {1}", remotePath, regExpression);
            IList<string> result = _ftpClient.GetAllFtpFiles(remotePath, regExpression).OrderBy(x => x).ToList();
            _logger.InfoFormat("result: {0}", result);
            return result;
        }

        private void prepareContextData(IJobExecutionContext context)
        {
            _logger.Info("Getting Job Data from context");

            var confProvider = context.Trigger.JobDataMap.Get("ConfigurationProvider") as IConfigurationProvider;
            if (confProvider == null)
                throw new ArgumentException("Cant get IConfigurationProvider from context");
            _configProvider = confProvider;

            var ftpClient = context.Trigger.JobDataMap.Get("FtpClient") as IFtpClient;
            if (ftpClient == null)
                throw new ArgumentException("Cant get IFtpClient from context");
            _ftpClient = ftpClient;

            var service = context.Trigger.JobDataMap.Get("MeasureService") as IMeasureService;
            if (service == null)
                throw new ArgumentException("Cant get IMeasureService from context");
            _measureService = service;

            var plantPowerService = context.Trigger.JobDataMap.Get("PlantPowerService") as IPlantPowerService;
            if (plantPowerService == null)
                throw new ArgumentException("Cant get IPlantPowerService from context");
            _plantPowerService = plantPowerService;

            var plantService = context.Trigger.JobDataMap.Get("PlantService") as IPlantService;
            if (plantService == null)
                throw new ArgumentException("Cant get IPlantService from context");
            _plantService = plantService;

            var extracter = context.Trigger.JobDataMap.Get("MeasureFileExtracter") as IMeasureFileExtracter;
            if (extracter == null)
                throw new ArgumentException("Cant get IMeasureFileExtracter from context");
            _fileExtracter = extracter;

            var extracterPlantPower = context.Trigger.JobDataMap.Get("PlantPowerFileExtracter") as IPlantPowerFileExtracter;
            if (extracterPlantPower == null)
                throw new ArgumentException("Cant get IPlantPowerFileExtracter from context");
            _filePlantPowerExtracter = extracterPlantPower;

            _logger.Info("Done getting Job Data from context");
        }

        private void processFiles(IList<string> filePaths) //, ApiPlant plant)
        {
            foreach (var filePath in filePaths)
            {

                _logger.Debug(Path.GetFileName(filePath));

                // only one Unit so far 
                string checkPlant = _configProvider.GetPlantName();
                // jvr: skip file when name of the plant is not recognized
                if (checkPlant == "unknown")
                {
                    _logger.InfoFormat("Unkown name of the plant: {0}", Path.GetFileName(filePath));
                    continue;
                }

                ApiPlant plant = _plantService.GetPlant(checkPlant);
                try
                {
                    _logger.InfoFormat("Processing file: {0}", filePath);
                    processFile(filePath, plant);
                    _logger.InfoFormat("Processed file: {0}", filePath);
                }
                catch (Exception e)
                {
                    _logger.ErrorFormat("Exception while processing file: {0}\nStackTrace: \n{1}\n", e.Message, e.StackTrace);
                }
            }
        }

        private void processFile(string filenamePath, ApiPlant plant)
        {

            FileStream _fileStream = new FileStream(filenamePath, FileMode.Open,
                                      FileAccess.Read);

            IWorkbook _workbook = WorkbookFactory.Create(_fileStream);
            _fileStream.Close();
            // End initialize

            string sheetName = findCurrentSheet(_workbook);
            if (sheetName == " ")
            {
                _logger.Warn("No sheet name matched current month");
                var processedFileTextToAppend = _configProvider.GetFtpProcessedFilesPath();
                _ftpClient.MoveFtpProcessedFile(filenamePath, processedFileTextToAppend, ".nodata");
                return;
            }

            ISheet _workSheet = _workbook.GetSheet(sheetName);
            IList<Measure> measures = getAllMeasuresFromSheet(_workSheet, plant);

            if (measures == null || !measures.Any())
            {
                _logger.Warn("No measures found inside the file");
                // jvr we check as processed even when no data is found
                var processedFileTextToAppend = _configProvider.GetFtpProcessedFilesPath();
                _ftpClient.MoveFtpProcessedFile(filenamePath, processedFileTextToAppend, ".nodata");
                return;
            }

            _logger.InfoFormat("Found {0} measures to send", measures.Count);
            sendMeasures(filenamePath, measures);

        }

        private IList<Measure> getAllMeasuresFromSheet(ISheet WorkSheet, ApiPlant Plant)
        {
            // first iteration to extract row and column index from the measure´s table
            int FirstRow = 0;
            int LastDay = 0;
            foreach (IRow row in WorkSheet)
            {
                //Index to find sheet header (first row)
                if (row.GetCell(0) != null && row.GetCell(1).NumericCellValue == 1)
                {
                    FirstRow = row.RowNum;
                    int c = 1;

                    //Index to find last column (day) with value
                    while (row.GetCell(c).NumericCellValue <= 31)
                    {
                        LastDay = row.GetCell(c).ColumnIndex;
                        //check that there is some value for the iterated day by going to the SUM cell at the end
                        if (WorkSheet.GetRow(FirstRow + 25).GetCell(c).NumericCellValue == 0)
                        {
                            break;
                        }
                        c++;
                    }
                    break;
                }
            }

            // set current month
            DateTime now = DateTime.Now;
            DateTime ProdDate = new DateTime(now.Year, now.Month, 1);
            DateTime InitialDate = new DateTime(now.Year, now.Month, 1);

            // initialize variables
            bool TheEnd = false;
            double Data = 0;
            var result = new List<Measure>();

            //start iteration per day
            for (int day = 1; day <= LastDay; day++)
            {
                //get every hour in the sheet
                for (int hour = 1; hour <= 24; hour++)
                {

                    //it will finish reading when a blank cell is found
                    var check = WorkSheet.GetRow(FirstRow + hour).GetCell(day);
                    if (check.ToString() == "")
                    {
                        TheEnd = true;
                        break;
                    }

                    Data = WorkSheet.GetRow(FirstRow + hour).GetCell(day).NumericCellValue;
                    // ProdDate = ProdDate(now.Year, now.Month, 1);
                    ProdDate = InitialDate.Date.AddDays(day - 1).AddHours(hour);

                    var measure = _fileExtracter.ProcessLine(ProdDate, Data, Plant);

                    result.Add(measure);
                }

                if (TheEnd == true)
                {
                    break;
                }

            }

            return result;
        }

        // Extract sheet name matching current year and month 
        private string findCurrentSheet(IWorkbook Workbook)
        {
            bool isMonth = false;
            bool isYear = false;
            string currentSheet = " ";
            foreach (ISheet sheet in Workbook)
            {

                isMonth = checkMonthinSheetName(sheet.SheetName);
                if (isMonth)
                {

                    isYear = checkYearinSheetName(sheet.SheetName);
                    if (isYear)
                    {
                        currentSheet = sheet.SheetName;
                        break;
                    }
                }

            }
            return currentSheet;
        }

        private bool checkMonthinSheetName(string sheet)
        {

            // find three chracter month in Romanian culture
            DateTime now = DateTime.Now;
            CultureInfo ru = new CultureInfo("ro-RO");
            string currentMonth = now.ToString("MMM", ru);
            currentMonth = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(currentMonth.Remove(currentMonth.Length - 1));

            if (sheet.Substring(0, 3) == currentMonth)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private bool checkYearinSheetName(string sheet)
        {
            // extract year from last 4 characters in sheet name
            DateTime now = DateTime.Now;
            CultureInfo ru = new CultureInfo("ro-RO");
            string currentYear = now.Year.ToString();

            if (sheet.Substring(4, 4) == currentYear)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        private void sendMeasures(string filePath, IList<Measure> measures)
        {
            trySendMeasures(filePath, measures);
        }

        private bool trySendMeasures(string filePath, IList<Measure> measures)
        {
            _logger.InfoFormat("Sending {0} Measures from file {1}", measures.Count, filePath);
            if (!_measureService.TrySendMeasures(measures))
            {
                _logger.ErrorFormat("Data has not been sent: {0}", filePath);
                return false;
            }

            moveProcessedFile(filePath);
            _logger.InfoFormat("DONE sending {0} Measures from file {1}", measures.Count, filePath);
            return true;
        }

        private void moveProcessedFile(string fullPathToFile)
        {
            var name = Path.GetFileName(fullPathToFile);
            try
            {
                var movedFile = string.Format("{0}{1}", _configProvider.GetFtpProcessedFilesPath(), string.Format("{0}.OK", name));
                _logger.Debug(string.Format("Moving file to {0}", movedFile));
                var version = 0;
                while (File.Exists(movedFile))
                {
                    _logger.Warn(string.Format("The file {0} exists. Creating new version", movedFile));
                    version++;
                    movedFile = string.Format("{0}{1}", _configProvider.GetFtpProcessedFilesPath(),
                        string.Format("{0}.OK.{1}", name, version));
                }
                File.Move(fullPathToFile, movedFile);
            }
            catch (Exception ex)
            {
                String error = String.Format("Error trying to move file '{0}' to directory '{1}' Exception: {2}", name,
                     _configProvider.GetFtpProcessedFilesPath(), ex.Message);
                _logger.Error(error);
            }
        }

        private void refreshConfigSettings()
        {
            _logger.Debug("Refreshing config file cache");
            _configProvider.Refresh();
            _logger.Debug("Done refreshing config file cache");
        }
    }
}
