using System;
using System.Windows.Input;

namespace WPFApp.Helpers
{
    /// <summary>
    /// RelayCommand polyvalent : accepte Action, Func<bool> (sans param)
    /// ou Action<object?>, Predicate<object?> (avec paramètre).
    /// </summary>
    public class RelayCommand : ICommand
    {
        // champs internes : l'un des couples sera non-null selon constructeur utilisé
        private readonly Action? _executeNoParam;
        private readonly Func<bool>? _canExecuteNoParam;

        private readonly Action<object?>? _executeWithParam;
        private readonly Predicate<object?>? _canExecuteWithParam;

        public event EventHandler? CanExecuteChanged;

        // Constructor for parameterless actions
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _executeNoParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteNoParam = canExecute;
        }

        // Constructor for object? parameter actions (commonly used by WPF)
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _executeWithParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteWithParam = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecuteWithParam != null) return _canExecuteWithParam(parameter);
            if (_canExecuteNoParam != null) return _canExecuteNoParam();
            return true;
        }

        public void Execute(object? parameter)
        {
            if (_executeWithParam != null) { _executeWithParam(parameter); return; }
            _executeNoParam?.Invoke();
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
