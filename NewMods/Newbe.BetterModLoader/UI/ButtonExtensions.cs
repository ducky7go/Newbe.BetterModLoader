using UnityEngine.Events;

namespace Newbe.BetterModLoader.UI;

/// <summary>
/// Unity Button 的 R3 响应式扩展方法
/// </summary>
internal static class ButtonExtensions
{
    /// <summary>
    /// 将 Unity Button 的点击事件转换为 Observable 流
    /// 使用此方法可以用响应式方式订阅按钮点击事件
    /// </summary>
    /// <param name="button">要订阅的按钮</param>
    /// <returns>按钮点击事件的 Observable 流</returns>
    /// <example>
    /// <code>
    /// button.OnClickAsObservable()
    ///     .Subscribe(_ => DoSomething())
    ///     .AddTo(ref disposables);
    /// </code>
    /// </example>
    public static Observable<Unit> OnClickAsObservable(this Button button)
    {
        return Observable.Create<Unit>(Subscribe);

        IDisposable Subscribe(Observer<Unit> observer)
        {
            UnityAction onClick = () => observer.OnNext(Unit.Default);
            button.onClick.AddListener(onClick);

            return Disposable.Create(OnDisposed);

            void OnDisposed() => button.onClick.RemoveListener(onClick);
        }
    }

    /// <summary>
    /// 将 TMP_InputField 的 onValueChanged 事件转为 R3 Observable。
    /// </summary>
    public static Observable<string> OnValueChangedAsObservable(this TMP_InputField inputField)
    {
        IDisposable Subscribe(Observer<string> observer)
        {
            UnityAction<string> onValueChanged = observer.OnNext;
            inputField.onValueChanged.AddListener(onValueChanged);

            void OnDisposed() => inputField.onValueChanged.RemoveListener(onValueChanged);

            return Disposable.Create(OnDisposed);
        }

        return Observable.Create<string>(Subscribe);
    }

    public static Observable<bool> OnValueChangedAsObservable(this Toggle toggle)
    {
        IDisposable Subscribe(Observer<bool> observer)
        {
            UnityAction<bool> onValueChanged = observer.OnNext;
            toggle.onValueChanged.AddListener(onValueChanged);

            void OnDisposed() => toggle.onValueChanged.RemoveListener(onValueChanged);

            return Disposable.Create(OnDisposed);
        }

        return Observable.Create<bool>(Subscribe);
    }
}
