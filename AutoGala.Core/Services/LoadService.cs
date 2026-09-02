using Plugin.Core.Contracts;
using Plugin.Core.Models;

namespace Plugin.Core.Services
{
    public class LoadService : ILoadService
    {
        public LoadItem CreateLoad()
        {
            return new LoadItem
            {
                N = 0,
                Mx = 0,
                My = 0
            };
        }

        public LoadItem CreateLoad(double? n, double? mx, double? my)
        {
            return new LoadItem
            {
                N = n,
                Mx = mx,
                My = my
            };
        }
    }
}
