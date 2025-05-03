using Duende.IdentityServer.Models;
using Duende.IdentityModel;

public static class Config
{
    public static IEnumerable<IdentityResource> Identity =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile()
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new[] { new ApiScope("myApi", "Demo API") };

    public static IEnumerable<Client> Clients =>
        new[]
        {
            new Client
            {
                ClientId = "razor",
                ClientName = "Razor Pages Client",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                RedirectUris = { "https://localhost:5001/signin-oidc" },
                AllowedScopes = { "openid", "profile", "myApi" },
                AllowAccessTokensViaBrowser = true
            }
        };
}
