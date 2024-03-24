
namespace WebUI.Filters;

public class RequirePreconditionFilter : IEndpointFilter
{
    static string[] _applyToMethods = ["PUT", "PATCH", "DELETE"];

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_applyToMethods.Contains(context.HttpContext.Request.Method) && context.HttpContext.Request.Headers.IfMatch.Count == 0)
        {
            throw new MissingPreconditionException();
        }

        return next(context);
    }
}

public class MissingPreconditionException : ApplicationException
{

}
