namespace DataImport.Api.Dtos
{
 
    /// <summary>
    /// This wraps all the DTOS.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Items"></param>
    /// <param name="Page"></param>
    /// <param name="PageSize"></param>
    /// <param name="TotalCount"></param>
    /// <param name="TotalPages"></param>
    public record PagedResult<T>(
        List<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}