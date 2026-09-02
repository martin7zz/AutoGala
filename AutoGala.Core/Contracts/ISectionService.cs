using Plugin.Core.Models;

namespace Plugin.Core.Contracts
{
    public interface ISectionService
    {
        SectionItem CreateSection();
        SectionItem CreateSection(double? x, double? y);
    }
}
