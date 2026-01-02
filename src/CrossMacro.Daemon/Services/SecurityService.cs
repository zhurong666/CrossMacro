using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using CrossMacro.Daemon.Security; // For PeerCredentials, AuditLogger, RateLimiter
using Serilog;

namespace CrossMacro.Daemon.Services;

public class SecurityService : ISecurityService
{
    private readonly RateLimiter _rateLimiter;
    private readonly AuditLogger _auditLogger;
    
    // Stateless implementation of validation
    
    public SecurityService()
    {
        _rateLimiter = new RateLimiter(maxConnectionsPerWindow: 10, windowSeconds: 60, banSeconds: 60);
        _auditLogger = new AuditLogger();
    }

    public async Task<(uint Uid, int Pid)?> ValidateConnectionAsync(Socket client)
    {
        // Get peer credentials
        var creds = PeerCredentials.GetCredentials(client);
        if (creds == null)
        {
            Log.Warning("[Security] Failed to get peer credentials, rejecting connection");
            _auditLogger.LogSecurityViolation(0, 0, "PEER_CRED_FAILED");
            client.Dispose();
            return null;
        }
        
        var (uid, gid, pid) = creds.Value;
        var executable = PeerCredentials.GetProcessExecutable(pid);
        
        Log.Information("Client connected: UID={Uid}, GID={Gid}, PID={Pid}, Exe={Exe}", 
            uid, gid, pid, executable ?? "unknown");
        
        // Reject root connections (unless configured otherwise, but default security policy says no)
        if (uid == 0)
        {
            Log.Warning("[Security] Root connection rejected (UID=0)");
            _auditLogger.LogConnectionAttempt(uid, pid, executable, false, "ROOT_REJECTED");
            client.Dispose();
            return null;
        }
        
        // Rate limiting
        if (_rateLimiter.IsRateLimited(uid))
        {
            Log.Warning("[Security] UID {Uid} is rate limited", uid);
            _auditLogger.LogRateLimited(uid, pid);
            client.Dispose();
            return null;
        }
        
        // Check group membership
        if (!PeerCredentials.IsUserInGroup(uid, "crossmacro"))
        {
            Log.Warning("[Security] UID {Uid} is not in 'crossmacro' group", uid);
            _auditLogger.LogConnectionAttempt(uid, pid, executable, false, "NOT_IN_GROUP");
            client.Dispose();
            return null;
        }
        
        // Polkit authorization
        var polkitAuthorized = await PolkitChecker.CheckAuthorizationAsync(
            uid, pid, PolkitChecker.Actions.InputCapture);
        
        if (!polkitAuthorized)
        {
            Log.Warning("[Security] Polkit authorization denied for UID {Uid}", uid);
            _auditLogger.LogConnectionAttempt(uid, pid, executable, false, "POLKIT_DENIED");
            client.Dispose();
            return null;
        }
        
        // Success
        _auditLogger.LogConnectionAttempt(uid, pid, executable, true);
        _rateLimiter.RecordSuccess(uid);
        
        return (uid, pid);
    }

    public void LogDisconnect(uint uid, int pid, TimeSpan duration)
    {
        _auditLogger.LogDisconnect(uid, pid, duration);
        Log.Information("Client disconnected (session: {Duration}s)", duration.TotalSeconds);
    }

    public void LogCaptureStart(uint uid, int pid, bool mouse, bool kb)
    {
        _auditLogger.LogCaptureStart(uid, pid, mouse, kb);
    }

    public void LogCaptureStop(uint uid, int pid)
    {
        _auditLogger.LogCaptureStop(uid, pid);
    }


}
