using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Core.Services
{
    public class LoadService : ILoadService
    {
        private int _nextId = 1;

        public LoadItem CreateLoad()
        {
            return new LoadItem
            {
                Id = _nextId++,
                N = 0,
                Mx = 0,
                My = 0
            };
        }

        public LoadItem CreateLoad(double n, double mx, double my)
        {
            return new LoadItem
            {
                Id = _nextId++,
                N = n,
                Mx = mx,
                My = my
            };
        }

        public void ResetCounter()
        {
            _nextId = 1;
        }
    }
}
