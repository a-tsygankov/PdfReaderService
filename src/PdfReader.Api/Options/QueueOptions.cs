namespace PdfReader.Api.Options;

public sealed class QueueOptions
{
    public const string SectionName = "Queue";

    public string HostName { get; set; } = "localhost";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string QueueName { get; set; } = "pdfreader.documents";
}
