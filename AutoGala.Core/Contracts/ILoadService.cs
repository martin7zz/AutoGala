using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Core.Contracts
{
    public interface ILoadService
    {
        LoadItem CreateLoad();
        LoadItem CreateLoad(double n, double mx, double my);
        void ResetCounter();

    }
}
