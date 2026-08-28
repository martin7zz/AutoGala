using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace AutoGala.Contracts
{
    public interface ISortingService
    {
        public List<SectionItem> SortLines(List<LineData> lines);
    }
}
