namespace Test1.DTOs;

public class DeliveryRequestDto
{
    public int DeliveryId { get; set; }
    public int CustomerId { get; set; }
    public string LicenceNumber { get; set; }

    public List<ProductRequestDto> Products { get; set; }
}

public class ProductRequestDto
{
    public string Name { get; set; }
    public int Amount { get; set; }
}