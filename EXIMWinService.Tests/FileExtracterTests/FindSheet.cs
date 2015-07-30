using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EXIMWinService.Model;
using EXIMWinService.FileExtracter;
using System;
using EXIMWinService.Services;
using EXIMWinService.Configuration;
using EXIMWinService.Quartz;
using Quartz;
using Moq;
using Quartz.Impl;
using System.Text.RegularExpressions;
using System.Data;
using System.Windows.Forms;
using System.Globalization;
using NPOI.HSSF.UserModel;
using System.IO;
using NPOI.SS.UserModel;


namespace EXIMWinService.Tests.FileExtracterTests
{
    [TestClass]
    public class FindSheet
    {

        [TestMethod]
        [DeploymentItem(@"../../TestData/Production NALBANT Jan-Jul16 2015.xls", "TestData")]
        public void FindMonth()
        {

            string filenamePath = @"TestData\Production NALBANT Jan-Jul16 2015.xls";
            FileStream _fileStream = new FileStream(filenamePath, FileMode.Open,
                                      FileAccess.Read);

            IWorkbook _workbook = WorkbookFactory.Create(_fileStream);
            _fileStream.Close();

            // End initialize

            DateTime now = DateTime.Now;
            string monthName = new DateTime(2010, 8, 1)
                .ToString("MMM");
            CultureInfo ru = new CultureInfo("ro-RO");
            string currentMonth = now.ToString("MMM", ru);
            currentMonth = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(currentMonth.Remove(currentMonth.Length - 1));

            string getMonth = "";

            foreach (ISheet sheet in _workbook)
            {

                if (sheet.SheetName.Substring(0, 3) == currentMonth)
                {
                    getMonth = sheet.SheetName;
                }
            }

            Assert.AreEqual(getMonth, "Iul 2015");

        }

        [TestMethod]
        [DeploymentItem(@"../../TestData/Production NALBANT Jan-Jul16 2015.xls", "TestData")]
        public void FindYear()
        {

            string filenamePath = @"TestData\Production NALBANT Jan-Jul16 2015.xls";
            FileStream _fileStream = new FileStream(filenamePath, FileMode.Open,
                                      FileAccess.Read);

            IWorkbook _workbook = WorkbookFactory.Create(_fileStream);
            _fileStream.Close();

            // End initialize

            DateTime now = DateTime.Now;

            CultureInfo ru = new CultureInfo("ro-RO");
            string currentYear = now.Year.ToString();
            string getYear = " ";

            foreach (ISheet sheet in _workbook)
            {
                string sheetYear = sheet.SheetName.Substring(4, 4);
                if (sheetYear == currentYear)
                {
                    getYear = sheetYear;
                }

            }

            Assert.AreEqual(getYear, "2015");
        }

        [TestMethod]
        [DeploymentItem(@"../../TestData/Production NALBANT Jan-Jul16 2015.xls", "TestData")]
        public void FindDateFirstLine()
        {

            string filenamePath = @"TestData\Production NALBANT Jan-Jul16 2015.xls";
            FileStream _fileStream = new FileStream(filenamePath, FileMode.Open,
                                      FileAccess.Read);

            IWorkbook _workbook = WorkbookFactory.Create(_fileStream);
            _fileStream.Close();

            //formulas of the Workbook are evaluated and an instance of a data formatter is created
            IFormulaEvaluator formulaEvaluator = new HSSFFormulaEvaluator(_workbook);
            DataFormatter dataFormatter = new HSSFDataFormatter(new CultureInfo("en-US"));

            // End initialize

            ISheet _worksheet = _workbook.GetSheet("Iul 2015");
            IRow firstRow = _worksheet.GetRow(0);
            int numrows = firstRow.LastCellNum;

            DateTime now = DateTime.Now;
            string result = " ";

            foreach (ICell cell in firstRow)
            {

                if (cell.StringCellValue != "")
                {
                    var Title = cell.StringCellValue;
                    var Date = Title.Substring(Title.Length - 10, 10);
                    DateTime dt = Convert.ToDateTime(Date);
                    int currentYear = now.Year;
                    int currentMonth = now.Month;
                    if (dt.Year == currentYear)
                    {
                        if (dt.Month == currentMonth)
                        {
                            result = Date;
                            break;
                        }

                    }
                }
            }

            Assert.AreEqual(result, "31.07.2015");

        }

