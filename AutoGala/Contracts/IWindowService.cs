using AutoGala.views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace AutoGala.Contracts
{
    public interface IWindowService
    {
        GalaPromptView ShowGalaPrompt();
    }
}
