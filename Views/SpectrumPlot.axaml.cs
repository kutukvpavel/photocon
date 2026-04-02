using System;
using Avalonia.Controls;

namespace photocon.Views;

public partial class SpectrumPlot : UserControl
{
    public SpectrumPlot()
    {
        InitializeComponent();
    }

    public void UpdatePlot()
    {
        Plot1.Refresh();
    }
}