        [TestMethod]
        [DeploymentItem(@"../../TestData/Production NALBANT Jan-Jul22 2015.xls", "TestData")]
        public void iterateData()
        {

            var mockConfigProvider = new Mock<IConfigurationProvider>();
            mockConfigProvider.Setup(x => x.GetHourlyPlantsString()).Returns("MAREE01");
            mockConfigProvider.Setup(x => x.GetMeasureSourceFor1HResolution()).Returns("CLIENTE1H");
            mockConfigProvider.Setup(x => x.GetDataVariable()).Returns("E");
            mockConfigProvider.Setup(x => x.GetResolution()).Returns("1H");
            mockConfigProvider.Setup(x => x.GetEXIMToGnarumOffsetHours()).Returns(-1);
            mockConfigProvider.Setup(x => x.GetMeasureValueMultiplier()).Returns(1000);


            var apiPlant = new ApiPlant
            {
                Id = "NALBAN01",
                Technology = "EO",
                CountryCode = "ES",
                RegionCode = "28",
                TimeZone = "E. Europe Standard Time",
                Latitude = 40.4293,
                Longitude = -3.6574,
                Power = 22668
            };

            var mockPlantService = new Mock<IPlantService>();
            mockPlantService.Setup(x => x.GetPlant("NALBAN01")).Returns(apiPlant);


            string filenamePath = @"TestData\Production NALBANT Jan-Jul22 2015.xls";
            FileStream _fileStream = new FileStream(filenamePath, FileMode.Open,
                                      FileAccess.Read);

            IWorkbook _workbook = WorkbookFactory.Create(_fileStream);
            _fileStream.Close();

            //formulas of the Workbook are evaluated and an instance of a data formatter is created
            IFormulaEvaluator formulaEvaluator = new HSSFFormulaEvaluator(_workbook);
            DataFormatter dataFormatter = new HSSFDataFormatter(new CultureInfo("en-US"));

            MeasureFileExtracter measureFile = new MeasureFileExtracter(mockConfigProvider.Object, mockPlantService.Object);

            // End initialize
            ISheet _worksheet = _workbook.GetSheet("Iul 2015");
            int FirstRow = 0;
            int LastDay = 0;
            foreach (IRow row in _worksheet)
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
                        if (_worksheet.GetRow(FirstRow + 25).GetCell(c).NumericCellValue == 0)
                        {
                            break;
                        }
                        c++;
                    }
                    break;
                }
            }

            //DataTable results = new DataTable();
            //results.Columns.Add("Date", typeof(DateTime));
            //results.Columns.Add("Value", typeof(double));
            DateTime now = DateTime.Now;
            DateTime ProdDate = new DateTime(now.Year, now.Month, 1);
            DateTime InitialDate = new DateTime(now.Year, now.Month, 1);
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
                    var check = _worksheet.GetRow(FirstRow + hour).GetCell(day);

                    if (check.ToString() == "")
                    {
                        TheEnd = true;
                        break;
                    }
                    Data = _worksheet.GetRow(FirstRow + hour).GetCell(day).NumericCellValue;
                    // ProdDate = ProdDate(now.Year, now.Month, 1);
                    ProdDate = InitialDate.Date.AddDays(day - 1).AddHours(hour);
                    var measure = measureFile.ProcessLine(ProdDate, Data, apiPlant);

                    result.Add(measure);
                }

                if (TheEnd == true)
                {
                    break;
                }

            }

            Assert.AreEqual(Data, 0.254);
        }
    }

}
