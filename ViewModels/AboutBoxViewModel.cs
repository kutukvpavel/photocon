using System;

namespace photocon.ViewModels;

public class AboutBoxViewModel : ViewModelBase
{
    public AboutBoxViewModel()
    {

    }

    public string CopyrightString => $"Photoconductivity spectra acquisition application{Environment.NewLine}Kutukov Pavel, 2024-{DateTime.Today.Year}";
}
