using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.Json.Serialization;
using WebUI.Endpoints.Resources;
using WebUI.Extensions;

namespace WebUI.Filters;

public record class ValidationMessage(string PropertyName, string Code, string Message);

public interface IValidator2
{
    Task<ValidationResult> ValidateAsync();
}

public class ValidationException(IEnumerable<ValidationMessage> failures) : ApplicationException("Validation errors have occurred.")
{
    public ValidationException()
        : this([])
    { }

    public ValidationException(ValidationResult validationResult) 
        : this(validationResult.Errors.Select(vf => new ValidationMessage(vf.PropertyName, vf.ErrorMessage, vf.ErrorCode.TrimFromEnd("Validator", StringComparison.OrdinalIgnoreCase))))
    { }

    public IReadOnlyDictionary<string, ValidationMessage[]> Errors { get; } = failures
        .GroupBy(e => e.PropertyName)
        .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
}

public class ValidationFilter(int parameterIndex) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var bodyArgument = context.GetArgument<object>(parameterIndex);
        if (bodyArgument is IValidator2 validator)
        {
            var validationResult = await validator.ValidateAsync();

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult);
            }
        }

        return await next.Invoke(context);
    }

    public static EndpointFilterDelegate FilterFactory(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        static int? GetBodyParameterIndex(ParameterInfo[] parameters)
        {
            int? paramIndex = null;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterInfo = parameters[i];

                // If the parameter is not a class it won't have Field properties
                if (!parameterInfo.ParameterType.IsClass)
                    continue;

                // Early return when explicitly marked as body
                if (parameterInfo.GetCustomAttribute<FromBodyAttribute>() != null
                    || parameterInfo.GetCustomAttribute<FromFormAttribute>() != null)
                    return i;

                // Early return when we have a 'best candidate' already
                // By convention the body parameter is the first candidate parameter
                // But keep iterating in case there's a FromBody parameter
                if (paramIndex is not null)
                    continue;

                if (parameterInfo.GetCustomAttribute<FromServicesAttribute>() != null
                    || parameterInfo.GetCustomAttribute<FromKeyedServicesAttribute>() != null
                    || parameterInfo.GetCustomAttribute<FromQueryAttribute>() != null
                    || parameterInfo.GetCustomAttribute<FromRouteAttribute>() != null
                    || parameterInfo.GetCustomAttribute<FromHeaderAttribute>() != null)
                    continue;

                // Right now this our best guess
                // But keep iterating in case there's a FromBody parameter
                paramIndex ??= i;
            }

            return paramIndex;
        }

        var handlerParameters = context.MethodInfo.GetParameters();
        int? bodyParamIndex = GetBodyParameterIndex(handlerParameters);

        if (bodyParamIndex is not null)
        {
            var bodyParameter = handlerParameters[bodyParamIndex.Value];

            if (bodyParameter.ParameterType.GetInterface(nameof(IValidator2)) != null)
            {
                var validationFilter = new ValidationFilter(bodyParamIndex.Value);
                return invocationContext => validationFilter.InvokeAsync(invocationContext, next);
            }
        }

        return invocationContext => next(invocationContext);
    }
}