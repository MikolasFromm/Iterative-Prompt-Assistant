using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;
using WebWhisperer.Services;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    builder.Services.AddSingleton<WhisperService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");

    app.Run();
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message.ToString());
}