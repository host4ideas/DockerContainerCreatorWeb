using Docker.DotNet;
using Docker.DotNet.Models;
using DockerContainerCreatorWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                var images = await _client.Images.ListImagesAsync(new ImagesListParameters(), ct);

                // Check if the image is present on the machine
                var exists = images.Any(x => x.RepoTags.Contains(image));

                if (!exists)
                {
                    // Pull the image from the Docker registry
                    await _client.Images.CreateImageAsync(
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
                            item.ContainerPort,
                            new List<PortBinding> {
                                new PortBinding { HostPort = item.HostPort }
                            }
                        );
                        exposedPorts.Add(item.ContainerPort, default);
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
                var createdContainer = await _client.Containers.CreateContainerAsync(new CreateContainerParameters(config)
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
            var images = _client.Images.ListImagesAsync(new ImagesListParameters(), ct).Result;

            // Obtain the list of containers on the Docker server
            var containers = _client.Containers.ListContainersAsync(new ContainersListParameters
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
