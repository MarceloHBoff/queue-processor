namespace Worker.Interface;

public interface IWorker
{
    Task ProcessAsync(string message);
}
