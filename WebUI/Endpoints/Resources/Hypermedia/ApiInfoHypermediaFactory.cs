using WebUI.Model;
using WebUI.Model.Hypermedia;

namespace WebUI.Endpoints.Resources.Hypermedia;

public class ApiInfoHypermediaFactory(HttpContext httpContext, LinkGenerator linkGenerator) : HypermediaResourceFactory<ApiInfoDto>
{
    protected override Hyperlink GetCanonicalSelf(ApiInfoDto resource)
        => new(linkGenerator.GetUriByRouteValues(httpContext, nameof(ApiInfoEndpoints.GetApiInfo)) ?? throw new NullReferenceException());
}
