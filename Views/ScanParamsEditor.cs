using System;
using Avalonia.Controls;

namespace photocon.Views;

public partial class ScanParamsEditor : UserControl
{
    public ScanParamsEditor()
    {
        InitializeComponent();
        DataContextChanged += (o, e) => {
            Random.Shared.Next();
        };
    }
}