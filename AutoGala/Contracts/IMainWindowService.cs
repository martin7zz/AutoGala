using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Core.Contracts
{
    public interface IMainWindowService
    {
        void SaveExcel(
            ObservableCollection<SectionItem> items);
        void SaveExcel(
            ObservableCollection<RebarItem> items);
        void SaveExcel(
            ObservableCollection<LoadItem> items);

        ObservableCollection<SectionItem> LoadSectionsExcel();
        ObservableCollection<RebarItem> LoadRebarsExcel();
        ObservableCollection<LoadItem> LoadLoadsExcel();

        void SaveAllToExcel(
            ObservableCollection<SectionItem> sections,
            ObservableCollection<RebarItem> rebars,
            ObservableCollection<LoadItem> loads);

        List<Object> LoadAllExcel();
    }
}
