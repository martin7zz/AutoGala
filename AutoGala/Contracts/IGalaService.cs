using Plugin.Core.Models;
using System.Collections.ObjectModel;

namespace Plugin.Core.Contracts
{
    public interface IGalaService
    {
        Task HookToGalaAsync(ObservableCollection<SectionItem> items);
        Task HookToGalaAsync(ObservableCollection<RebarItem> items);
        Task HookToGalaAsync(ObservableCollection<LoadItem> items, bool isSimpleBending);
        Task HookToGalaJobAsync(JobInfo jobInfo);

        Task<ObservableCollection<SectionItem>> GetSectionsFromGalaAsync();
        Task<ObservableCollection<RebarItem>> GetRebarsFromGalaAsync();
        Task<ObservableCollection<LoadItem>> GetLoadsFromGalaAsync(bool isSimpleBending);
    }
}
