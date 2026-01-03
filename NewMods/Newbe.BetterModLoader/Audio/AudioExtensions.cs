namespace Newbe.BetterModLoader.Audio;

/// <summary>
/// UI组件音频扩展方法
/// </summary>
public static class AudioExtensions
{
    /// <summary>
    /// 为Toggle添加音效
    /// </summary>
    /// <param name="toggle">Toggle组件</param>
    /// <param name="onAudioEnabled">是否启用开启音效</param>
    /// <param name="onAudioDisabled">是否启用关闭音效</param>
    /// <returns>可释放的订阅对象</returns>
    public static IDisposable WithToggleAudio(this Toggle toggle, bool onAudioEnabled = true,
        bool onAudioDisabled = true)
    {
        if (toggle == null) return Disposable.Empty;

        var disposables = new CompositeDisposable();

        if (onAudioEnabled)
        {
            toggle.OnValueChangedAsObservable()
                .Where(isOn => isOn)
                .Subscribe(_ => BMLAudioManager.PlaySoundStatic(AudioEffectType.ToggleOn))
                .AddTo(disposables);
        }

        if (onAudioDisabled)
        {
            toggle.OnValueChangedAsObservable()
                .Where(isOn => !isOn)
                .Subscribe(_ => BMLAudioManager.PlaySoundStatic(AudioEffectType.ToggleOff))
                .AddTo(disposables);
        }

        return disposables;
    }

    /// <summary>
    /// 为Button添加点击音效
    /// </summary>
    /// <param name="button">Button组件</param>
    /// <returns>可释放的订阅对象</returns>
    public static IDisposable WithClickAudio(this Button button)
    {
        if (button == null) return Disposable.Empty;

        return button.OnClickAsObservable()
            .Subscribe(_ => BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick));
    }

    /// <summary>
    /// 为Button添加点击音效（条件版本）
    /// </summary>
    /// <param name="button">Button组件</param>
    /// <param name="shouldPlay">条件函数</param>
    /// <returns>可释放的订阅对象</returns>
    public static IDisposable WithClickAudio(this Button button, Func<bool> shouldPlay)
    {
        if (button == null || shouldPlay == null) return Disposable.Empty;

        return button.OnClickAsObservable()
            .Subscribe(_ =>
            {
                if (shouldPlay())
                {
                    BMLAudioManager.PlaySoundStatic(AudioEffectType.ButtonClick);
                }
            });
    }

    /// <summary>
    /// 为可拖拽项添加音效
    /// </summary>
    /// <param name="eventTrigger">EventTrigger组件</param>
    /// <returns>可释放的订阅对象</returns>
    public static IDisposable WithDragAudio(this EventTrigger eventTrigger)
    {
        if (eventTrigger == null) return Disposable.Empty;

        var disposables = new CompositeDisposable();

        // 拖拽开始音效
        var beginDragEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.BeginDrag
        };
        beginDragEntry.callback.AddListener(_ => BMLAudioManager.PlaySoundStatic(AudioEffectType.DragStart));
        eventTrigger.triggers.Add(beginDragEntry);

        // 拖拽结束音效
        var endDragEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.EndDrag
        };
        endDragEntry.callback.AddListener(_ => BMLAudioManager.PlaySoundStatic(AudioEffectType.DragRelease));
        eventTrigger.triggers.Add(endDragEntry);

        // 清理方法
        var cleanup = Disposable.Create(() =>
        {
            eventTrigger.triggers.Remove(beginDragEntry);
            eventTrigger.triggers.Remove(endDragEntry);
        });

        cleanup.AddTo(disposables);

        return disposables;
    }
}
