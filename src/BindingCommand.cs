using System.ComponentModel;
using System.Windows.Input;

namespace TripleG3.CIS.Maui;

/// <summary>
/// A lightweight <see cref="ICommand"/> implementation that wires <see cref="CanExecuteChanged"/>
/// to an <see cref="INotifyPropertyChanged"/> source. Useful for MVVM command bindings in .NET MAUI.
/// Supports both parameterless and parameterized delegates.
/// </summary>
public class BindingCommand : ICommand
{
    /// <summary>
    /// Occurs when changes occur that affect whether the command should execute.
    /// This event is raised whenever the provided <see cref="INotifyPropertyChanged"/> source
    /// triggers <see cref="INotifyPropertyChanged.PropertyChanged"/>.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    private readonly Action<object?> _execute;
    private readonly Func<object?, bool> _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingCommand"/> class using parameterless delegates.
    /// </summary>
    /// <param name="execute">The action to invoke when the command is executed.</param>
    /// <param name="canExecute">A function that determines whether the command can execute.</param>
    /// <param name="notifyPropertyChanged">The notifier whose <see cref="INotifyPropertyChanged.PropertyChanged"/> raises <see cref="CanExecuteChanged"/>.</param>
    public BindingCommand(Action execute, Func<bool> canExecute, INotifyPropertyChanged notifyPropertyChanged)
        : this(x => execute(), x => canExecute(), notifyPropertyChanged) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingCommand"/> class using parameterized delegates.
    /// </summary>
    /// <param name="execute">The action to invoke when the command is executed.</param>
    /// <param name="canExecute">A function that determines whether the command can execute.</param>
    /// <param name="notifyPropertyChanged">The notifier whose <see cref="INotifyPropertyChanged.PropertyChanged"/> raises <see cref="CanExecuteChanged"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="execute"/> or <paramref name="canExecute"/> is <c>null</c>.</exception>
    public BindingCommand(Action<object?> execute, Func<object?, bool> canExecute, INotifyPropertyChanged notifyPropertyChanged)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        notifyPropertyChanged.PropertyChanged += (s, e) => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">An optional command parameter.</param>
    /// <returns><c>true</c> if the command can execute; otherwise, <c>false</c>.</returns>
    public bool CanExecute(object? parameter) => _canExecute(parameter);

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="parameter">An optional command parameter.</param>
    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event.
    /// </summary>
    protected virtual void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// Strongly-typed variant of <see cref="BindingCommand"/> that uses a parameter of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
public class BindingCommand<T>(Action<T?> execute, Func<T?, bool> canExecute, INotifyPropertyChanged notifyPropertyChanged)
    : BindingCommand(x => execute((T?)x), x => canExecute((T?)x), notifyPropertyChanged)
{ }