using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Contracts
{
    public interface IAutoCADStateService
    {
        bool HasActiveDocument { get; }
        event Action? StateChanged;
    }
}
