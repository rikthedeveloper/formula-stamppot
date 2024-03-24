using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using WebUI.Endpoints;
using WebUI.Endpoints.Resources;
using WebUI.Endpoints.Resources.Hypermedia;
using WebUI.Model.Hypermedia;

namespace WebUI.Filters;

public record class HypermediaTypeOptions(Type FactoryType);

public class HypermediaOptions
{
    readonly Dictionary<Type, HypermediaTypeOptions> _options;
    readonly string _canonicalContentType = "application/vnd.lss.hyp+json";
    readonly string[] _additionalMatchingContentTypes = ["application/json"];

    public HypermediaOptions()
    {
        _options = new()
        {
            { typeof(ChampionshipResource), new HypermediaTypeOptions(typeof(ChampionshipHypermediaFactory)) },
            { typeof(ResourceCollection<ChampionshipResource>), new HypermediaTypeOptions(typeof(ChampionshipsCollectionHypermediaFactory)) },
            { typeof(TrackResource), new HypermediaTypeOptions(typeof(TrackHypermediaFactory)) },
            { typeof(TrackResourceCollection), new HypermediaTypeOptions(typeof(TracksCollectionHypermediaFactory)) }
        };
    }

    public string CanonicalMediaType => _canonicalContentType;
    public string[] AdditionalMatchingMediaTypes => _additionalMatchingContentTypes;

    public HypermediaTypeOptions? GetOptions(Type resultType)
        => _options.TryGetValue(resultType, out var typeOptions) ? typeOptions : null;
}

public static class HypermediaFilterFactory
{
    public static TBuilder AddHypermediaFilters<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.AddEndpointFilter(HypermediaContentTypeFilter).AddEndpointFilterFactory(HypermediaEnricherFilterFactory);

    static async ValueTask<object?> HypermediaContentTypeFilter(EndpointFilterInvocationContext invocationContext, EndpointFilterDelegate next)
    {
        var result = await next(invocationContext);
        if (result is HypermediaResource or IValueHttpResult<HypermediaResource>)
        {
            invocationContext.HttpContext.Response.ContentType = "application/vnd.less.hyp+json";
        }
        return result;
    }

    static EndpointFilterDelegate HypermediaEnricherFilterFactory(EndpointFilterFactoryContext filterFactoryContext, EndpointFilterDelegate next)
    {
        var options = new HypermediaOptions();
        var returnParameterType = filterFactoryContext.MethodInfo.ReturnParameter.ParameterType;
        if (returnParameterType == typeof(void) || returnParameterType == typeof(Task))
        {
            return next;
        }

        // Convert async return types to their real types
        if (returnParameterType.IsGenericType && returnParameterType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnParameterType = returnParameterType.GenericTypeArguments[0];
        }

        var returnTypeOptions = options.GetOptions(returnParameterType);
        // IResult return types can only be processed once their exact value is known
        if (!returnParameterType.IsAssignableTo(typeof(IResult)) && returnTypeOptions is null)
        {
            return next;
        }

        return async (invocationContext) =>
        {
            var acceptHeader = invocationContext.HttpContext.Request.GetTypedHeaders().Accept;
            if (acceptHeader.Count > 0 && !acceptHeader.Any(acc => acc.MatchesMediaType(options.CanonicalMediaType) || options.AdditionalMatchingMediaTypes.Any(ammt => acc.MatchesMediaType(ammt))))
            {
                return await next(invocationContext);
            }

            var result = await next(invocationContext);
            // If the return type is a result object, extra parsing is needed
            if (result is IValueHttpResult valueResult and { Value: not null })
            {
                var resultType = valueResult.GetType();
                returnTypeOptions = options.GetOptions(valueResult.Value.GetType());
                if (returnTypeOptions is null)
                {
                    return result;
                }

                var hypermediaFactory = (HypermediaResourceFactory)ActivatorUtilities.CreateInstance(invocationContext.HttpContext.RequestServices, returnTypeOptions.FactoryType);
                if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(CreatedAtRoute<>))
                {
                    return _convertCreatedAtRouteMethod.MakeGenericMethod(resultType.GenericTypeArguments[0]).Invoke(null, [hypermediaFactory, result, valueResult.Value]);
                }

                return new HypermediaResult(hypermediaFactory.GetHypermedia(valueResult.Value));
            }
            else if (result is null || returnTypeOptions is null)
            {
                return result;
            }
            else
            {
                var hypermediaFactory = (HypermediaResourceFactory)ActivatorUtilities.CreateInstance(invocationContext.HttpContext.RequestServices, returnTypeOptions.FactoryType);
                return new HypermediaResult(hypermediaFactory.GetHypermedia(result));
            }
        };
    }

    static readonly MethodInfo _convertCreatedAtRouteMethod = typeof(HypermediaFilterFactory).GetMethod(nameof(ConvertCreatedAtRoute), BindingFlags.Static | BindingFlags.NonPublic)!;

    static CreatedHypermediaResult ConvertCreatedAtRoute<TValue>(HypermediaResourceFactory hypermediaResourceFactory, CreatedAtRoute<TValue> result, TValue value)
        where TValue : class
        => new(result.RouteName, result.RouteValues, hypermediaResourceFactory.GetHypermedia(value));
}

