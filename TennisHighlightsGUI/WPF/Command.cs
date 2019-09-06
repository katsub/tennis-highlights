using System;
using System.Windows.Input;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// The base class for WPF button commands
    /// </summary>
    /// <seealso cref="System.Windows.Input.ICommand" />
    public class Command : ICommand
    {
        /// <summary>
        /// The execute
        /// </summary>
        private readonly Action<object> _execute;
        /// <summary>
        /// The can execute
        /// </summary>
        private readonly Func<object, bool> _canExecute;

        /// <summary>
        /// Called when can execute changed.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        /// <param name="canExecute">The can execute.</param>
        public Command(Action<object> execute, Func<object,bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// True if the command can be executed, false otherwise.
        /// </summary>
        /// <param name="parameter">The command parameter
        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        /// <summary>
        /// The command's action.
        /// </summary>
        /// <param name="parameter">The command's parameters.
        public void Execute(object parameter) => _execute(parameter);
    }
}
