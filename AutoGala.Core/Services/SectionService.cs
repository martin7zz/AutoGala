using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
