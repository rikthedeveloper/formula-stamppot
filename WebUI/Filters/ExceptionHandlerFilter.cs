using Microsoft.AspNetCore.Http.Extensions;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace WebUI.Filters;

public record class ProblemInfo(HttpStatusCode StatusCode, string Detail, object? Extensions);

public delegate ProblemInfo ProblemInfoFactory(Exception exception);

public delegate ProblemInfo ProblemInfoFactory<TException>(TException exception) where TException : Exception;

public class ExceptionHandlerFilter : IEndpointFilter
{
    readonly bool _useDetailedExceptions;
    readonly Dictionary<HttpStatusCode, string> _statusCodeTitles = [];
    readonly Dictionary<Type, ProblemInfoFactory> _problemInfoFactories = [];
    readonly ProblemInfoFactory<Exception> _defaultProblemInfoFactory = exception => new(HttpStatusCode.InternalServerError, string.Empty, null);

    public ExceptionHandlerFilter(bool useDetailedExceptions)
    {
        _useDetailedExceptions = useDetailedExceptions;
    }

    public ExceptionHandlerFilter AddProblemInfoFactory<TException>(ProblemInfoFactory<TException> factory) where TException : Exception
    {
        _problemInfoFactories.Add(typeof(TException), exception => exception is TException typedException
            ? factory(typedException)
            : _defaultProblemInfoFactory(exception));
        return this;
    }

    public ExceptionHandlerFilter AddStatusCodeTitle(HttpStatusCode statusCode, string title)
    {
        _statusCodeTitles.Add(statusCode, title);
        return this;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception ex)
        {

            var problemInfoFactory = _problemInfoFactories.GetValueOrDefault(ex.GetType(), ex => _defaultProblemInfoFactory(ex));

            var info = problemInfoFactory(ex);
            var errorUri = GetErrorUri(context, ex);
            var errorInstance = GetErrorInstance(context);

            var details = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = (int)info.StatusCode,
                Type = errorUri.ToString(),
                Title = _statusCodeTitles.GetValueOrDefault(info.StatusCode, "An error occurred while processing your request."),
                Detail = info.Detail,
                Instance = errorInstance.ToString()
            };

            if (info.Extensions is not null)
            {
                var serializerOptions = (context.HttpContext.RequestServices.GetService<IOptions<JsonOptions>>()?.Value ?? new JsonOptions()).SerializerOptions;
                var fields = info.Extensions.GetType().GetProperties();
                foreach (var f in fields)
                {
                    var name = serializerOptions.PropertyNamingPolicy is not null ? serializerOptions.PropertyNamingPolicy.ConvertName(f.Name) : f.Name;
                    details.Extensions.Add(name, f.GetValue(info.Extensions));
                }
            }

            if (_useDetailedExceptions)
            {
                details.Extensions.Add("exception_message", ex.Message);
                details.Extensions.Add("exception_trace", ex.StackTrace);
            }

            return Results.Json(details, contentType: "application/problem+json", statusCode: details.Status);
        }
    }

    static string GetErrorName(Exception ex)
    {
        static string toKebabCase(string value)
        {
            var sb = new StringBuilder()
                .Append(char.ToLowerInvariant(value[0]));

            foreach (var c in value[1..])
            {
                sb = char.IsUpper(c) 
                    ? sb.Append('-').Append(char.ToLower(c)) 
                    : sb.Append(c);
            }

            return sb.ToString();
        }


        var name = ex.GetType().Name.EndsWith("EXCEPTION", StringComparison.InvariantCultureIgnoreCase) 
            ? ex.GetType().Name[0..^"EXCEPTION".Length]
            : ex.GetType().Name;
        return toKebabCase(name);
    }

    static Uri GetErrorUri(EndpointFilterInvocationContext context, Exception ex)
        => new(new Uri(context.HttpContext.Request.Scheme + "://" + context.HttpContext.Request.Host, UriKind.Absolute), $"errors/{GetErrorName(ex)}");

    static Uri GetErrorInstance(EndpointFilterInvocationContext context)
        => new(new Uri(context.HttpContext.Request.GetEncodedUrl() + "/"), $"error/{context.HttpContext.TraceIdentifier}");
}
