namespace CampusConnect.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var isLoggedIn = context.Session.GetString("UserId") != null;

        // Allow access to Account, Home (landing), and static files without login
        bool allowedWithoutLogin = path.StartsWith("/account") 
                                 || path.StartsWith("/home") 
                                 || path == "/" 
                                 || path.StartsWith("/css")
                                 || path.StartsWith("/js");

        if (!isLoggedIn && !allowedWithoutLogin)
        {
            context.Response.Redirect("/Account/Login");
            return;
        }

        await _next(context);
    }
}
