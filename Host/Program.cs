using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSettings(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIdentityInfrastructure();
builder.Services.AddApplication();
builder.Services.AddCronJobs();

builder.WebHost.UseStaticWebAssets();

var app = builder.Build();

await app.UseInfrastructureAsync();

app.UseHttpsRedirection();

app.MapStaticAssets();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapFallback(context =>
{
    context.Response.Redirect("/Home/Error");
    return Task.CompletedTask;
});

app.UseRequestLocalization();

app.Run();
