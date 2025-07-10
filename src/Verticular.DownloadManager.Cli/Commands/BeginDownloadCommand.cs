
public class BeginDownloadCommand
{
  public required ResourceLocation Location { get; init; }
  public required ResourceLocation[]? AlternativeLocations { get; init; }
  public required OutputOptions OutputOptions { get; init; }
}

public class OutputOptions
{
  private OutputOptions() { }

  public static OutputOptions SaveAsFile(string filePath)
  {
    return new() { OutputFilePath = filePath };
  }

  internal string? OutputFilePath { get; set; }
}
