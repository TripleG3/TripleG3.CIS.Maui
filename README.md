# TripleG3.CIS.Maui

Tools to help implement the CIS (Command Immutable State) pattern in .NET MAUI.

This package provides three small building blocks for clean MVVM:

- `IState<TState>` — observable immutable state for Services (state type lives in your Models layer)
- `BindingCommand` — simple `ICommand` for ViewModels that auto-refreshes `CanExecute` on property changes
- `Ioc` — an attached property to resolve a page's `BindingContext` via dependency injection in XAML

The goal is to keep ViewModels thin, Services state-driven, and Views declarative.

## Install

```pwsh
dotnet add package TripleG3.CIS.Maui
```

## 1) Immutable Service State with `IState<TState>` (Models + Services)

`IState<TState>` is intended to be implemented by Services. The state type (`TState`) should be an immutable record in your Models layer. Services expose the current `State` and raise `StateChanged` whenever the state is replaced with a new instance.

Models (immutable state record):

```csharp
// Models/PersonServiceState.cs
public sealed record PersonServiceState(
	ImmutableList<Person> People,
	bool IsLoading,
	Person? SelectedPerson)
{
	public static readonly PersonServiceState Empty =
		new(ImmutableList<Person>.Empty, false, null);
}
```

Service (implements `IState<PersonServiceState>`):

```csharp
// Services/PersonService.cs
using TripleG3.CIS.Maui; // IState<TState>

public interface IPersonService : IState<PersonServiceState>
{
	Task LoadPersonsAsync();
	void SelectPerson(Person person);
	void DeselectPerson();
}

public class PersonService(IPersonDB personDB) : IPersonService
{
	private PersonServiceState state = PersonServiceState.Empty;
	public event Action<PersonServiceState> StateChanged = delegate { };

	public PersonServiceState State
	{
		get => state;
		private set { state = value; StateChanged(state); }
	}

	public async Task LoadPersonsAsync()
	{
		State = State with { People = ImmutableList<Person>.Empty, IsLoading = true, SelectedPerson = null };
		var people = await personDB.GetPeopleAsync();
		State = State with { People = people, IsLoading = false };
	}

	public void SelectPerson(Person person) => State = State with { SelectedPerson = person };
	public void DeselectPerson() => State = State with { SelectedPerson = null };
}
```

Notes:

- Use immutable updates (`with` expressions) so consumers can treat `State` as a snapshot.
- Services are the source of truth; ViewModels subscribe/reflect state as needed.

## 2) ViewModel Commands with `BindingCommand` (ViewModels)

`BindingCommand` is a lightweight `ICommand` that listens to `INotifyPropertyChanged` and automatically raises `CanExecuteChanged` when your ViewModel properties change. Wire it immutably in the ViewModel constructor.

```csharp
// ViewModels/MainViewModel.cs
using System.Windows.Input;
using TripleG3.CIS.Maui; // BindingCommand

public class MainViewModel : INotifyPropertyChanged
{
	private readonly IPersonService _service;
	public event PropertyChangedEventHandler? PropertyChanged;

	public ICommand RefreshCommand { get; }
	public ICommand SelectFirstCommand { get; }

	public bool CanRefresh => !_service.State.IsLoading;

	public MainViewModel(IPersonService service)
	{
		_service = service;

		RefreshCommand = new BindingCommand(
			execute: async () => await _service.LoadPersonsAsync(),
			canExecute: () => CanRefresh,
			notifyPropertyChanged: this);

		SelectFirstCommand = new BindingCommand(
			execute: () =>
			{
				var p = _service.State.People.FirstOrDefault();
				if (p is not null) _service.SelectPerson(p);
			},
			canExecute: () => _service.State.People.Count > 0,
			notifyPropertyChanged: this);

		// Optionally mirror service state into VM props and raise PropertyChanged
		_service.StateChanged += s =>
		{
			// e.g., raise PropertyChanged for props that depend on service state
			PropertyChanged?.Invoke(this, new(nameof(CanRefresh)));
		};
	}
}
```

Notes:

- Because `BindingCommand` is subscribed to the ViewModel's `PropertyChanged`, updating any property will trigger a `CanExecuteChanged`, keeping buttons enabled/disabled correctly.
- For parameterized commands, use `BindingCommand<T>`.

## 3) XAML DI with `Ioc` Attached Property (Views)

Use `Ioc.Ioc.BindingContext` to resolve your View's `BindingContext` from the DI container right in XAML. This keeps code-behind minimal and wiring declarative.

```xml
<!-- Views/MainPage.xaml -->
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
	xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
	xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
	xmlns:vm="clr-namespace:YourApp.ViewModels"
	xmlns:ioc="clr-namespace:TripleG3.CIS.Maui"
	x:Class="YourApp.Views.MainPage"
	x:DataType="{x:Type vm:MainViewModel}"
	ioc:Ioc.BindingContext="{x:Type vm:MainViewModel}">

	<VerticalStackLayout Padding="24">
		<Button Text="Refresh" Command="{Binding RefreshCommand}" />
		<Button Text="Select First" Command="{Binding SelectFirstCommand}" />
	</VerticalStackLayout>
</ContentPage>
```

DI registrations (typically in `MauiProgram`):

```csharp
// MauiProgram.cs
builder.Services.AddSingleton<IPersonDB, PersonDb>();
builder.Services.AddSingleton<IPersonService, PersonService>();
builder.Services.AddTransient<MainViewModel>();
```

The attached property defers assignment until the View is loaded, then resolves the specified type from the MAUI service provider and sets it as the `BindingContext`.

## Layering Guidance (MVVM)

- Models: immutable state records (e.g., `PersonServiceState`) and domain entities/value objects.
- Services: implement `IState<TState>`; own business logic and mutate state immutably.
- ViewModels: compose services, expose commands via `BindingCommand`, and surface computed properties.
- Views (XAML): set `BindingContext` via `Ioc.Ioc.BindingContext`; bind to VM properties/commands.

## Notes & Tips

- `IState<TState>` updates should be atomic (replace entire state object) to keep consumers simple.
- If `StateChanged` must notify UI, consider marshalling to the UI thread (e.g., `MainThread.BeginInvokeOnMainThread`).
- `BindingCommand` ties into `INotifyPropertyChanged`—be sure to raise for any properties affecting `CanExecute`.
- `Ioc.Ioc.BindingContext` requires the requested type to be registered with MAUI DI.

