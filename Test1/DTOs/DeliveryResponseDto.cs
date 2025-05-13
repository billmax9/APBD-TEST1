namespace Test1.DTOs;

public class DeliveryResponseDto
{
    public DateTime Date { get; set; }
    public CustomerResponseDto Customer { get; set; }
    public DriverResponseDto Driver { get; set; }
    public List<ProductResponseDto> Products { get; set; }
}

public class CustomerResponseDto
{
    public string FristName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
}

public class DriverResponseDto
{
    public string FristName { get; set; }
    public string LastName { get; set; }
    public string LicenceNumber { get; set; }
}

public class ProductResponseDto
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Amount { get; set; }
}