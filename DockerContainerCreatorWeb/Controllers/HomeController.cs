using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace DockerContainerCreatorWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly DockerClient _client;

        public HomeController(DockerClient client)
        {
            _client = client;
        }

        public IActionResult Index()
        {
            // Obtener la lista de imágenes disponibles en el servidor de Docker
            var images = _client.Images.ListImagesAsync(new ImagesListParameters()).Result;
            System.Diagnostics.Debug.WriteLine(images);
            // Enviar la lista de imágenes al formulario de creación de contenedores
            return View(images);
        }

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
            return View("Index", images);
        }
    }
}
