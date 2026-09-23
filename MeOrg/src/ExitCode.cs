namespace MeOrg;

public enum ExitCode
{
    Success = 0,
    Unexpected = 1,
    PermissionDenied = 126,
    Cancelled = 130,
    TooManyFailedCopies = 500,
    DirectoriesNotInSync = 501,
    HashNotEqual = 502
}
