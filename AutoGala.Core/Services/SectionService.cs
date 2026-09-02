using Plugin.Core.Contracts;
using Plugin.Core.Models;

namespace Plugin.Core.Services
{
    public class SectionService : ISectionService
    {
        public SectionItem CreateSection()
        {
            return new SectionItem
            {
                X = 0,
                Y = 0
            };
        }

        public SectionItem CreateSection(double? x, double? y)
        {
            return new SectionItem
            {
                X = x,
                Y = y
            };
        }

    }
}
