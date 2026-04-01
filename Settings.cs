namespace photocon;

public class Settings
{
    public Settings()
    {

    }

    public string? SaveLocation { get; set; }
    public string? LoadLocation { get; set; }
    public string ElectrometerIp { get; set; } = "192.168.1.200";
    public int ElectrometerPort { get; set; } = 3000;
    public string FluidNcIp { get; set; } = "192.168.1.200";
    public int FluidNcPort { get; set; } = 2000;
    public float BacklashCompensationNm { get; set; } = 0.1f; // nm
    public int FluidNcAutoReportIntervalMs { get; set; } = 150; // ms
    public int ElectrometerPollIntervalMs { get; set; } = 1000; //ms
}
