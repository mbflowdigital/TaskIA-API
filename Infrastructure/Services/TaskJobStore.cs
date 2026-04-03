using System.Collections.Concurrent;

namespace Infrastructure.Services;

public enum TaskJobStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public class TaskJobResult
{
    public TaskJobStatus Status { get; set; } = TaskJobStatus.Pending;
    public int TasksCreated { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Armazena em memória o estado de jobs assíncronos de geração de tarefas.
/// Registrado como Singleton.
/// </summary>
public class TaskJobStore
{
    private readonly ConcurrentDictionary<string, TaskJobResult> _jobs = new();

    public string CreateJob()
    {
        var id = Guid.NewGuid().ToString("N");
        _jobs[id] = new TaskJobResult { Status = TaskJobStatus.Pending };
        return id;
    }

    public void SetRunning(string id)
    {
        if (_jobs.TryGetValue(id, out var job))
            job.Status = TaskJobStatus.Running;
    }

    public void SetCompleted(string id, int tasksCreated)
    {
        _jobs[id] = new TaskJobResult { Status = TaskJobStatus.Completed, TasksCreated = tasksCreated };
    }

    public void SetFailed(string id, string errorMessage)
    {
        _jobs[id] = new TaskJobResult { Status = TaskJobStatus.Failed, ErrorMessage = errorMessage };
    }

    public TaskJobResult? Get(string id) =>
        _jobs.TryGetValue(id, out var job) ? job : null;
}
