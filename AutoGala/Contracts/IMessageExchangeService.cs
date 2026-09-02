using Plugin.Core.Models;

namespace AutoGala.Contracts
{
    public interface IMessageExchangeService
    {
        Task<(List<SectionItem>, string)> GetSectionsAsync(IAutoGalaPipeClientService autoGalaPipeClientService, string name);
        Task<(List<RebarItem>, string)> GetRebarsAsync(IAutoGalaPipeClientService autoGalaPipeClientService, string name);
        Task<((List<SectionItem>, List<RebarItem>), string)> GetAllAsync(IAutoGalaPipeClientService autoGalaPipeClientService, string name);
    }
}
