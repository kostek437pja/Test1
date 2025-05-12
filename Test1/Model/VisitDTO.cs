namespace Test1.Model;

public class VisitDTO
{
    public DateTime Date { get; set; }
    public ClientDTO client { get; set; }
    public MechanicDto mechanic { get; set; }
    public List<VisitServiceDto> visitServices { get; set; }
}

public class ClientDTO
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
}

public class MechanicDto
{
    public int MechanicId { get; set; }
    public string LicenceNumber { get; set; }
}

public class VisitServiceDto
{
    public string Name { get; set; }
    public Decimal ServiceFee { get; set; }
}