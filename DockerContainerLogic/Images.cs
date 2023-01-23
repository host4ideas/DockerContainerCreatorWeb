using Docker.DotNet;
using Docker.DotNet.Models;

namespace DockerContainerLogic
{
    public class Images
    {
        static public IList<ImagesListResponse> GetImages()
        {
            return DockerInstance.Instance.ClientInstance!.Images.ListImagesAsync(new ImagesListParameters()).Result;
        }
    }
}
