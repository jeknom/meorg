namespace MeOrg;

public interface IConsole
{
    void WriteInfoLine(string message);
    void WriteErrorLine(string message);
    void WriteException(Exception ex);
    void WriteReport();
    void WriteInputs(string source, string target, int dayOffset, bool dedupe, bool promptUser);
    Task<bool> Confirm(string question, CancellationToken cancellationToken);
}
