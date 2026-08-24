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
            ObservableCollection<LoadItem> items, bool isSimpleBending);

        ObservableCollection<SectionItem> LoadSectionsExcel();
        ObservableCollection<RebarItem> LoadRebarsExcel();
        ObservableCollection<LoadItem> LoadLoadsExcel(bool isSimpleBending);

        void SaveAllToExcel(
            ObservableCollection<SectionItem> sections,
            ObservableCollection<RebarItem> rebars,
            ObservableCollection<LoadItem> loads,
            bool isSimpleBending);

        List<Object> LoadAllExcel(bool isSimpleBending);
    }
}
