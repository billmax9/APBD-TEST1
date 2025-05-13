using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
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

        await using (SqlConnection connection = new SqlConnection(_connectionString))
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
                    Driver = new DriverResponseDto
                    {
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

    public async Task AddNewDeliveryAsync(DeliveryRequestDto dto)
    {
        string deliverySql = "SELECT delivery_id FROM Delivery WHERE delivery_id = @IdDelivery;";
        string customerSql = "SELECT customer_id FROM Customer WHERE customer_id = @IdCustomer;";
        string driverSql = "SELECT driver_id FROM Driver WHERE licence_number = @LicenceNumber;";
        string productSql = "SELECT product_id FROM Product WHERE name = @ProductName;";

        string insertDeliverySql = """
                                   INSERT INTO Delivery(delivery_id, customer_id, driver_id, date)
                                   VALUES (@IdDelivery, @IdCustomer, @IdDriver, @Date);
                                   """;
        
        string insertProductDeliverySql = """
                                          INSERT INTO Product_Delivery(product_id, delivery_id, amount)
                                          VALUES(@IdProduct, @IdDelivery, @Amount);
                                          """;
        
        await using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            SqlTransaction tx = connection.BeginTransaction();
            
            // Checking delivery with provided id
            await using (SqlCommand deliveryCommand = new SqlCommand(deliverySql, connection, tx))
            {
                deliveryCommand.Parameters.AddWithValue("@IdDelivery", dto.DeliveryId);
                var reader = await deliveryCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    await reader.CloseAsync();
                    tx.Rollback();
                    throw new ConflictException("Delivery with provided id already exist");
                }

                await reader.CloseAsync();
            }

            // Checking if customer exist
            await using (SqlCommand customerCommand = new SqlCommand(customerSql, connection, tx))
            {
                customerCommand.Parameters.AddWithValue("@IdCustomer", dto.CustomerId);
                var reader = await customerCommand.ExecuteReaderAsync();

                if (await reader.ReadAsync() == false)
                {
                    await reader.CloseAsync();
                    tx.Rollback();
                    throw new NotFoundException($"Customer with id {dto.CustomerId} was not found!");
                }

                await reader.CloseAsync();
            }

            // Checking if driver exist
            int driverId = 0;
            await using (SqlCommand driverCommand = new SqlCommand(driverSql, connection, tx))
            {
                driverCommand.Parameters.AddWithValue("@LicenceNumber", dto.LicenceNumber);
                var reader = await driverCommand.ExecuteReaderAsync();

                if (await reader.ReadAsync() == false)
                {
                    await reader.CloseAsync();
                    tx.Rollback();
                    throw new NotFoundException($"Driver with licence number {dto.LicenceNumber} was not found!");
                }

                driverId = reader.GetInt32(reader.GetOrdinal("driver_id"));
                await reader.CloseAsync();
            }

            // Checking if all provided products exist
            Dictionary<string, int> productNameDict = new Dictionary<string, int>();
            foreach (var prod in dto.Products)
            {
                if (prod.Name.IsNullOrEmpty())
                {
                    tx.Rollback();
                    throw new DataValidationException("Product name can't be empty");
                }

                if (prod.Amount <= 0)
                {
                    tx.Rollback();
                    throw new DataValidationException("Product's amount should be at lest 1");
                }

                await using (SqlCommand productCommand = new SqlCommand(productSql, connection, tx))
                {
                    productCommand.Parameters.AddWithValue("@ProductName", prod.Name);
                    var reader = await productCommand.ExecuteReaderAsync();

                    if (await reader.ReadAsync() == false)
                    {
                        await reader.CloseAsync();
                        tx.Rollback();
                        throw new NotFoundException($"Product with name {prod.Name} was not found!");
                    }

                    int productId = reader.GetInt32(reader.GetOrdinal("product_id"));
                    productNameDict.Add(prod.Name, productId);
                    
                    await reader.CloseAsync();
                }
            }

            
            // Inserting delivery
            await using (SqlCommand insertDeliveryCommand = new SqlCommand(insertDeliverySql, connection, tx))
            {
                try
                {
                    insertDeliveryCommand.Parameters.AddWithValue("@IdDelivery", dto.DeliveryId);
                    insertDeliveryCommand.Parameters.AddWithValue("@IdCustomer", dto.CustomerId);
                    insertDeliveryCommand.Parameters.AddWithValue("@IdDriver", driverId);
                    insertDeliveryCommand.Parameters.AddWithValue("@Date", DateTime.Now);

                    await insertDeliveryCommand.ExecuteNonQueryAsync();
                }
                catch (Exception e)
                {
                    tx.Rollback();
                    throw;
                }
            }
            
            foreach (var prod in dto.Products)
            {
                try
                {
                    await using (SqlCommand insertProductDeliveryCommand = new SqlCommand(insertProductDeliverySql, connection, tx))
                    {
                        insertProductDeliveryCommand.Parameters.AddWithValue("@IdDelivery", dto.DeliveryId);
                        insertProductDeliveryCommand.Parameters.AddWithValue("@IdProduct", productNameDict[prod.Name]);
                        insertProductDeliveryCommand.Parameters.AddWithValue("@Amount", prod.Amount);
            
                        await insertProductDeliveryCommand.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception e)
                {
                    tx.Rollback();
                    throw;
                }
            }
            
            tx.Commit();
        }
    }
}