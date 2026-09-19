namespace CC.api.Services
{
    public interface ITenantService
    {
        int? GetCurrentCompanyId();
        string GetTenantConnectionString();
    }
}
