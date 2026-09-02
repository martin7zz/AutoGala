using ClosedXML.Excel;
using Microsoft.Win32;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace AutoGala.Services
{
    public class MainWindowService : IMainWindowService
    {
        private readonly ISectionService _sectionService;
        private readonly IRebarService _rebarService;
        private readonly ILoadService _loadService;

        public MainWindowService(
            ISectionService sectionService,
            IRebarService rebarService,
            ILoadService loadService)
        {
            _sectionService = sectionService;
            _rebarService = rebarService;
            _loadService = loadService;
        }

        Dictionary<string, object?> information = new Dictionary<string, object?>();

        private Dictionary<string, object?> BuildJobInfoDictionary(JobInfo jobInfo)
        {
            return new Dictionary<string, object?>
            {
                ["Job Title:"] = jobInfo.JobTitle,
                ["Job Number:"] = jobInfo.JobNumber,
                ["Client:"] = jobInfo.Client,
                ["Calcs by:"] = jobInfo.CalcsBy,
                ["Checked by:"] = jobInfo.CheckedBy,
            };
        }

        private void SaveExcel<T>(
            ObservableCollection<T> items,
            string sheetName,
            JobInfo jobInfo,
            params (string Header, Func<T, object?> Value)[] columns)
        {
            if (items == null || items.Count == 0)
            {
                MessageBox.Show(
                    "Nothing to save.",
                    "Save to Excel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "Save Excel File",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true,
                FileName = $"{(string.IsNullOrWhiteSpace(jobInfo.JobTitle) ? sheetName : $"{jobInfo.JobTitle} - {sheetName}")}.xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(sheetName);

                information = BuildJobInfoDictionary(jobInfo);

                for (int col = 0; col < information.Count; col++)
                {
                    worksheet.Cell(1, col + 1).Value = information.Keys.ToArray()[col];

                    var cell = worksheet.Cell(2, col + 1);

                    if (col == 1)
                    {
                        cell.Value = Convert.ToInt64(information.Values.ToArray()[col]);
                    }
                    else
                    {
                        cell.Value = XLCellValue.FromObject(information.Values.ToArray()[col]);
                    }
                }

                var range = worksheet.Range(1, 1, 1, information.Count);
                var range2 = worksheet.Range(2, 1, 2, information.Count);

                range.Style.Font.Bold = true;
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                range.Style.Alignment.WrapText = true;

                range2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                range2.Style.Alignment.WrapText = true;

                for (int col = 0; col < columns.Length; col++)
                {
                    worksheet.Cell(4, col + 1).Value = columns[col].Header;
                }

                for (int row = 0; row < items.Count; row++)
                {
                    var item = items[row];

                    for (int col = 0; col < columns.Length; col++)
                    {
                        worksheet.Cell(row + 5, col + 1).Value =
                           XLCellValue.FromObject(columns[col].Value(item));
                    }
                }

                worksheet.Columns();

                workbook.SaveAs(saveDialog.FileName);

                MessageBox.Show(
                    "File saved successfully.",
                    "Save to Excel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not save the Excel file.\n\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        public void SaveExcel(ObservableCollection<SectionItem> items, JobInfo jobInfo) =>
            SaveExcel(items,
                "Sections",
                jobInfo,
                ("Point", s => s.Id),
                ("X [cm]", s => s.X),
                ("Y [cm]", s => s.Y));

        public void SaveExcel(ObservableCollection<RebarItem> items, JobInfo jobInfo) =>
            SaveExcel(items,
                "Rebars",
                jobInfo,
                ("Bar", r => r.Id),
                ("Asi [cm²]", r => r.Area),
                ("X [cm]", r => r.X),
                ("Y [cm]", r => r.Y));

        public void SaveExcel(ObservableCollection<LoadItem> items, bool isSimpleBending, JobInfo jobInfo)
        {
            var columns = new List<(string Header, Func<LoadItem, object?> Value)>
            {
                ("Load", l => l.Id),
                ("N [kN]", l => l.N),
                ("Mx [kNm]", l => l.Mx)
            };

            if (!isSimpleBending)
            {
                columns.Add(("My [kNm]", l => l.My));
            }

            SaveExcel(items, "Loads", jobInfo, columns.ToArray());
        }

        public void SaveAllToExcel(
            ObservableCollection<SectionItem> sections,
            ObservableCollection<RebarItem> rebars,
            ObservableCollection<LoadItem> loads,
            bool isSimpleBending,
            JobInfo jobInfo)
        {
            if ((sections == null || sections.Count == 0) &&
        (rebars == null || rebars.Count == 0) &&
        (loads == null || loads.Count == 0))
            {
                MessageBox.Show(
                    "Nothing to save.",
                    "Save to Excel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "Save Excel File",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true,
                FileName = $"{(string.IsNullOrWhiteSpace(jobInfo.JobTitle) ? "AutoGala" : jobInfo.JobTitle)}.xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("All");

                information = BuildJobInfoDictionary(jobInfo);

                for (int col = 0; col < information.Count; col++)
                {
                    worksheet.Cell(1, col + 1).Value = information.Keys.ToArray()[col];
                    var cell = worksheet.Cell(2, col + 1);

                    if (col == 1)
                    {
                        cell.Value = Convert.ToInt64(information.Values.ToArray()[col]);
                    }
                    else
                    {
                        cell.Value = XLCellValue.FromObject(information.Values.ToArray()[col]);
                    }
                }

                var range = worksheet.Range(1, 1, 1, information.Count);
                var range2 = worksheet.Range(2, 1, 2, information.Count);

                range.Style.Font.Bold = true;
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                range.Style.Alignment.WrapText = true;

                range2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                range2.Style.Alignment.WrapText = true;

                // Sections
                if (sections != null && sections.Count > 0)
                {
                    worksheet.Cell(4, 1).Value = "Point";
                    worksheet.Cell(4, 2).Value = "X [cm]";
                    worksheet.Cell(4, 3).Value = "Y [cm]";

                    for (int row = 0; row < sections.Count; row++)
                    {
                        var item = sections[row];

                        worksheet.Cell(row + 5, 1).Value = item.Id;
                        worksheet.Cell(row + 5, 2).Value = item.X;
                        worksheet.Cell(row + 5, 3).Value = item.Y;
                    }

                    worksheet.Columns();
                }

                // Rebars
                if (rebars != null && rebars.Count > 0)
                {
                    worksheet.Cell(4, 5).Value = "Bar";
                    worksheet.Cell(4, 6).Value = "Asi [cm²]";
                    worksheet.Cell(4, 7).Value = "X [cm]";
                    worksheet.Cell(4, 8).Value = "Y [cm]";

                    for (int row = 0; row < rebars.Count; row++)
                    {
                        var item = rebars[row];

                        worksheet.Cell(row + 5, 5).Value = item.Id;
                        worksheet.Cell(row + 5, 6).Value = item.Area;
                        worksheet.Cell(row + 5, 7).Value = item.X;
                        worksheet.Cell(row + 5, 8).Value = item.Y;
                    }

                    worksheet.Columns();
                }

                // Loads
                if (loads != null && loads.Count > 0)
                {
                    worksheet.Cell(4, 10).Value = "Load";
                    worksheet.Cell(4, 11).Value = "N [kN]";
                    worksheet.Cell(4, 12).Value = "Mx [kNm]";
                    if (!isSimpleBending)
                    {
                        worksheet.Cell(4, 13).Value = "My [kNm]";
                    }

                    for (int row = 0; row < loads.Count; row++)
                    {
                        var item = loads[row];

                        worksheet.Cell(row + 5, 10).Value = item.Id;
                        worksheet.Cell(row + 5, 11).Value = item.N;
                        worksheet.Cell(row + 5, 12).Value = item.Mx;
                        if (!isSimpleBending)
                        {
                            worksheet.Cell(row + 5, 13).Value = item.My;
                        }
                    }

                    worksheet.Columns();
                }

                workbook.SaveAs(saveDialog.FileName);

                MessageBox.Show(
                    "File saved successfully.",
                    "Save to Excel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not save the Excel file.\n\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private ObservableCollection<T> LoadExcel<T>(Func<IXLRow, T> rowFactory, Action<T, int> setId, JobInfo jobInfo,
            params string[] expectedHeaders)
        {
            var openDialogue = new OpenFileDialog
            {
                Title = "Open Excel File",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                Multiselect = false
            };

            if (openDialogue.ShowDialog() != true)
            {
                return new ObservableCollection<T>();
            }

            try
            {
                using var workbook = new XLWorkbook(openDialogue.FileName);

                var worksheet = workbook.Worksheets.First();

                bool expectedJobInfo = JobInfoHasHeaders(
                    worksheet,
                    1,
                    "Job Title:", "Job Number:", "Client:", "Calcs by:", "Checked by:");

                if (expectedJobInfo)
                {
                    // jobInfo
                    jobInfo.JobTitle = worksheet.Cell(2, 1).GetString();
                    jobInfo.JobNumber = worksheet.Cell(2, 2).GetString();
                    jobInfo.Client = worksheet.Cell(2, 3).GetString();
                    jobInfo.CalcsBy = worksheet.Cell(2, 4).GetString();
                    jobInfo.CheckedBy = worksheet.Cell(2, 5).GetString();
                }

                for (int i = 0; i < expectedHeaders.Length; i++)
                {
                    var actualHeaders = worksheet.Cell(4, i + 1).GetString().Trim();
                    var expected = expectedHeaders[i];

                    if (!string.Equals(actualHeaders, expected, StringComparison.Ordinal))
                    {
                        MessageBox.Show(
                            $"Excel is in wrong format.\n\n" +
                            $"Column {i + 1} should be '{expected}', " +
                            $"but found '{actualHeaders}'.",
                            "Load Excel",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return new ObservableCollection<T>();
                    }
                }

                var result = new ObservableCollection<T>();

                foreach (var row in worksheet.RowsUsed().Skip(3))
                {
                    if (row.CellsUsed().Count() == 0)
                    {
                        continue;
                    }

                    var item = rowFactory(row);

                    setId(item, result.Count + 1);

                    result.Add(item);
                }

                if (result.Count == 0)
                {
                    MessageBox.Show(
                        $"No items were loaded",
                        "Load Excel",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        $"Loaded {result.Count} items successfully",
                        "Load Excel",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }


                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load the Excel file.\n\n{ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return new ObservableCollection<T>();
            }
        }

        public ObservableCollection<SectionItem> LoadSectionsExcel(JobInfo jobInfo)
        {
            return LoadExcel(
                row => _sectionService.CreateSection(row.Cell(2).GetValue<double>(), row.Cell(3).GetValue<double>()),
                (item, id) => item.Id = id,
                jobInfo,
                "Point", "X [cm]", "Y [cm]");
        }

        public ObservableCollection<RebarItem> LoadRebarsExcel(JobInfo jobInfo)
        {
            return LoadExcel(
                row => _rebarService.CreateRebar(row.Cell(2).GetValue<double>(), row.Cell(3).GetValue<double>(), row.Cell(4).GetValue<double>()),
                (item, id) => item.Id = id,
                jobInfo,
                "Bar", "Asi [cm²]", "X [cm]", "Y [cm]"
                );
        }

        public ObservableCollection<LoadItem> LoadLoadsExcel(bool isSimpleBending, JobInfo jobInfo)
        {
            var expectedHeaders = new List<string>
            {
                "Load",
                "N [kN]",
                "Mx [kNm]"
            };

            if (!isSimpleBending)
            {
                expectedHeaders.Add("My [kNm]");
            }

            return LoadExcel(
                row => _loadService.CreateLoad(
                    row.Cell(2).GetValue<double>(),
                    row.Cell(3).GetValue<double>(),
                    isSimpleBending ? 0 : row.Cell(4).GetValue<double>()),
                (item, id) => item.Id = id,
                jobInfo,
                expectedHeaders.ToArray());
        }

        private bool JobInfoHasHeaders(
           IXLWorksheet worksheet,
           int startColumn,
           params string[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                var actual = worksheet.Cell(1, startColumn + i).GetString().Trim();

                if (!string.Equals(actual, expected[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasHeaders(
            IXLWorksheet worksheet,
            int startColumn,
            params string[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                var actual = worksheet.Cell(4, startColumn + i).GetString().Trim();

                if (!string.Equals(actual, expected[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public List<object> LoadAllExcel(bool isSimpleBending)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Open Excel File",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                Multiselect = false
            };

            if (openDialog.ShowDialog() != true)
            {
                return (
                    new List<object>()
                );
            }

            try
            {
                using var workbook = new XLWorkbook(openDialog.FileName);

                var worksheet = workbook.Worksheets.First();

                var items = new List<object>();

                var sections = new ObservableCollection<SectionItem>();
                var rebars = new ObservableCollection<RebarItem>();
                var loads = new ObservableCollection<LoadItem>();

                var jobInfo = new JobInfo();

                bool sectionsValid = HasHeaders(
                    worksheet,
                    1,
                    "Point", "X [cm]", "Y [cm]");

                bool rebarsValid = HasHeaders(
                    worksheet,
                    5,
                    "Bar", "Asi [cm²]", "X [cm]", "Y [cm]");

                var expectedLoads = new List<string>
                {
                    "Load",
                    "N [kN]",
                    "Mx [kNm]"
                };

                bool expectedJobInfo = JobInfoHasHeaders(
                    worksheet,
                    1,
                    "Job Title:", "Job Number:", "Client:", "Calcs by:", "Checked by:");

                if (!isSimpleBending)
                {
                    expectedLoads.Add("My [kNm]");
                }

                bool loadsValid = HasHeaders(
                    worksheet,
                    10,
                    expectedLoads.ToArray());

                if (!sectionsValid && !rebarsValid && !loadsValid)
                {
                    MessageBox.Show(
                        "The Excel file does not have a valid format.",
                        "Load Excel",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return new List<object>();
                }

                if (sectionsValid)
                {
                    // Sections
                    foreach (var row in worksheet.RowsUsed().Skip(3))
                    {
                        if (!row.Cell(1).IsEmpty())
                        {
                            var section = _sectionService.CreateSection(row.Cell(2).GetValue<double>(), row.Cell(3).GetValue<double>());

                            section.Id = sections.Count + 1;

                            sections.Add(section);
                        }
                    }
                }

                if (rebarsValid)
                {
                    // Rebars
                    foreach (var row in worksheet.RowsUsed().Skip(3))
                    {
                        if (!row.Cell(5).IsEmpty())
                        {
                            var rebar = _rebarService.CreateRebar(row.Cell(6).GetValue<double>(), row.Cell(7).GetValue<double>(), row.Cell(8).GetValue<double>());

                            rebar.Id = rebars.Count + 1;

                            rebars.Add(rebar);
                        }
                    }
                }

                if (loadsValid)
                {
                    // Loads
                    foreach (var row in worksheet.RowsUsed().Skip(3))
                    {
                        if (!row.Cell(10).IsEmpty())
                        {
                            double my = 0;

                            var load = _loadService.CreateLoad(row.Cell(11).GetValue<double>(), row.Cell(12).GetValue<double>(), isSimpleBending ? my : row.Cell(13).GetValue<double>());

                            load.Id = loads.Count + 1;

                            loads.Add(load);
                        }
                    }
                }

                if (expectedJobInfo)
                {
                    // jobInfo
                    jobInfo.JobTitle = worksheet.Cell(2, 1).GetString();
                    jobInfo.JobNumber = worksheet.Cell(2, 2).GetString();
                    jobInfo.Client = worksheet.Cell(2, 3).GetString();
                    jobInfo.CalcsBy = worksheet.Cell(2, 4).GetString();
                    jobInfo.CheckedBy = worksheet.Cell(2, 5).GetString();
                }

                items.Add(sections);
                items.Add(rebars);
                items.Add(loads);
                items.Add(jobInfo);

                var loaded = new List<string>();
                var notLoaded = new List<string>();

                if (sectionsValid)
                    loaded.Add("Sections");
                else
                    notLoaded.Add("Sections");

                if (rebarsValid)
                    loaded.Add("Rebars");
                else
                    notLoaded.Add("Rebars");

                if (loadsValid)
                    loaded.Add("Loads");
                else
                    notLoaded.Add("Loads");
                if (expectedJobInfo)
                    loaded.Add("Job Information");
                else
                    notLoaded.Add("Job Information");

                if (loaded.Count == 0)
                {
                    MessageBox.Show(
                        "No items were loaded.\n\n" +
                        $"Could not load: {string.Join(", ", notLoaded)}.",
                        "Load Excel",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return new List<object>();
                }
                else
                {
                    var message = $"Loaded successfully: {string.Join(", ", loaded)}.";

                    if (notLoaded.Count > 0)
                    {
                        message += $"\nCould not load: {string.Join(", ", notLoaded)}.";
                    }

                    MessageBox.Show(
                    message,
                    "Load Excel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                }

                return items;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load the Excel file.\n\n{ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return (
                   new List<object>()
                );
            }
        }
    }

}
