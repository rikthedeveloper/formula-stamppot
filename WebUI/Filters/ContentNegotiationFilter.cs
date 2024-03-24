using Microsoft.AspNetCore.Http.Features;
using System.Net.Http.Headers;

namespace WebUI.Filters;

//public class ContentNegotiationFilter<TResponse>(string ContentType, string[] AdditionalMatchingContentTypes, string[] ApplicableProfiles) : IEndpointFilter
//{
//    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
//    {
//        var endpointFeature = context.HttpContext.Features.Get<IEndpointFeature>()!;
//        var endpoint = endpointFeature.Endpoint;
//        var result = await next(context);

//        if (result is not IResult && context.HttpContext.Response.StatusCode is 200 or 201)
//        {
//            var mediaType = new MediaTypeHeaderValue(ContentType);
//            if (ApplicableProfiles.Length > 0)
//            {
//                mediaType.Parameters.Add(new("profile", string.Join(' ', ApplicableProfiles)));
//            }

//            result = Results.Json(result, contentType: mediaType.ToString());
//        }

//        return result;
//    }
//}

