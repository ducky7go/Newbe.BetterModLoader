namespace Newbe.BetterModLoader.DeveloperInfos;

internal partial class DeveloperInfoVm : IDisposable
{
    private DeveloperInfoData _data;

    private DisposableBag _disposableBag;

    private readonly ReactiveProperty<double> _LogDirectorySizeInMB;
    private readonly ReactiveProperty<DeveloperInfoData> _developerInfoData;
    private readonly ReactiveProperty<bool> _forceRestartOnOrderChanges;

    public Observable<double> LogDirectorySizeInMB { get; }
    public Observable<DeveloperInfoData> DeveloperInfoData { get; }
    public Observable<bool> ForceRestartOnOrderChanges { get; }

    public DeveloperInfoVm(DeveloperInfoData data)
    {
        _data = data;

        _LogDirectorySizeInMB = new ReactiveProperty<double>(data.LogDirectorySizeInMB)
            .AddTo(ref _disposableBag);
        _developerInfoData = new ReactiveProperty<DeveloperInfoData>(data)
            .AddTo(ref _disposableBag);

        // Initialize force restart setting from Configs
        var initialForceRestartValue = Configs.ForceRestartOnOrderChanges;
        _forceRestartOnOrderChanges = new ReactiveProperty<bool>(initialForceRestartValue)
            .AddTo(ref _disposableBag);

        // Subscribe to force restart changes to persist them (but skip initial value to avoid double save)
        _forceRestartOnOrderChanges
            .Skip(1) // Skip initial value since SetForceRestartOnOrderChanges already saves it
            .DistinctUntilChanged()
            .Subscribe(value =>
            {
                Log.Debug($"[DeveloperInfoVm] Force restart setting changed via reactive subscription: {value}");
                Configs.ForceRestartOnOrderChanges = value;
            })
            .AddTo(ref _disposableBag);

        LogDirectorySizeInMB = _LogDirectorySizeInMB
            .DistinctUntilChanged()
            .AsObservable();
        DeveloperInfoData = _developerInfoData
            .DistinctUntilChanged()
            .AsObservable();
        ForceRestartOnOrderChanges = _forceRestartOnOrderChanges
            .AsObservable();
    }

    public void SetForceRestartOnOrderChanges(bool value)
    {
        _forceRestartOnOrderChanges.Value = value;
        // Also persist immediately to ensure it's saved
        Configs.ForceRestartOnOrderChanges = value;
    }

    public void Dispose()
    {
        _disposableBag.Dispose();
    }
}
