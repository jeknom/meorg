namespace MeOrg.Exceptions;

public class ErrorExitException : Exception
{
    public ExitCode Code { get; private init; }

    public ErrorExitException(ExitCode code, string message) : base(message)
    {
        Code = code;
    }
}