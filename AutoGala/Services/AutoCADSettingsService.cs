using AutoGala.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Services
{
    internal class AutoCADSettingsService : IAutoCADSettingsService
    {
        public double ScaleFactor { get; set; } = 0.1;
    }
}
