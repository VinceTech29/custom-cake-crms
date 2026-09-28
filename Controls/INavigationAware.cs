using System.Threading.Tasks;

namespace CC.Controls
{
    /// <summary>
    /// Interface for views hosted in DashboardShell that perform asynchronous data loading.
    /// Ensures deterministic data initialization regardless of Form.Load lifecycle quirks.
    /// </summary>
    public interface INavigationAware
    {
        Task InitializeDataAsync();
    }
}
