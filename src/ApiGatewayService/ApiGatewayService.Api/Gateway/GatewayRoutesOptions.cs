namespace ApiGatewayService.Api.Gateway;

public sealed class GatewayRoutesOptions
{
    public string IdentityService { get; init; } = "http://localhost:5145";
    public string UserProfileService { get; init; } = "http://localhost:5182";
    public string PetService { get; init; } = "http://localhost:5035";
    public string MediaService { get; init; } = "http://localhost:5217";
    public string ProfessionalService { get; init; } = "http://localhost:5231";
    public string HelpRequestService { get; init; } = "http://localhost:5228";
    public string AlertService { get; init; } = "http://localhost:5214";
}
