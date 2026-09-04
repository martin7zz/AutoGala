using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Contracts
{
    public interface IAutoCADOperationRunner
    {
        Task<(bool Success, T?)> RunAsync<T>(Func<Task<T>> operation);
    }
}
