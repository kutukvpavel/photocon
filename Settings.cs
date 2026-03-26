namespace photocon;

public class Settings
{
    public Settings()
    {

    }

    public string? SaveLocation { get; set; }
    public string? LoadLocation { get; set; }
    public string? ElectrometerConnectionString { get; set; }
    public string? FluidNcConnectionString { get; set; }
    public float BacklashCompensationNm { get; set; } = 0.1f; // nm
    public int FluidNcAutoRepoortIntervalMs { get; set; } = 100; // ms
}
