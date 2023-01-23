using Docker.DotNet;
using System.Runtime.InteropServices;

namespace DockerContainerLogic
{
    public class DockerInstance
    {
        private static Lazy<DockerInstance>? instance;

        public static DockerInstance Instance
        {
            get { return instance!.Value; }
        }

        public DockerClient? ClientInstance;

        private DockerInstance()
        {
            ClientInstance = CreateDockerInstance();
        }

        public static void Init()
        {
            var initTask = Task.Factory.StartNew(() =>
            {
                return new DockerInstance();
            });

            instance = new Lazy<DockerInstance>(() => initTask.Result);
        }

        #region METHODS 
        private static DockerClient CreateDockerInstance()
        {
            var IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            string DockerApiUri()
            {
                if (IsWindows)
                    return "npipe://./pipe/docker_engine";

                if (IsLinux)
                    return "unix:///var/run/docker.sock";

                throw new Exception(
                    "Was unable to determine what OS this is running on, does not appear to be Windows or Linux!?");
            }

            // Agregar el cliente de Docker como un servicio
            return new DockerClientConfiguration(
                 new Uri(DockerApiUri()))
                  .CreateClient();
        }

        public bool CheckDockerService()
        {
            try
            {
                this.ClientInstance.System.PingAsync().Wait();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
    }
}
