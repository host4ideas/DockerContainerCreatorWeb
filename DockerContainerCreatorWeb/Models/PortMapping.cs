namespace DockerContainerCreatorWeb.Models
{
    public class PortMapping
    {
        public string? ContainerPort { get; set; }
        public string? HostPort { get; set; }
    }
}