namespace Hagalaz.Services.Abstractions;

public interface IStartupTaskState
{
    void MarkStarted();
    void MarkCompleted();
    void MarkFailed();
}
