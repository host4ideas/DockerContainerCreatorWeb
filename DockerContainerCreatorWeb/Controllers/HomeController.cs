using Docker.DotNet.Models;
using DockerContainerCreatorWeb.Models;
using DockerContainerLogic;
using DockerContainerLogic.Models;
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
                if (this.imagesClient.CheckDockerService() != true)
                {
                    ViewData["ErrorMessage"] = $"No se pudo conectar al Docker daemon. Compruebe que Docker se está ejecutando con normalidad";
                    return View();
                }

                // Add the list of images and containers to the view data
                ViewData["images"] = this.imagesClient.GetImages();
                ViewData["containers"] = this.containersClient.GetContainers();
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = $"No se pudo recoger la información de imágenes ni contenedores:<br />{ex.Message}";
            }

            return View();
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
            var mappingPortsObject = JsonConvert.DeserializeObject<PortMapping[]>(mappingPorts)!;

            ResultModel result = await containersClient.CreateContainer(image: image, containerName: containerName, mappingPorts: mappingPortsObject, ct: ct);

            if (result.IsError == true)
            {
                ViewData["ErrorMessage"] = $"No se pudo crear el contenedor:<br />{result.Message}";
                return View("Index");
            }

            // Add the list of images and containers to the view data
            ViewData["images"] = this.imagesClient.GetImages();
            ViewData["containers"] = this.containersClient.GetContainers();
            ViewData["Message"] = result.Message;

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
