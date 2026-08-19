using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Contracts
{
    public interface IEditStateService
    {
        bool IsEditing { get; }
        object? EditOwner { get; }

        void StartEditing(object owner);
        void StopEditing(object owner);

        event EventHandler? EditStateChanged;
    }
}
