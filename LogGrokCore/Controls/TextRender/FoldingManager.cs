using System;
using System.Collections.Generic;

namespace LogGrokCore.Controls.TextRender;

public class FoldingManager
{
    private readonly CollapsibleRegionsMachine _thisTextViewMachine;
    private readonly TextViewSharedFoldingState _textViewSharedFoldingState;

    public FoldingManager(CollapsibleRegionsMachine thisTextViewMachine, 
        TextViewSharedFoldingState textViewSharedFoldingState,
        Func<HashSet<int>> defaultSettingsGetter)
    {
        _thisTextViewMachine = thisTextViewMachine;
        _textViewSharedFoldingState = textViewSharedFoldingState;

        ExpandRecursivelyCommand = new DelegateCommand(thisTextViewMachine.ExpandRecursively, 
            thisTextViewMachine.HasCollapsedRegions);

        CollapseRecursivelyCommand = new DelegateCommand(thisTextViewMachine.CollapseRecursively,
            thisTextViewMachine.HasExpandedRegions);

        ResetToDefaultCommand = new DelegateCommand(() => thisTextViewMachine.UpdateCollapsedLines(
            defaultSettingsGetter()));

        ExpandAllCommand = new DelegateCommand(_textViewSharedFoldingState.ExpandAll);
        CollapseAllCommand = new DelegateCommand(_textViewSharedFoldingState.CollapseAll);
        ResetAllToDefaultCommand = new DelegateCommand(_textViewSharedFoldingState.ResetAllToDefault);
    }

    public DelegateCommand? ExpandRecursivelyCommand { get; }

    public DelegateCommand? CollapseRecursivelyCommand { get; }

    public DelegateCommand? ResetToDefaultCommand { get; }
    
    public DelegateCommand? ExpandAllCommand { get; }
    
    public DelegateCommand? CollapseAllCommand { get; }

    public DelegateCommand? ResetAllToDefaultCommand { get; }
}