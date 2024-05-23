using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Windows;
using NexusMods.Icons;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageDesignViewModel : NewTabPageViewModel
{
    public NewTabPageDesignViewModel() : base(DesignWindowManager.Instance, CreateDesignData()) { }

    private static PageDiscoveryDetails[] CreateDesignData()
    {
        return Enumerable
            .Range(1, 9)
            .Select(i => new PageDiscoveryDetails
            {
                SectionName = $"Section {i % 3}",
                ItemName = $"Item {i}",
                PageData = null!,
                Icon = IconValues.Alert,
            })
            .ToArray();
    }
}
