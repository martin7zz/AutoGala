using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Contracts
{
    public interface IDialogService
    {
        bool Confirm(string message, string title);
    }
}
