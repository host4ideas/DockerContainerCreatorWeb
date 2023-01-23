using Docker.DotNet.Models;
using DockerContainerCreatorWeb.Models;
using DockerContainerLogic;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace DockerContainerCreatorWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly Images imagesClient;
        private readonly Containers containersClient;

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            // Initialize Docker Client
            this.imagesClient = new();
            this.containersClient = new();

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
                // Add the list of images and containers to the view data
                ViewData["images"] = this.imagesClient.GetImages();
                ViewData["containers"] = this.containersClient.GetContainers();
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = $"No se pudo conectar al Docker daemon. Compruebe que Docker se está ejecutando con normalidad:<br />{ex.Message}";
            }

            return View();
        }

        /// <summary>
        /// Checks if the desired image exist, if not it will be pulled from DockerHub.
        /// </summary>
        /// <param name="image"></param> Name of the image.
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task PullImageIfNotExist(string image, CancellationToken ct = default)
        {
            try
            {
                // List all images on the machine
                var images = this.imagesClient.GetImages();

                // Check if the image is present on the machine
                var exists = images.Any(x => x.RepoTags.Contains(image));

                if (!exists)
                {
                    // Pull the image from the Docker registry
                    await this.imagesClient.ClientInstance.Images.CreateImageAsync(
                        new ImagesCreateParameters
                        {
                            FromImage = image,
                        },
                        null,
                        new Progress<JSONMessage>(m => Console.WriteLine(m.Status)), ct);
                }
            }
            catch (Exception ex)
            {
                // Mostrar un mensaje de error al usuario
                ViewData["ErrorMessage"] = $"Error al crear el contenedor: {ex.Message}";
            }
        }

        /// <summary>
        /// Method <c>Create</c> creates a Docker container. Sends to view the existing containers and images.
        /// </summary>
        /// <param name="image">The image to use to create the container</param>
        /// <param name="containerName">The container name</param>
        /// <param name="mappingPorts">The map of the exposed ports and it's binding to a it's host port</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateFormContainer(
            string image,
            string containerName,
            string mappingPorts,
            CancellationToken ct = default)
        {
            try
            {
                await PullImageIfNotExist(image, ct);

                var mappingPortsObject = JsonConvert.DeserializeObject<PortMapping[]>(mappingPorts);

                // Crear una nueva configuración para el contenedor
                var config = new Config
                {
                    AttachStdin = true,
                    AttachStdout = true,
                    AttachStderr = true,
                    OpenStdin = true,
                    Tty = true,
                };

                //Define the host configuration
                var hostConfig = new HostConfig { };
                var exposedPorts = new Dictionary<string, EmptyStruct> { };

                if (mappingPortsObject != null && mappingPortsObject.Any())
                {
                    var portBindings = new Dictionary<string, IList<PortBinding>> { };

                    foreach (var item in mappingPortsObject)
                    {
                        portBindings.Add(
                            item.ContainerPort!,
                            new List<PortBinding> {
                                new PortBinding { HostPort = item.HostPort }
                            }
                        );
                        exposedPorts.Add(item.ContainerPort!, default);
                    }

                    hostConfig = new HostConfig
                    {
                        PortBindings = portBindings,
                        AutoRemove = false,
                    };
                }
                else
                {
                    hostConfig = new HostConfig
                    {
                        PublishAllPorts = true,
                        AutoRemove = false,
                    };

                    exposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        { "80/tcp", default },
                    };
                }

                // Crear el contenedor
                var createdContainer = await this.imagesClient.ClientInstance.Containers.CreateContainerAsync(new CreateContainerParameters(config)
                {
                    Name = containerName,
                    Image = image,
                    ExposedPorts = exposedPorts,
                    HostConfig = hostConfig,
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
            var images = this.imagesClient.ClientInstance.Images.ListImagesAsync(new ImagesListParameters(), ct).Result;

            // Obtain the list of containers on the Docker server
            var containers = this.imagesClient.ClientInstance.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            }, ct).Result;

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
            await this.imagesClient.ClientInstance.Containers.StopContainerAsync(containerID,
                new ContainerStopParameters
                {
                    WaitBeforeKillSeconds = 10
                });
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RemoveContainer(string containerID)
        {
            await this.imagesClient.ClientInstance.Containers.RemoveContainerAsync(containerID,
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
