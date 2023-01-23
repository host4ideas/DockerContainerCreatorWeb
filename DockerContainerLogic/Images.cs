using Docker.DotNet.Models;

namespace DockerContainerLogic
{
    internal class Images : DockerInstance
    {
        public IList<ImagesListResponse> GetImages()
        {
            return this.ClientInstance.Images.ListImagesAsync(new ImagesListParameters()).Result;
        }
    }
}
