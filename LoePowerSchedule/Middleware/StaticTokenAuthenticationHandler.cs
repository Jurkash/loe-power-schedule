namespace LoePowerSchedule.Middleware;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

public class StaticTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly string _staticToken;

    public StaticTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration configuration)
        : base(options, logger, encoder, clock)
    {
        _staticToken = configuration["Authentication:StaticToken"];
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var extractedToken))
        {
            return Task.FromResult(AuthenticateResult.Fail("Token is missing"));
        }

        string extractedTokenStr = extractedToken.ToString().Replace("Bearer", "").Trim();
        if (!_staticToken.Equals(extractedTokenStr))
        {
            return Task.FromResult(AuthenticateResult.Fail("Unauthorized client"));
        }

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "StaticTokenUser") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
