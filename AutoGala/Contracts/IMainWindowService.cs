using Plugin.Core.Models;
using System.Collections.ObjectModel;

namespace Plugin.Core.Contracts
{
    public interface IMainWindowService
    {
        void SaveExcel(
            ObservableCollection<SectionItem> items, JobInfo jobInfo);
        void SaveExcel(
            ObservableCollection<RebarItem> items, JobInfo jobInfo);
        void SaveExcel(
            ObservableCollection<LoadItem> items, bool isSimpleBending, JobInfo jobInfo);

        ObservableCollection<SectionItem> LoadSectionsExcel(JobInfo jobInfo);
        ObservableCollection<RebarItem> LoadRebarsExcel(JobInfo jobInfo);
        ObservableCollection<LoadItem> LoadLoadsExcel(bool isSimpleBending, JobInfo jobInfo);

        void SaveAllToExcel(
            ObservableCollection<SectionItem> sections,
            ObservableCollection<RebarItem> rebars,
            ObservableCollection<LoadItem> loads,
            bool isSimpleBending,
            JobInfo jobInfo);

        List<Object> LoadAllExcel(bool isSimpleBending);
    }
}
