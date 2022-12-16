using Docker.DotNet;
using Docker.DotNet.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Agregar el cliente de Docker como un servicio
// Default Docker Engine on Windows
DockerClient client = new DockerClientConfiguration(
    new Uri("npipe://./pipe/docker_engine"))
     .CreateClient();

// Default Docker Engine on Linux
//DockerClient client = new DockerClientConfiguration(
//    new Uri("unix:///var/run/docker.sock"))
//     .CreateClient();

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
