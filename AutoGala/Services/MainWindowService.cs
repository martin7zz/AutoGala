using ClosedXML.Excel;
using Microsoft.Win32;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace AutoGala.Services
{
    public class MainWindowService : IMainWindowService
    {
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
    }
}
