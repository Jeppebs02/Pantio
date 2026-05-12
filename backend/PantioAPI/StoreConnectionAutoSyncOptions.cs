namespace PantioAPI;

public class StoreConnectionAutoSyncOptions
{
    public const string Section = "StoreConnectionAutoSync";

    public double IntervalHours { get; set; } = 6;
}
