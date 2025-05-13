using Microsoft.Data.SqlClient;
using Test1.DTOs;
using Test1.Exceptions;

namespace Test1.Services;

public class DBService : IDBService
{

    private readonly string _connectionString;

    public DBService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DockerSqlServerConnection");
    }
    
    public async Task<DeliveryResponseDto> FindDeliveryByIdAsync(int id)
    {
        DeliveryResponseDto response = null;
        
        string sql = """
                     SELECT 
                         d.date,
                         c.first_name AS CustomerName,
                         c.last_name AS CustomerLastname,
                         c.date_of_birth,
                         dr.first_name AS DriverName,
                         dr.last_name AS DriverLastname,
                         dr.licence_number,
                         p.name,
                         p.price,
                         pd.amount
                         FROM Delivery d
                     INNER JOIN Customer c ON d.customer_id = c.customer_id
                     INNER JOIN Driver dr ON d.driver_id = dr.driver_id
                     INNER JOIN Product_Delivery pd ON d.delivery_id = pd.delivery_id
                     INNER JOIN Product p ON p.product_id  = pd.product_id
                     WHERE d.delivery_id = @IdDelivery;
                     """;
        
        await using(SqlConnection connection = new SqlConnection(_connectionString))
        await using (SqlCommand command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@IdDelivery", id);
            await connection.OpenAsync();

            var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                response = new DeliveryResponseDto
                {
                    Date = reader.GetDateTime(reader.GetOrdinal("date")),
                    Customer = new CustomerResponseDto
                    {
                        DateOfBirth = reader.GetDateTime(reader.GetOrdinal("date_of_birth")),
                        FristName = reader.GetString(reader.GetOrdinal("CustomerName")),
                        LastName = reader.GetString(reader.GetOrdinal("CustomerLastname")),
                    },
                    Driver = new DriverResponseDto{
                        FristName = reader.GetString(reader.GetOrdinal("DriverName")),
                        LastName = reader.GetString(reader.GetOrdinal("DriverLastname")),
                        LicenceNumber = reader.GetString(reader.GetOrdinal("licence_number"))
                    },
                    Products = new List<ProductResponseDto>()
                    {
                        new ProductResponseDto
                        {
                            Amount = reader.GetInt32(reader.GetOrdinal("amount")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            Price = reader.GetDecimal(reader.GetOrdinal("price")),
                        }
                    }
                };
            }

            while (await reader.ReadAsync())
            {
                response.Products.Add(new ProductResponseDto
                {
                    Amount = reader.GetInt32(reader.GetOrdinal("amount")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Price = reader.GetDecimal(reader.GetOrdinal("price")),
                });
            }
        }

        if (response == null)
            throw new NotFoundException($"Delivery with id {id} was not found!");

        return response;
    }
}