using Plugin.Core.Models;

namespace Plugin.Core.Contracts
{
    public interface ILoadService
    {
        LoadItem CreateLoad();
        LoadItem CreateLoad(double? n, double? mx, double? my);
    }
}
