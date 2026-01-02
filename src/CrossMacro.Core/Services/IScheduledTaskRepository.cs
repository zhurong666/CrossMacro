using System.Collections.Generic;
using System.Threading.Tasks;
using CrossMacro.Core.Models;

namespace CrossMacro.Core.Services;

/// <summary>
/// Repository for managing scheduled tasks persistence
/// </summary>
public interface IScheduledTaskRepository
{
    /// <summary>
    /// Loads all scheduled tasks from storage
    /// </summary>
    Task<List<ScheduledTask>> LoadAsync();

    /// <summary>
    /// Saves all scheduled tasks to storage
    /// </summary>
    Task SaveAsync(IEnumerable<ScheduledTask> tasks);
}
