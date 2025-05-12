namespace Test1.Model;

public class CreateVisitDTO
{
    public int VisitId { get; set; }
    public int ClientId { get; set; }
    public string MechanicLicenceNumber { get; set; }
    public List<CreateVisitServiceDto> services { get; set; }
}

public class CreateVisitServiceDto
{
    public string ServiceName { get; set; }
    public Decimal ServiceFee { get; set; }
}