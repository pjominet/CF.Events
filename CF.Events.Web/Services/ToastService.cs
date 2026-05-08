using System.Timers;

namespace CF.Events.Web.Services;

public enum ToastType
{
    Success,
    Error,
    Info
}

public class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Message { get; init; } = string.Empty;
    public ToastType Type { get; init; } = ToastType.Info;
}

public class ToastService : IDisposable
{
    public event Action? OnChange;
    private readonly List<ToastMessage> toasts = [];
    private readonly System.Timers.Timer timer;

    public ToastService()
    {
        timer = new System.Timers.Timer(5000);
        timer.Elapsed += OnTimerElapsed;
        timer.AutoReset = true;
        timer.Start();
    }

    public List<ToastMessage> GetToasts() => toasts.ToList();

    public void Show(string message, ToastType type = ToastType.Info)
    {
        var toast = new ToastMessage { Message = message, Type = type };
        lock (toasts)
        {
            toasts.Add(toast);
        }
        NotifyStateChanged();

        // Auto-remove after 5 seconds
        Task.Delay(5000).ContinueWith(_ => Remove(toast.Id));
    }

    public void Remove(Guid id)
    {
        lock (toasts)
        {
            var toast = toasts.FirstOrDefault(t => t.Id == id);
            if (toast is null) return;

            toasts.Remove(toast);
            NotifyStateChanged();
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // This is a fallback, Task.Delay should handle it better for per-toast timing
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void Dispose()
    {
        timer.Dispose();
    }
}
