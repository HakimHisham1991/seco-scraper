namespace SecoItemHarvester.Web.Dtos;

public class ProcessingStatusDto
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Processing { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
}
