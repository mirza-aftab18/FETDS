namespace FETDS.Services
{
    public interface IProductExpiryService
    {
        Task ExpireOverdueProductsAsync();
        Task ExpireOverdueProductsAsync(string donorId);
    }
}