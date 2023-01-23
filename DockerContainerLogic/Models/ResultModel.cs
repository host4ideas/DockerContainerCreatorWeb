namespace DockerContainerLogic.Models
{
    public class ResultModel
    {
        public string Error { get; set; }
        public bool IsError { get; set; }

        public ResultModel(string error, bool isError)
        {
            Error = error;
            IsError = isError;
        }
    }
}
