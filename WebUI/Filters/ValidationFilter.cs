using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.Json.Serialization;
using WebUI.Endpoints.Resources;

namespace WebUI.Filters;

public record class ValidationMessage(string PropertyName, string Code, string Message);

public record class RequiredValidationMessage(
    string PropertyName)
    : ValidationMessage(PropertyName, PropertyName + "Required", $"{PropertyName} is required");

public record class StringLengthValidationMessage(
    string PropertyName, 
    [property:JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Min,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Max) 
    : ValidationMessage(PropertyName, PropertyName + "Length", GetMessage(PropertyName, Min, Max))
{
    static string GetMessage(string propertyName, int? min, int? max)
    {
        return (min, max) switch
        {
            (int, int) => $"{propertyName} must be between {min} and {max} characters",
            (int, null) => $"{propertyName} must be at least {min} characters",
            (null, int) => $"{propertyName} must be at most {max} characters",
            (null, null) => throw new InvalidOperationException()
        };
    }
}

public interface IValidator
{
    IEnumerable<ValidationMessage> Validate();
}

public class ValidationException(IEnumerable<ValidationMessage> failures) : ApplicationException("Validation errors have occurred.")
{
    public ValidationException()
        : this([])
    { }

    public IReadOnlyDictionary<string, ValidationMessage[]> Errors { get; } = failures
        .GroupBy(e => e.PropertyName)
        .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
}

public class ValidationFilter(int parameterIndex, IEnumerable<PropertyInfo> fieldProperties) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        object bodyArgument = context.GetArgument<object>(parameterIndex);
        if (bodyArgument is not null)
        {
            var validationResult = new List<ValidationMessage>();
            foreach (var property in fieldProperties)
            {
                var fieldValue = property.GetValue(bodyArgument)!;
                var hasValue = fieldValue.GetType().GetProperty(nameof(Field<object>.HasValue))!;
                if (!(bool)hasValue.GetValue(fieldValue)!)
                {
                    validationResult.Add(new ValidationMessage(property.Name, property.Name + "Invalid", $"{property.Name} could not be parsed."));
                }
            }

            if (bodyArgument is IValidator validator)
            {
                validationResult.AddRange(validator.Validate());
            }

            if (validationResult.Count > 0)
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
            var fieldProperties = bodyParameter.ParameterType.GetProperties()
                .Where(p => p.CanRead && p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Field<>))
                .ToList();

            if (fieldProperties.Count > 0 || bodyParameter.ParameterType.GetInterface(nameof(IValidator)) != null) 
            {
                var validationFilter = new ValidationFilter(bodyParamIndex.Value, fieldProperties);
                return invocationContext => validationFilter.InvokeAsync(invocationContext, next);
            }
        }

        return invocationContext => next(invocationContext);
    }
}
