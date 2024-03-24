namespace WebUI.Model.Hypermedia;

public delegate Uri GenerateHypermediaUri(string routeName, object? values = default);
public static class GenerateHypermediaUriServiceCollectionExtensions
{
    public static IServiceCollection AddHypermediaUriGenerator(this IServiceCollection services)
        => services.AddTransient(CreateGenerateHypermediaUriDelegate);

    public static GenerateHypermediaUri CreateGenerateHypermediaUriDelegate(IServiceProvider sp)
    {
        var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new NullReferenceException();
        var linkGenerator = sp.GetRequiredService<LinkGenerator>();
        return (routeName, values) => GenerateHypermediaUri(httpContext, linkGenerator, routeName, values);
    }

    public static Uri GenerateHypermediaUri(HttpContext httpContext, LinkGenerator linkGenerator, string routeName, object? values)
    {
        var url = linkGenerator.GetUriByRouteValues(httpContext, routeName, values)
            ?? throw new ArgumentOutOfRangeException(nameof(routeName), "The supplied route name did not match any known routes");
        // For easy compatibility with URI templates [rfc6570] always unescape the resulting string
        return new(Uri.UnescapeDataString(url));
    }
}
