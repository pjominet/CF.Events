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
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Info;
}

public class ToastService : IDisposable
{
    public event Action? OnChange;
    private readonly List<ToastMessage> _toasts = new();
    private readonly System.Timers.Timer _timer;

    public ToastService()
    {
        _timer = new System.Timers.Timer(5000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();
    }

    public List<ToastMessage> GetToasts() => _toasts.ToList();

    public void Show(string message, ToastType type = ToastType.Info)
    {
        var toast = new ToastMessage { Message = message, Type = type };
        lock (_toasts)
        {
            _toasts.Add(toast);
        }
        NotifyStateChanged();

        // Auto-remove after 5 seconds
        Task.Delay(5000).ContinueWith(_ => Remove(toast.Id));
    }

    public void Remove(Guid id)
    {
        lock (_toasts)
        {
            var toast = _toasts.FirstOrDefault(t => t.Id == id);
            if (toast != null)
            {
                _toasts.Remove(toast);
                NotifyStateChanged();
            }
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // This is a fallback, Task.Delay should handle it better for per-toast timing
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void Dispose()
    {
        _timer.Dispose();
    }
}
