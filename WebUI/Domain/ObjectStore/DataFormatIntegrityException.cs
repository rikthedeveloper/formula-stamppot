namespace WebUI.Domain.ObjectStore;

public class DataFormatIntegrityException : Exception
{
    const string _message = "Data retrieved from the database was incorectly formatted.";

    public DataFormatIntegrityException()
        : base(_message)
    {
    }
}