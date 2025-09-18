namespace TripleG3.CIS.Maui;

/// <summary>
/// Defines a minimal observable state pattern for services/view models: exposes the current <see cref="State"/>
/// and notifies consumers via <see cref="StateChanged"/> whenever the state updates.
/// </summary>
/// <typeparam name="TState">The state type exposed by the service. Prefer an immutable record for safe updates.</typeparam>
/// <remarks>
/// Guidance:
/// - Use an immutable state (e.g., C# record) and update with <c>with</c>-expressions.
/// - Assign the new state first, then raise <see cref="StateChanged"/> with the new value.
/// - In UI contexts (MAUI), marshal notifications onto the UI thread if bindings require it.
/// </remarks>
/// <example>
/// Example of implementing <see cref="IState{TState}"/> in a service:
/// <code><![CDATA[
/// public class PersonService(IPersonDB personDB) : IPersonService, IState<PersonServiceState>
/// {
///     private PersonServiceState state = PersonServiceState.Empty;
///
///     public event Action<PersonServiceState> StateChanged = delegate { };
///
///     public PersonServiceState State
///     {
///         get => state;
///         private set
///         {
///             state = value;
///             StateChanged(state);
///         }
///     }
///
///     public async Task LoadPersonsAsync()
///     {
///         State = State with { People = ImmutableList<Person>.Empty, IsLoading = true, SelectedPerson = Person.Empty };
///         State = State with { People = await personDB.GetPeopleAsync(), IsLoading = false };
///     }
///
///     public void SelectPerson(Person person)
///     {
///         State = State with { SelectedPerson = person };
///     }
///
///     public void DeselectPerson()
///     {
///         State = State with { SelectedPerson = Person.Empty };
///     }
/// }
/// ]]></code>
/// </example>
public interface IState<TState>
{
    /// <summary>
    /// Gets the current state snapshot.
    /// </summary>
    /// <remarks>
    /// Implementations typically hold a private backing field and assign a new immutable instance
    /// before raising <see cref="StateChanged"/>.
    /// </remarks>
    TState State { get; }

    /// <summary>
    /// Raised whenever <see cref="State"/> changes.
    /// </summary>
    /// <remarks>
    /// The new state is passed as the event argument. Raise after updating the backing field.
    /// </remarks>
    event Action<TState> StateChanged;
}