namespace WindowsTooling.Exceptions;

public class ProcessWrapperException : InvalidOperationException
{
    public ProcessWrapperException(
        string message,
        int exitCode,
        IList<string> standardError,
        IList<string> standardOutput) : base(message)
    {
        ExitCode = exitCode;
        StandardError = standardError;
        StandardOutput = standardOutput;
    }

    public int ExitCode { get; private set; }

    public IList<string> StandardError { get; private set; }

    public IList<string> StandardOutput { get; private set; }
}
