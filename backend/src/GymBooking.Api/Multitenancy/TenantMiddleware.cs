namespace GymBooking.Api.Multitenancy;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CurrentTenant currentTenant)
    {
        var tenantIdClaim = context.User.FindFirst("tenantId")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            currentTenant.SetTenant(tenantId);
        }

        await _next(context);
    }
}
