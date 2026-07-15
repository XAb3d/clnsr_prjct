namespace CleanserBlazorUI.Helpers;

public class StreamHelper
{ /// <summary>
  /// Converts the length of a stream to megabytes (MB) and returns the result as a long.
  /// </summary>
  /// <param name="long">The length you want to convert.(Convert file stream size in long to MB)</param>
  /// <returns>The size of the stream in MB as a long (whole megabytes).</returns>
    public string? ConvertLongToMB(long? streamSize)
    {
        double lengthInKB = streamSize / 1024.0 ?? 0;
        try
        {
            if (lengthInKB < 1024)
            {
                return $"{Math.Floor(lengthInKB).ToString()}MB";  // Return size in kilobytes (rounded down)
            }
            else
            {
                double lengthInMB = lengthInKB / 1024.0;  // Convert to MB
                return $"{Math.Floor(lengthInMB).ToString()}KB";  // Return size in megabytes (rounded down)
            }
        }
        catch (Exception)
        {
            throw new ArgumentException("cannot covert value to to double", nameof(streamSize));
        }
    }
}
