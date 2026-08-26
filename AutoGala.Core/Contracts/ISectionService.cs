using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Core.Contracts
{
    public interface ISectionService
    {
        SectionItem CreateSection();
        SectionItem CreateSection(double? x, double? y);
    }
}
