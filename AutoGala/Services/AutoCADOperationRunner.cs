using AutoGala.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Services
{
    public class AutoCADOperationRunner : IAutoCADOperationRunner
    {
        private readonly IAutoGalaPipeClientService _pipeClientService;
        private readonly IWindowService _windowService;

        public AutoCADOperationRunner(IAutoGalaPipeClientService pipeClientService, IWindowService windowService)
        {
            _pipeClientService = pipeClientService;
            _windowService = windowService;
        }

        public async Task<(bool Success, T?)> RunAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                _pipeClientService.ActivateAutoCAD();
                return (true, await operation());
            }
            catch (InvalidOperationException ex)
            {
                _windowService.ShowError(ex.Message);
                return (false, default);
            }
        }
    }
}
