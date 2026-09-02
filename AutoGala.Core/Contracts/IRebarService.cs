using Plugin.Core.Models;

namespace Plugin.Core.Contracts
{
    public interface IRebarService
    {
        RebarItem CreateRebar();
        RebarItem CreateRebar(double? area, double? x, double? y);
    }
}
