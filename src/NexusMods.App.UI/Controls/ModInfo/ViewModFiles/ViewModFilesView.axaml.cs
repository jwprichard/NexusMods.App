﻿using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive.Disposables;
using System.Security.Cryptography;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.ReactiveUI;
using Humanizer.Bytes;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.ModInfo.ViewModFiles;
using ModFileNode = TreeNodeVM<IFileTreeNodeViewModel, GamePath>;

public partial class ViewModFilesView : ReactiveUserControl<IViewModFilesViewModel>
{
    public ViewModFilesView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            // Unleash the tree!
            ModFilesTreeDataGrid.Source = CreateTreeSource(ViewModel!.Items);
        });
    }
    
    private static HierarchicalTreeDataGridSource<ModFileNode> CreateTreeSource(
        ReadOnlyObservableCollection<ModFileNode> treeRoots)
    {
        return new HierarchicalTreeDataGridSource<ModFileNode>(treeRoots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<ModFileNode>(
                    new TemplateColumn<ModFileNode>(
                        Language.Helpers_GenerateHeader_NAME,
                        new FuncDataTemplate<ModFileNode>((node, _) =>
                            {
                                // This should never be null but can be during rapid resize, due to
                                // virtualization shenanigans. Think this is a control bug, but well, gotta work with what we have.
                                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                                if (node == null)
                                    return new Control();
                                    
                                // Very sus but it works, t r u s t.
                                var view = new FileTreeNodeView();
                                view!.DataContext = node.Item;
                                
                                // This is a 'hack' which allows us to receive events from the wrapping 'ModFileNode'
                                // and transfer it into the child IFileTreeNodeViewModel.
                                
                                // Given that we may reuse the `FileTreeNodeView` for other views in the future, 
                                // which may not necessarily need the wrapping `ModFileNode` to get info on when it's
                                // expanded or not expanded, this does not seem unreasonable to do.
                                ((IActivatableViewModel)node.Item).WhenActivated(d =>
                                {
                                    node.WhenAnyValue(x => x.IsExpanded)
                                        .Subscribe(isExpanded => node.Item.OnExpanded(isExpanded))
                                        .DisposeWith(d);
                                });
                                
                                return view;
                            }
                        ),
                        width: new GridLength(1, GridUnitType.Star),
                        options: new()
                        {
                            CompareAscending = (x, y) => string.Compare(x!.Item.Name, y!.Item.Name, StringComparison.OrdinalIgnoreCase),
                            CompareDescending = (x, y) => string.Compare(y!.Item.Name, x!.Item.Name, StringComparison.OrdinalIgnoreCase),
                        }
                    ),
                    node => node.Children,
                    null,  
                    node => node.IsExpanded),
                
                new TextColumn<ModFileNode,string?>(
                    Language.Helpers_GenerateHeader_SIZE,
                    x => ByteSize.FromBytes(x.Item.FileSize).ToString(),
                    options: new()
                    {
                        CompareAscending = (x, y) => x!.Item.FileSize.CompareTo(y!.Item.FileSize),
                        CompareDescending = (x, y) => y!.Item.FileSize.CompareTo(x!.Item.FileSize),
                    }
                ),
            }
        };
    }
}

