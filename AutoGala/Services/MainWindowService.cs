using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Win32;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
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

        private void SaveExcel<T>(
            ObservableCollection<T> items,
            string sheetName,
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
                FileName = $"{sheetName}.xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(sheetName);

                for (int col = 0; col < columns.Length; col++)
                {
                    worksheet.Cell(1, col + 1).Value = columns[col].Header;
                }

                for (int row = 0; row < items.Count; row++)
                {
                    var item = items[row];

                    for (int col = 0; col < columns.Length; col++)
                    {
                        worksheet.Cell(row + 2, col + 1).Value =
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
        public void SaveExcel(ObservableCollection<SectionItem> items) =>
            SaveExcel(items,
                "Sections",
                ("Id", s => s.Id),
                ("X", s => s.X),
                ("Y", s => s.Y));

        public void SaveExcel(ObservableCollection<RebarItem> items) =>
            SaveExcel(items,
                "Rebars",
                ("Id", r => r.Id),
                ("Area", r => r.Area),
                ("X", r => r.X),
                ("Y", r => r.Y));

        public void SaveExcel(ObservableCollection<LoadItem> items) =>
            SaveExcel(items,
                "Loads",
                ("Id", l => l.Id),
                ("N", l => l.N),
                ("Mx", l => l.Mx),
                ("My", l => l.My));

        public void SaveAllToExcel(ObservableCollection<SectionItem> sections, ObservableCollection<RebarItem> rebars, ObservableCollection<LoadItem> loads)
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
                FileName = "AutoGala.xlsx"
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("All");

                // Sections
                if (sections != null && sections.Count > 0)
                {
                    worksheet.Cell(1, 1).Value = "Id";
                    worksheet.Cell(1, 2).Value = "X";
                    worksheet.Cell(1, 3).Value = "Y";

                    for (int row = 0; row < sections.Count; row++)
                    {
                        var item = sections[row];

                        worksheet.Cell(row + 2, 1).Value = item.Id;
                        worksheet.Cell(row + 2, 2).Value = item.X;
                        worksheet.Cell(row + 2, 3).Value = item.Y;
                    }

                    worksheet.Columns();
                }

                // Rebars
                if (rebars != null && rebars.Count > 0)
                {
                    worksheet.Cell(1, 5).Value = "Id";
                    worksheet.Cell(1, 6).Value = "Area";
                    worksheet.Cell(1, 7).Value = "X";
                    worksheet.Cell(1, 8).Value = "Y";

                    for (int row = 0; row < rebars.Count; row++)
                    {
                        var item = rebars[row];

                        worksheet.Cell(row + 2, 5).Value = item.Id;
                        worksheet.Cell(row + 2, 6).Value = item.Area;
                        worksheet.Cell(row + 2, 7).Value = item.X;
                        worksheet.Cell(row + 2, 8).Value = item.Y;
                    }

                    worksheet.Columns();
                }

                // Loads
                if (loads != null && loads.Count > 0)
                {
                    worksheet.Cell(1, 10).Value = "Id";
                    worksheet.Cell(1, 11).Value = "N";
                    worksheet.Cell(1, 12).Value = "Mx";
                    worksheet.Cell(1, 13).Value = "My";

                    for (int row = 0; row < loads.Count; row++)
                    {
                        var item = loads[row];

                        worksheet.Cell(row + 2, 10).Value = item.Id;
                        worksheet.Cell(row + 2, 11).Value = item.N;
                        worksheet.Cell(row + 2, 12).Value = item.Mx;
                        worksheet.Cell(row + 2, 13).Value = item.My;
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

        private ObservableCollection<T> LoadExcel<T>(Func<IXLRow, T> rowFactory, Action<T, int> setId)
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

                var result = new ObservableCollection<T>();

                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    if (row.CellsUsed().Count() == 0)
                    {
                        continue;
                    }

                    var item = rowFactory(row);

                    setId(item, result.Count + 1);

                    result.Add(item);
                }

                MessageBox.Show(
                    $"Loaded {result.Count} items successfully",
                    "Load Excel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

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

        public ObservableCollection<SectionItem> LoadSectionsExcel()
        {
            return LoadExcel(
                row => _sectionService.CreateSection(row.Cell(2).GetValue<double>(), row.Cell(3).GetValue<double>()), (item, id) => item.Id = id
                );
        }

        public ObservableCollection<RebarItem> LoadRebarsExcel()
        {
            return LoadExcel(
                row => _rebarService.CreateRebar(row.Cell(2).GetValue<double>(), row.Cell(3).GetValue<double>(), row.Cell(4).GetValue<double>()), (item, id) => item.Id = id
                );
        }

        public ObservableCollection<LoadItem> LoadLoadsExcel()
        {
            return LoadExcel(
                row => _loadService.CreateLoad(row.Cell(2).GetValue<double>(), row.Cell(3).GetValue<double>(), row.Cell(4).GetValue<double>()), (item, id) => item.Id = id
                );
        }

        public List<Object> LoadAllExcel()
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
                    new List<Object>()
                );
            }

            try
            {
                using var workbook = new XLWorkbook(openDialog.FileName);

                var worksheet = workbook.Worksheets.First();

                var items = new List<Object>();

                var sections = new ObservableCollection<SectionItem>();
                var rebars = new ObservableCollection<RebarItem>();
                var loads = new ObservableCollection<LoadItem>();

                // Sections
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    if (!row.Cell(1).IsEmpty())
                    {
                        var section = _sectionService.CreateSection(row.Cell(2).GetValue<double>(), row.Cell(3).GetValue<double>());

                        section.Id = sections.Count + 1;

                        sections.Add(section);
                    }
                }

                // Rebars
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    if (!row.Cell(5).IsEmpty())
                    {
                        var rebar = _rebarService.CreateRebar(row.Cell(6).GetValue<double>(), row.Cell(7).GetValue<double>(), row.Cell(8).GetValue<double>());

                        rebar.Id = rebars.Count + 1;

                        rebars.Add(rebar);
                    }
                }

                // Loads
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    if (!row.Cell(10).IsEmpty())
                    {
                        var load = _loadService.CreateLoad(row.Cell(11).GetValue<double>(), row.Cell(12).GetValue<double>(), row.Cell(13).GetValue<double>());

                        load.Id = loads.Count + 1;

                        loads.Add(load);
                    }
                }

                items.Add(sections);
                items.Add(rebars);
                items.Add(loads);

                MessageBox.Show(
                    $"Loaded successfully.",
                    "Load Excel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

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
                   new List<Object>()
                );
            }
        }
    }

}
