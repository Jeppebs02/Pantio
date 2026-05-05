namespace PantioAPI;

public class ExpiryCheckOptions
{
    public int NotificationThresholdDays { get; set; } = 3;
    public double IntervalHours { get; set; } = 24.0 / 5;
}
