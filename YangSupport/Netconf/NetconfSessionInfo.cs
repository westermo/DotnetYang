using System;
using System.Collections.Generic;

namespace YangSupport.Netconf;

/// <summary>
/// Represents the NETCONF session state and negotiated capabilities.
/// </summary>
public class NetconfSessionInfo
{
    public int SessionId { get; set; }
    public bool Base11 { get; set; }
    public bool WritableRunning { get; set; }
    public bool Candidate { get; set; }
    public bool ConfirmedCommit { get; set; }
    public bool RollbackOnError { get; set; }
    public bool Validate { get; set; }
    public bool Startup { get; set; }
    public bool Xpath { get; set; }
    public List<string> ServerCapabilities { get; set; } = new();
}

/// <summary>
/// Target datastore for NETCONF operations.
/// </summary>
public enum Datastore
{
    Running,
    Candidate,
    Startup
}

/// <summary>
/// Default operation for edit-config.
/// </summary>
public enum DefaultOperation
{
    Merge,
    Replace,
    None
}

/// <summary>
/// Error handling option for edit-config.
/// </summary>
public enum ErrorOption
{
    StopOnError,
    ContinueOnError,
    RollbackOnError
}
