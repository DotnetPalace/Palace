namespace Palace.Shared;

public enum ServiceState
{
    Offline = 0,
    NotResponding = 1,
    NotExists = 2,
    NotInstalled = 3,
    Starting = 4,
    StartFail = 5,
    Started = 6,
    UpdateDetected = 7,
    UpdateInProgress = 8,
    Updated = 9,
    InstallationFailed = 10,
    NotExitedAfterStop = 11,
    Removed = 12,
    InstallationInProgress = 13,
    UninstallFailed = 14,
    Running = 15,
    Stopping = 16,
    TryToStop = 17,
	ForceInnerKill = 18,
    Down = 19
}