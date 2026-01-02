using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using CrossMacro.Infrastructure.Helpers;
using CrossMacro.Infrastructure.Serialization;
using Serilog;

namespace CrossMacro.Infrastructure.Services;

public class JsonScheduledTaskRepository : IScheduledTaskRepository
{
    private static readonly ILogger Logger = Log.ForContext<JsonScheduledTaskRepository>();
    private readonly string _scheduleFilePath;

    public JsonScheduledTaskRepository() : this(PathHelper.GetConfigFilePath(ConfigFileNames.Schedules))
    {
    }

    public JsonScheduledTaskRepository(string scheduleFilePath)
    {
        _scheduleFilePath = scheduleFilePath;
    }

    public async Task<List<ScheduledTask>> LoadAsync()
    {
        try
        {
            if (!File.Exists(_scheduleFilePath)) 
                return new List<ScheduledTask>();
            
            var json = await File.ReadAllTextAsync(_scheduleFilePath);
            var tasks = JsonSerializer.Deserialize(json, CrossMacroJsonContext.Default.ListScheduledTask);
            
            return tasks ?? new List<ScheduledTask>();
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load scheduled tasks from {Path}", _scheduleFilePath);
            return new List<ScheduledTask>();
        }
    }

    public async Task SaveAsync(IEnumerable<ScheduledTask> tasks)
    {
        try
        {
            var directory = Path.GetDirectoryName(_scheduleFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonSerializer.Serialize(tasks.ToList(), CrossMacroJsonContext.Default.ListScheduledTask);
            await File.WriteAllTextAsync(_scheduleFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to save scheduled tasks to {Path}", _scheduleFilePath);
            throw; 
        }
    }
}
