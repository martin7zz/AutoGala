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
        private int _nextId = 1;

        public SectionItem CreateSection()
        {
            return new SectionItem
            {
                Id = _nextId++,
                X = 0,
                Y = 0
            };
        }

        public SectionItem CreateSection(double x, double y)
        {
            return new SectionItem
            {
                Id = _nextId++,
                X = x,
                Y = y
            };
        }

        public void ResetCounter()
        {
            _nextId = 1;
        }
    }
}
