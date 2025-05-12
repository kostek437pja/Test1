using Test1.Model;

namespace Test1.Service;

public interface IDBService
{
    Task<VisitDTO> GetVisit(int visitId);
    Task CreateVisit(CreateVisitDTO createVisitDTO);
}