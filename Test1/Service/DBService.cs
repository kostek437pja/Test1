using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Test1.Exceptions;
using Test1.Model;

namespace Test1.Service;

public class DBService : IDBService
{
    private readonly string  connectionString = "Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True";
    
    public async Task<VisitDTO> GetVisit(int visitId)
    {
        SqlConnection connection = new SqlConnection(connectionString);
        SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        
        await connection.OpenAsync();
        
        command.CommandText = @"SELECT count(*) FROM Visit WHERE visit_id = @visitId";
        command.Parameters.AddWithValue("@visitId", visitId);
        
        if ((int)await command.ExecuteScalarAsync() < 1)
        {
            throw new NotFoundException($"Visit with id {visitId} not found");
        }
        
        command.CommandText = @"SELECT * FROM Visit v INNER JOIN Client c ON v.client_id = c.client_id 
                                Inner JOIN Mechanic m on v.mechanic_id = m.mechanic_id where v.visit_id = @visitId";
        
        
        VisitDTO visit = new VisitDTO();
        using (var reader = await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                visit.Date = reader.GetDateTime(reader.GetOrdinal("date"));
                visit.client = new ClientDTO
                {
                    FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                    LastName = reader.GetString(reader.GetOrdinal("last_name")),
                    DateOfBirth = reader.GetDateTime(reader.GetOrdinal("date_of_birth"))
                };
                visit.mechanic = new MechanicDto
                {
                    MechanicId = reader.GetInt32(reader.GetOrdinal("mechanic_id")),
                    LicenceNumber = reader.GetString(reader.GetOrdinal("licence_number"))
                };
                visit.visitServices = new List<VisitServiceDto>();
            }
        }
        
        command.Parameters.Clear();
        command.CommandText = @"SELECT * FROM Visit_Service v INNER JOIN Service s on v.service_id = s.service_id where v.visit_id = @visitId";
        command.Parameters.AddWithValue("@visitId", visitId);
        
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                visit.visitServices.Add(new VisitServiceDto
                {
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    ServiceFee = reader.GetDecimal(reader.GetOrdinal("service_fee"))
                });
            }
        }

        return visit;
    }

    public async Task CreateVisit(CreateVisitDTO createVisitDTO)
    {
        if (createVisitDTO.MechanicLicenceNumber.Length > 14)
        {
            throw new InvalidArgumentException("Mechanic licence number must be less than 14 characters");
        }
        
        SqlConnection connection = new SqlConnection(connectionString);
        SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.Text;

        command.CommandText = @"SELECT count(*) FROM Client WHERE client_id = @client_id";
        command.Parameters.AddWithValue("@client_id", createVisitDTO.ClientId);
        
        await connection.OpenAsync();
        
        if ((int) await command.ExecuteScalarAsync() < 1)
        {
            throw new NotFoundException($"Client with id {createVisitDTO.ClientId} not found");
        }
        
        command.Parameters.Clear();
        command.CommandText = @"SELECT mechanic_id FROM Mechanic Where licence_number = @licence_number";
        command.Parameters.AddWithValue("@licence_number", createVisitDTO.MechanicLicenceNumber);

        var mechanicId = await command.ExecuteScalarAsync();
        if (mechanicId == null)
        {
            throw new NotFoundException($"Mechanic with licence number {createVisitDTO.MechanicLicenceNumber} not found");
        }

        Dictionary<int, Decimal> services = new Dictionary<int, Decimal>();
        foreach (var service in createVisitDTO.services)
        {
            command.Parameters.Clear();
            command.CommandText = @"SELECT service_id FROM Service where name = @name";
            command.Parameters.AddWithValue("@name", service.ServiceName);
            
            var serviceId = await command.ExecuteScalarAsync();
            if (serviceId == null)
            {
                throw new NotFoundException($"Service with name {service.ServiceName} not found");
            }

            services.Add((int) serviceId, service.ServiceFee);
        }

        DbTransaction transaction = connection.BeginTransaction();
        command.Transaction = transaction as SqlTransaction;
        
        command.Parameters.Clear();
        command.CommandText = @"INSERT INTO Visit (visit_id, client_id, mechanic_id, date)
                                values (@visit_id, @client_id, @mechanic_id, @date)";
        command.Parameters.AddWithValue("@visit_id", createVisitDTO.VisitId);
        command.Parameters.AddWithValue("@client_id", createVisitDTO.ClientId);
        command.Parameters.AddWithValue("@mechanic_id", (int) mechanicId);
        command.Parameters.AddWithValue("@date", DateTime.Now);


        try
        {
            await command.ExecuteNonQueryAsync();

            foreach (var (serviceId, serviceFee) in services)
            {
                command.Parameters.Clear();
                command.CommandText = @"INSERT INTO Visit_Service (visit_id, service_id, service_fee)
                                        values (@visit_id, @service_id, @service_fee)";
                command.Parameters.AddWithValue("@visit_id", createVisitDTO.VisitId);
                command.Parameters.AddWithValue("@service_id", serviceId);
                command.Parameters.AddWithValue("@service_fee", serviceFee);

                await command.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw new Exception($"Failed to create visit: {e.Message}");
        }
        
    }
}