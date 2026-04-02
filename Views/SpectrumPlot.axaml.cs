using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using photocon.Models;
using ScottPlot;

namespace photocon.Views;

public partial class SpectrumPlot : UserControl
{
    protected ScottPlot.Plottables.DataLogger PositionalPlot;
    protected ScottPlot.Plottables.DataLogger TimeDomainPlot;
    protected ScottPlot.Plottables.DataLogger TimeDiscrPlot;
    protected IYAxis TimeDiscrAxis;
    protected ScottPlot.AxisPanels.DateTimeXAxis TimeAxis;
    protected Spectrum? LastDataContext = null;
    protected Color DefaultFigureBkgColor;
    protected Color DefaultDataBkgColor;
    protected Color DefaultAxesColor;
    protected Color DefaultGridColor;
    protected Color DefaultLegendBackgroundColor;
    protected Color? DefaultLegendFontColor;
    protected Color DefaultLegendOutlineColor;

    protected void DataContext_Changed(object? sender, EventArgs e)
    {
        if (LastDataContext != null) LastDataContext.DataChanged -= OnDataChanged;
        LastDataContext = DataContext as Spectrum;
        if (LastDataContext == null) return;
        LastDataContext.DataChanged += OnDataChanged;
        OnDataChanged(this, new Spectrum.DataChangedEventArgs(Spectrum.DataChange.Cleared));
    }

    protected void OnDataChanged(object? sender, Spectrum.DataChangedEventArgs e)
    {
        switch (e.ChangeType)
        {
            case Spectrum.DataChange.PointAdded:
                if (e.PositionDomain != null) PositionalPlot.Add(e.PositionDomain.Value.Key, e.PositionDomain.Value.Value);
                if (e.TimeDomain != null) PositionalPlot.Add(e.TimeDomain.Value.Key.ToOADate(), e.TimeDomain.Value.Value);
                if (e.TimeDiscrepancy != null) PositionalPlot.Add(e.TimeDiscrepancy.Value.Key.ToOADate(), e.TimeDiscrepancy.Value.Value);
                break;
            case Spectrum.DataChange.Cleared:
                PositionalPlot.Clear();
                TimeDiscrPlot.Clear();
                TimeDomainPlot.Clear();
                break;
            default: break;
        }
        if (chkAutoscaleX.IsChecked ?? false)
        {
            Plot1.Plot.Axes.AutoScaleExpandX();
            Plot1.Plot.Axes.AutoScaleExpandX(TimeAxis);
        }
        if (chkAutoscaleY.IsChecked ?? false)
        {
            Plot1.Plot.Axes.AutoScaleY();
        }
        Plot1.Refresh();
    }

    protected void OnActivePlotsChanged(object? sender, RoutedEventArgs e)
    {
        if (LastDataContext == null) return;
        PositionalPlot.IsVisible = LastDataContext.EnablePositionalDomain;
        TimeDomainPlot.IsVisible = LastDataContext.EnableTimeDomain;
        TimeDiscrPlot.IsVisible = LastDataContext.EnableTimeDiscrepancy;
        TimeAxis.IsVisible = TimeDomainPlot.IsVisible || TimeDiscrPlot.IsVisible;
        TimeDiscrAxis.IsVisible = TimeDiscrPlot.IsVisible;
        Plot1.Plot.Axes.Left.IsVisible = PositionalPlot.IsVisible || TimeDiscrPlot.IsVisible;
        Plot1.Plot.Axes.Bottom.IsVisible = PositionalPlot.IsVisible;
    }

    protected void ResetScale_Click(object? sender, RoutedEventArgs e)
    {
        if (LastDataContext == null) return;
        var xlim = LastDataContext.GetPositionAxisLimits();
        Plot1.Plot.Axes.AutoScale();
        Plot1.Plot.Axes.SetLimits(xlim.Item1, xlim.Item2);
    }

    protected void ActualThemeVariant_Changed(object? sender, EventArgs e)
    {
        if (App.IsDarkThemed)
        {
            // change figure colors
            Plot1.Plot.FigureBackground.Color = Color.FromHex("#181818");
            Plot1.Plot.DataBackground.Color = Color.FromHex("#1f1f1f");
            // change axis and grid colors
            Plot1.Plot.Axes.Color(Color.FromHex("#d7d7d7"));
            Plot1.Plot.Grid.MajorLineColor = Color.FromHex("#404040");
            // change legend colors
            Plot1.Plot.Legend.BackgroundColor = Color.FromHex("#404040");
            Plot1.Plot.Legend.FontColor = Color.FromHex("#d7d7d7");
            Plot1.Plot.Legend.OutlineColor = Color.FromHex("#d7d7d7");   
        }
        else
        {
            // change figure colors
            Plot1.Plot.FigureBackground.Color = DefaultFigureBkgColor;
            Plot1.Plot.DataBackground.Color = DefaultDataBkgColor;
            // change axis and grid colors
            Plot1.Plot.Axes.Color(DefaultAxesColor);
            Plot1.Plot.Grid.MajorLineColor = DefaultGridColor;
            // change legend colors
            Plot1.Plot.Legend.BackgroundColor = DefaultLegendBackgroundColor;
            Plot1.Plot.Legend.FontColor = DefaultLegendFontColor;
            Plot1.Plot.Legend.OutlineColor = DefaultLegendOutlineColor;
        }
        Plot1.Refresh();
    }

    public SpectrumPlot()
    {
        InitializeComponent();
        DefaultAxesColor = Plot1.Plot.Axes.Bottom.FrameLineStyle.Color;
        DefaultDataBkgColor = Plot1.Plot.DataBackground.Color;
        DefaultFigureBkgColor = Plot1.Plot.FigureBackground.Color;
        DefaultGridColor = Plot1.Plot.Grid.MajorLineColor;
        DefaultLegendBackgroundColor = Plot1.Plot.Legend.BackgroundColor;
        DefaultLegendFontColor = Plot1.Plot.Legend.FontColor;
        DefaultLegendOutlineColor = Plot1.Plot.Legend.OutlineColor;

        if (Application.Current != null) Application.Current.ActualThemeVariantChanged += ActualThemeVariant_Changed;
        DataContextChanged += DataContext_Changed;

        TimeDiscrAxis = Plot1.Plot.Axes.Right;
        TimeAxis = new ScottPlot.AxisPanels.DateTimeXAxis(); 
        Plot1.Plot.Axes.AddXAxis(TimeAxis);
        PositionalPlot = Plot1.Plot.Add.DataLogger();
        TimeDomainPlot = Plot1.Plot.Add.DataLogger();
        TimeDomainPlot.Axes.XAxis = TimeAxis;
        TimeDiscrPlot = Plot1.Plot.Add.DataLogger();
        TimeDiscrPlot.Axes.XAxis = TimeAxis;
        TimeDiscrPlot.Axes.YAxis = TimeDiscrAxis;
    }
}