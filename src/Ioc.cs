namespace TripleG3.CIS.Maui;

/// <summary>
/// Attached property helpers for resolving and assigning a view's <see cref="BindableObject.BindingContext"/>
/// from the application's dependency injection container (IoC) at load time.
/// </summary>
/// <remarks>
/// Usage (XAML):
/// <code><![CDATA[
/// <ContentPage
///     xmlns:ioc="clr-namespace:TripleG3.CIS.Maui"
///     x:DataType="{x:Type vm:MyViewModel}"
///     ioc:Ioc.BindingContext="{x:Type vm:MyViewModel}">
/// </ContentPage>
/// ]]></code>
/// The type provided to <see cref="BindingContextProperty"/> must be registered in MAUI's service collection.
/// </remarks>
public static class Ioc
{
    /// <summary>
    /// Gets the <see cref="Type"/> currently set on the attached <see cref="BindingContextProperty"/>.
    /// </summary>
    /// <param name="target">The target bindable object.</param>
    /// <returns>The type to resolve from the service provider.</returns>
    public static Type GetBindingContext(BindableObject target) => (Type)target.GetValue(BindingContextProperty);

    /// <summary>
    /// Sets the <see cref="Type"/> to resolve from the service provider for the given target's binding context.
    /// </summary>
    /// <param name="target">The target bindable object.</param>
    /// <param name="type">The service type to resolve and assign to <see cref="BindableObject.BindingContext"/>.</param>
    public static void SetBindingContext(BindableObject target, Type type) => target.SetValue(BindingContextProperty, type);

    /// <summary>
    /// An attached property that accepts a <see cref="Type"/> to be resolved via MAUI's <see cref="IServiceProvider"/>
    /// and assigned to <see cref="BindableObject.BindingContext"/>. If the target element isn't loaded yet,
    /// assignment occurs on the <see cref="Page.Loaded"/> or <see cref="View.Loaded"/> event.
    /// </summary>
    public static readonly BindableProperty BindingContextProperty =
        BindableProperty.CreateAttached("BindingContext", typeof(Type), typeof(Ioc), null, propertyChanged: (b, o, n) =>
        {
            if (n is not Type t)
                throw new InvalidOperationException("BindingContext must be of type Type.");

            switch (b)
            {
                case Page page when !page.IsLoaded:
                    page.Loaded += Page_Loaded;
                    void Page_Loaded(object? sender, EventArgs e)
                    {
                        page.Loaded -= Page_Loaded;
                        var instance = Application.Current?.Handler?.MauiContext?.Services.GetService(t) ?? throw new InvalidOperationException($"Service of type {t.Name} not found.");
                        page.BindingContext = instance;
                    }
                    break;
                case View view when !view.IsLoaded:
                    view.Loaded += View_Loaded;
                    void View_Loaded(object? sender, EventArgs e)
                    {
                        view.Loaded -= View_Loaded;
                        var instance = Application.Current?.Handler?.MauiContext?.Services.GetService(t) ?? throw new InvalidOperationException($"Service of type {t.Name} not found.");
                        view.BindingContext = instance;
                    }
                    break;
                default:
                    var instance = Application.Current?.Handler?.MauiContext?.Services.GetService(t) ?? throw new InvalidOperationException($"Service of type {t.Name} not found.");
                    b.BindingContext = instance;
                    break;
            }
        });
}