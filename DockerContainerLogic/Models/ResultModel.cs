namespace DockerContainerLogic.Models
{
    public class ResultModel
    {
        public string Message { get; set; }
        public bool IsError { get; set; }

        public ResultModel(string message, bool isError)
        {
            Message = message;
            IsError = isError;
        }
    }
}
