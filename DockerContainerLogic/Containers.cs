using Docker.DotNet.Models;
using DockerContainerLogic.Models;

namespace DockerContainerLogic
{
    public class Containers : DockerInstance
    {
        public IList<ContainerListResponse> GetContainers()
        {
            return this.ClientInstance.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            }).Result;
        }

        /// <summary>
        /// Checks if the desired image exist, if not it will be pulled from DockerHub.
        /// </summary>
        /// <param name="image"></param> Name of the image.
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task PullImageIfNotExist(string image, CancellationToken ct = default)
        {
            // List all images on the machine
            var images = await this.ClientInstance.Images.ListImagesAsync(new ImagesListParameters(), ct);

            // Check if the image is present on the machine
            var exists = images.Any(x => x.RepoTags.Contains(image));

            if (!exists)
            {
                // Pull the image from the Docker registry
                await this.ClientInstance.Images.CreateImageAsync(
                    new ImagesCreateParameters
                    {
                        FromImage = image,
                    },
                    null,
                    new Progress<JSONMessage>(m => Console.WriteLine(m.Status)), ct);
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
        public async Task<ResultModel> CreateFormContainer(
            string image,
            string containerName,
            PortMapping[] mappingPorts,
            CancellationToken ct = default)
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
                    Tty = true,
                };

                //Define the host configuration
                var hostConfig = new HostConfig { };
                var exposedPorts = new Dictionary<string, EmptyStruct> { };

                if (mappingPorts != null && mappingPorts.Any())
                {
                    var portBindings = new Dictionary<string, IList<PortBinding>> { };

                    foreach (var item in mappingPorts)
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
                var createdContainer = await this.ClientInstance.Containers.CreateContainerAsync(new CreateContainerParameters(config)
                {
                    Name = containerName,
                    Image = image,
                    ExposedPorts = exposedPorts,
                    HostConfig = hostConfig,
                }, ct);

                return new ResultModel("Contenedor creado con éxito!", false);
            }
            catch (Exception ex)
            {
                return new ResultModel($"Error al crear el contenedor: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Stops a running Docker container
        /// </summary>
        /// <param name="containerID"></param>
        /// <returns></returns>
        public async Task<ResultModel> StopContainer(string containerID)
        {
            try
            {
                // Stop and remove the container
                await this.ClientInstance.Containers.StopContainerAsync(containerID,
                    new ContainerStopParameters
                    {
                        WaitBeforeKillSeconds = 10
                    });
                return new ResultModel("Contenedor parado con éxito!", false);
            }
            catch (Exception ex)
            {
                return new ResultModel($"Error al parar el contenedor: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Deletes a existing container
        /// </summary>
        /// <param name="containerID"></param>
        /// <returns></returns>
        public async Task<ResultModel> RemoveContainer(string containerID)
        {
            try
            {
                await this.ClientInstance.Containers.RemoveContainerAsync(containerID,
                new ContainerRemoveParameters
                {
                    Force = true
                });
                return new ResultModel("Contenedor borrado con éxito!", false);
            }
            catch (Exception ex)
            {
                return new ResultModel($"Error al borrar el contenedor: {ex.Message}", true);
            }
        }
    }
}
