using Microsoft.AspNetCore.Identity;
using CleanserBlazorUI.Data;

public class PasswordChangeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly UserManager<ApplicationUser> _userManager;

    public PasswordChangeMiddleware(RequestDelegate next, UserManager<ApplicationUser> userManager)
    {
        _next = next;
        _userManager = userManager;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Retrieve the current user from the database
            var userId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value; // Or use ClaimTypes.NameIdentifier
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user?.MustChangePassword == true)
                {
                    var path = context.Request.Path.ToString();
                    if (!path.Equals("/Account/ForgotPassword", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Redirect("/Account/ForgotPassword");
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}
