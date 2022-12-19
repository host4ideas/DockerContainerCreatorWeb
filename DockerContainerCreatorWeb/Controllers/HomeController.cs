using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using DockerContainerCreatorWeb.Models;
using Docker.DotNet.Models;
using Docker.DotNet;

namespace DockerContainerCreatorWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly DockerClient _client;

        private readonly ILogger<HomeController> _logger;

        public HomeController(DockerClient client, ILogger<HomeController> logger)
        {
            _client = client;
            _logger = logger;
        }
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Method <c>Index</c> executed on view creation.
        /// </summary>
        public IActionResult Index()
        {
            // Obtain the list of images available on the Docker server
            var images = _client.Images.ListImagesAsync(new ImagesListParameters()).Result;

            // Obtain the list of containers on the Docker server
            var containers = _client.Containers.ListContainersAsync(new ContainersListParameters()).Result;

            System.Diagnostics.Debug.WriteLine(containers.Count);
            System.Diagnostics.Debug.WriteLine(images.Count);

            // Add the list of images and containers to the view data
            ViewData["images"] = images;
            ViewData["containers"] = containers;

            return View();
        }

        /// <summary>
        /// Method <c>Create</c> creates a Docker container. Sends to view the existing containers and images.
        /// </summary>
        /// <param name="image">
        /// The name of the image to use to create the new container.
        /// </param>
        /// <param name="containerName">
        /// The name for the new container.
        /// </param>
        [HttpPost]
        public async Task<IActionResult> Create(string image, string containerName)
        {
            try
            {
                // Crear una nueva configuración para el contenedor
                var config = new Config
                {
                    Image = image,
                    AttachStdin = true,
                    AttachStdout = true,
                    AttachStderr = true,
                    OpenStdin = true,
                    Tty = true
                };

                // Crear una nueva configuración para el host del contenedor
                var hostConfig = new HostConfig
                {
                    AutoRemove = true
                };

                // Crear una nueva opción de creación de contenedor
                var createOptions = new CreateContainerParameters(config)
                {
                    HostConfig = hostConfig,
                    Name = containerName
                };

                // Crear el contenedor
                await _client.Containers.CreateContainerAsync(createOptions);

                // Mostrar un mensaje de éxito al usuario
                ViewData["Message"] = "Contenedor creado con éxito!";
            }
            catch (Exception ex)
            {
                // Mostrar un mensaje de error al usuario
                ViewData["Message"] = $"Error al crear el contenedor: {ex.Message}";
            }

            // Obtain the list of images available on the Docker server
            var images = _client.Images.ListImagesAsync(new ImagesListParameters()).Result;

            // Obtain the list of containers on the Docker server
            var containers = _client.Containers.ListContainersAsync(new ContainersListParameters()).Result;

            // Add the list of images and containers to the view data
            ViewData["images"] = images;
            ViewData["containers"] = containers;

            // Render the view, passing the list of images and containers as arguments
            return View("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
