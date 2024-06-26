using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Downloaders.Tasks.State;
using ReactiveUI;
using RocksDbSharp;

namespace NexusMods.App.UI.Pages.ModLibrary;

public class FileOriginsPageViewModel : APageViewModel<IFileOriginsPageViewModel>, IFileOriginsPageViewModel
{
    private readonly IConnection _conn;
    private readonly IRepository<DownloadAnalysis.Model> _dlAnalysisRepo;
    private readonly IArchiveInstaller _archiveInstaller;

    public ReadOnlyObservableCollection<IFileOriginEntryViewModel> FileOrigins => _fileOrigins;
    private ReadOnlyObservableCollection<IFileOriginEntryViewModel> _fileOrigins = new([]);

    public LoadoutId LoadoutId { get; private set; }


    public FileOriginsPageViewModel(
        LoadoutId loadoutId,
        IArchiveInstaller archiveInstaller,
        IRepository<DownloadAnalysis.Model> downloadAnalysisRepository,
        IConnection conn,
        IWindowManager windowManager) : base(windowManager)
    {
        _conn = conn;
        _archiveInstaller = archiveInstaller;
        _dlAnalysisRepo = downloadAnalysisRepository;

        TabTitle = Language.FileOriginsPageTitle;
        TabIcon = IconValues.ModLibrary;

        LoadoutId = loadoutId;

        var loadout = _conn.Db.Get<Loadout.Model>(LoadoutId.Value);
        var game = loadout.Installation.Game;

        var entriesObservable = downloadAnalysisRepository.Observable
                .ToObservableChangeSet()
                .Filter(model => FilterDownloadAnalysisModel(model, game.Domain))
                .OnUI()
                .Transform(fileOrigin => (IFileOriginEntryViewModel)new FileOriginEntryViewModel(
                        _conn,
                        _archiveInstaller,
                        LoadoutId,
                        fileOrigin
                    )
                )
                .Bind(out _fileOrigins);

        this.WhenActivated(d =>
        {
            entriesObservable.SubscribeWithErrorLogging().DisposeWith(d);
        });
    }

    public static bool FilterDownloadAnalysisModel(DownloadAnalysis.Model model, GameDomain currentGameDomain)
    {
        if (!model.TryGet(DownloaderState.GameDomain, out var domain)) return false;
        if (domain != currentGameDomain) return false;
        if (model.Contains(StreamBasedFileOriginMetadata.StreamBasedOrigin)) return false;
        return true;
    }
}
