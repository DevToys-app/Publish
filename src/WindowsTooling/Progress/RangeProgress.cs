namespace WindowsTooling.Progress;

public class RangeProgress : IProgress<ProgressData>
{
    private readonly IProgress<ProgressData> progress;
    private readonly int min;
    private readonly int max;

    public RangeProgress(IProgress<ProgressData> progress, int min, int max)
    {
        if (min < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(min), @"Minimum must be greater or equal to 0.");
        }

        if (max > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(max), @"Maximum must be lower than or equal to 100.");
        }

        if (min > max)
        {
            throw new ArgumentOutOfRangeException(nameof(min), @"Minimum must not be greater than maximum.");
        }

        this.progress = progress;
        this.min = min;
        this.max = max;
    }

    public void Report(ProgressData value)
    {
        if (progress == null)
        {
            // accept null progress.
            return;
        }

        double realProgress = min + value.Progress / 100.0 * (max - min);
        progress.Report(new ProgressData((int)realProgress, value.Message));
    }
}
