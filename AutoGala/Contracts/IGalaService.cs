using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Core.Contracts
{
    public interface IGalaService
    {
        Task HookToGalaAsync(ObservableCollection<SectionItem> items);
        Task HookToGalaAsync(ObservableCollection<RebarItem> items);
        Task HookToGalaAsync(ObservableCollection<LoadItem> items, bool isSimpleBending);
    }
}