public class HypermediaResult(HypermediaResource value) : IResult, IContentTypeHttpResult, IValueHttpResult, IStatusCodeHttpResult
{
    public virtual int StatusCode => StatusCodes.Status200OK;
    int? IStatusCodeHttpResult.StatusCode => StatusCode;
    public string ContentType { get; init; } = "application/vnd.lss.hyp+json";
    public HypermediaResource Value { get; } = value;
    object? IValueHttpResult.Value => Value;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<HypermediaResult>();

        await OnExecutingAsync(httpContext);

        logger.LogInformation(new EventId(1, "WritingResultAsStatusCode"), "Setting HTTP status code {StatusCode}.", StatusCode);
        httpContext.Response.StatusCode = StatusCode;
        if (Value.Metadata?.Version is not null)
        {
            httpContext.Response.Headers.ETag = Value.Metadata.Version;
        }

        await WriteResultAsJsonAsync(httpContext, logger, Value, ContentType);
    }

    protected virtual Task OnExecutingAsync(HttpContext httpContext) => Task.CompletedTask;

    #region Helper code copied from aspnetcore
    public static Task WriteResultAsJsonAsync<TValue>(
        HttpContext httpContext,
        ILogger logger,
        TValue? value,
        string? contentType = null,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        if (value is null)
        {
            return Task.CompletedTask;
        }

        jsonSerializerOptions ??= (httpContext.RequestServices.GetService<IOptions<JsonOptions>>()?.Value ?? new JsonOptions()).SerializerOptions;
        var jsonTypeInfo = (JsonTypeInfo<TValue>)jsonSerializerOptions.GetTypeInfo(typeof(TValue));

        var runtimeType = value.GetType();
        if (runtimeType is null || jsonTypeInfo.Type == runtimeType || jsonTypeInfo.Type.IsSealed || jsonTypeInfo.Type.IsValueType || jsonTypeInfo.PolymorphismOptions is not null)
        {
            logger.LogInformation(new EventId(3, "WritingResultAsJson"), "Writing value of type '{Type}' as Json.", jsonTypeInfo.Type.Name);
            return httpContext.Response.WriteAsJsonAsync(
                value,
                jsonTypeInfo,
                contentType: contentType);
        }

        logger.LogInformation(new EventId(3, "WritingResultAsJson"), "Writing value of type '{Type}' as Json.", runtimeType.Name);
        // Since we don't know the type's polymorphic characteristics
        // our best option is to serialize the value as 'object'.
        // call WriteAsJsonAsync<object>() rather than the declared type
        // and avoid source generators issues.
        // https://github.com/dotnet/aspnetcore/issues/43894
        // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-polymorphism
        return httpContext.Response.WriteAsJsonAsync<object>(
           value,
           jsonSerializerOptions,
           contentType: contentType);
    }
    #endregion
}

public class CreatedHypermediaResult(string? routeName, RouteValueDictionary? routeValues, HypermediaResource value) : HypermediaResult(value)
{
    public override int StatusCode => StatusCodes.Status201Created;

    /// <summary>
    /// Gets the name of the route to use for generating the URL.
    /// </summary>
    public string? RouteName { get; } = routeName;

    /// <summary>
    /// Gets the route data to use for generating the URL.
    /// </summary>
    public RouteValueDictionary RouteValues { get; } = routeValues ?? new();

    protected override Task OnExecutingAsync(HttpContext httpContext)
    {
        var linkGenerator = httpContext.RequestServices.GetRequiredService<LinkGenerator>();
        var url = linkGenerator.GetUriByRouteValues(
            httpContext,
            RouteName,
            RouteValues,
            fragment: FragmentString.Empty);

        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("No route matches the supplied values.");
        }

        httpContext.Response.Headers.Location = url;
        return Task.CompletedTask;
    }
}