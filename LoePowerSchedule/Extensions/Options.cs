namespace LoePowerSchedule.Extensions;

public class AzureVisionOptions
{
    public string Endpoint { get; set; } 
    public string Key { get; set; }  
}

public class MongoDbOptions
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
}

public class BrowserOptions
{
    public string BrowserUrl { get; set; }
}

public class ScrapeOptions
{
    public int ImportPeriodSec { get; set; }
}

public class ImportOptions
{
    public string ImportUrl { get; set; }
    public string ImportClassName { get; set; }
}