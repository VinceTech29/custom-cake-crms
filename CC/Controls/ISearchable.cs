namespace CC.Controls
{
    /// <summary>
    /// Interface for hosted module forms that support search filtering via top header bar.
    /// </summary>
    public interface ISearchable
    {
        void Search(string query);
    }
}
