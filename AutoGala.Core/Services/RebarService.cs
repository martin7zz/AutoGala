using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Core.Services
{
    public class RebarService : IRebarService
    {
        public RebarItem CreateRebar()
        {
            return new RebarItem
            {
                Area = 0,
                X = 0,
                Y = 0
            };
        }

        public RebarItem CreateRebar(double? area, double? x, double? y)
        {
            return new RebarItem
            {
                Area = area,
                X = x,
                Y = y
            };
        }
    }
}
