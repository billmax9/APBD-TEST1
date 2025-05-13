using Test1.DTOs;

namespace Test1.Services;

public interface IDBService
{

    Task<DeliveryResponseDto> FindDeliveryByIdAsync(int id);

    Task AddNewDeliveryAsync(DeliveryRequestDto dto);

}