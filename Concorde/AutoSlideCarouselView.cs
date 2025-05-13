using System.Timers;
using Microsoft.Maui.Controls;

namespace Concorde;

public class AutoSlideCarouselView : CarouselView
{
    private System.Timers.Timer _timer;

    public int SlideInterval { get; set; } = 3000; // 3 seconds interval

    public AutoSlideCarouselView()
    {
        Loaded += AutoSlideCarouselView_Loaded;
        Unloaded += AutoSlideCarouselView_Unloaded;
    }

    private void AutoSlideCarouselView_Loaded(object sender, EventArgs e)
    {
        StartTimer();
    }

    private void AutoSlideCarouselView_Unloaded(object sender, EventArgs e)
    {
        StopTimer();
    }

    private void StartTimer()
    {
        if (ItemsSource == null)
            return;

        _timer = new System.Timers.Timer(SlideInterval);
        _timer.Elapsed += Timer_Elapsed;
        _timer.Start();
    }

    private void StopTimer()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Dispose();
            _timer = null;
        }
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (Dispatcher.IsDispatchRequired)
        {
            Dispatcher.Dispatch(() => MoveToNext());
        }
        else
        {
            MoveToNext();
        }
    }

    private void MoveToNext()
    {
        if (ItemsSource == null)
            return;

        var items = ItemsSource.Cast<object>().ToList();
        if (items.Count == 0)
            return;

        Position = (Position + 1) % items.Count;
    }
}