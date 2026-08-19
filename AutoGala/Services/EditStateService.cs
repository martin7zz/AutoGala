using AutoGala.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace AutoGala.Services
{
    public class EditStateService : IEditStateService
    {
        private object? _editOwner;

        public bool IsEditing => _editOwner != null;
        
        public object? EditOwner => _editOwner;

        public event EventHandler? EditStateChanged;

        public void StartEditing(object owner)
        {
            _editOwner = owner;
            EditStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void StopEditing(object owner)
        {
            if (_editOwner == owner)
            {
                _editOwner = null;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
