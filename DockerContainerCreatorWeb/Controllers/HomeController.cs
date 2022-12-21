using Docker.DotNet;
using Docker.DotNet.Models;
using DockerContainerCreatorWeb.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
            try
            {
                // try to ping Doccker daemon through Docker Client --> raises DockerApiException
                _client.System.PingAsync().Wait();

                ViewData["Message"] = "Docker se está ejecutando con normalidad";

                // Obtain the list of images available on the Docker server
                var images = _client.Images.ListImagesAsync(new ImagesListParameters()).Result;

                // Obtain the list of containers on the Docker server
                var containers = _client.Containers.ListContainersAsync(new ContainersListParameters
                {
                    All = true
                }).Result;

                // Add the list of images and containers to the view data
                ViewData["images"] = images;
                ViewData["containers"] = containers;
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = $"No se pudo conectar al Docker daemon. Compruebe que Docker se está ejecutando con normalidad:<br />{ex.Message}";
            }

            return View();
        }

        private async Task PullImageIfNotExist(string image, CancellationToken ct = default)
        {
            var existingContainers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            }, ct);

            Console.WriteLine(existingContainers.Count);

            var exists = existingContainers.Any(x => { 
                Console.WriteLine(x.Image);
                return x.Image == image;
            });

            if (!exists)
            {
                await _client.Images.CreateImageAsync(new ImagesCreateParameters
                {
                    FromImage = image,
                }, null, null, ct);
            }
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
        public async Task<IActionResult> Create(string image, string containerName, CancellationToken ct = default)
        {
            try
            {
                await PullImageIfNotExist(image, ct);

                // Crear una nueva configuración para el contenedor
                var config = new Config
                {
                    AttachStdin = true,
                    AttachStdout = true,
                    AttachStderr = true,
                    OpenStdin = true,
                    Tty = true
                };

                // Crear el contenedor
                var createdContainer = await _client.Containers.CreateContainerAsync(new CreateContainerParameters(config)
                {
                    //ExposedPorts = new Dictionary<string, EmptyStruct>{
                    //    {
                    //        "80/tcp",
                    //        default
                    //    }
                    //},
                    Image = image,
                    Name = containerName,
                    HostConfig = new HostConfig
                    {
                        PublishAllPorts = true,
                        AutoRemove = true
                    }
                }, ct);

                // Mostrar un mensaje de éxito al usuario
                ViewData["Message"] = "Contenedor creado con éxito!";
            }
            catch (Exception ex)
            {
                // Mostrar un mensaje de error al usuario
                ViewData["ErrorMessage"] = $"Error al crear el contenedor: {ex.Message}";
            }

            // Obtain the list of images available on the Docker server
            var images = _client.Images.ListImagesAsync(new ImagesListParameters(), ct).Result;

            // Obtain the list of containers on the Docker server
            var containers = _client.Containers.ListContainersAsync(new ContainersListParameters(), ct).Result;

            // Add the list of images and containers to the view data
            ViewData["images"] = images;
            ViewData["containers"] = containers;

            // Render the view, passing the list of images and containers as arguments
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> StopContainer(string containerID)
        {
            // Stop and remove the container
            await _client.Containers.StopContainerAsync(containerID,
                new ContainerStopParameters
                {
                    WaitBeforeKillSeconds = 10
                });
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RemoveContainer(string containerID)
        {
            await _client.Containers.RemoveContainerAsync(containerID,
            new ContainerRemoveParameters
            {
                Force = true
            });

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
