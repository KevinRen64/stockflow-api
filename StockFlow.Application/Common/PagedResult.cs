namespace StockFlow.Application.Common;

public class PagedResult<T>
{
  public IReadOnlyList<T> Items { get; set; } = new List<T>();
  public int Page { get; set; }
  public int PageSize { get; set; }
  public int TotalCount { get; set; }
  public int TotalPages { get; set; }

  public static PagedResult<T> Create
  (
    IReadOnlyList<T> items,
    int page,
    int pageSize,
    int totalCount
  )
  {
    return new PagedResult<T>
    {
      Items = items,
      Page = page,
      PageSize = pageSize,
      TotalCount = totalCount,
      TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };
  }
}