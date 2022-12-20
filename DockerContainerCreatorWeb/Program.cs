using Docker.DotNet;
using Docker.DotNet.Models;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
var IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

string DockerApiUri()
{
    if (IsWindows)
        return "npipe://./pipe/docker_engine";

    if (IsLinux)
        return "unix:///var/run/docker.sock";

    throw new Exception(
        "Was unable to determine what OS this is running on, does not appear to be Windows or Linux!?");
}

// Agregar el cliente de Docker como un servicio
DockerClient client = new DockerClientConfiguration(
    new Uri(DockerApiUri()))
     .CreateClient();

System.Diagnostics.Debug.WriteLine(client);

builder.Services.AddSingleton<DockerClient>(client);

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

app.Run();
