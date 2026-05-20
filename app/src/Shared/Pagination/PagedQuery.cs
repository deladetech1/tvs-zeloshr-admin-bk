namespace ZelosHR.Api.Shared.Pagination;

public sealed class PagedQuery
{
    public const int DefaultPage = 1;
    public const int DefaultSize = 10;
    public const int MaxSize = 100;

    public int Page { get; init; } = DefaultPage;
    public int Size { get; init; } = DefaultSize;

    public int Offset => (Math.Max(Page, 1) - 1) * Math.Clamp(Size, 1, MaxSize);

    public static PagedQuery From(int page, int size) =>
        new()
        {
            Page = page < 1 ? DefaultPage : page,
            Size = size < 1 ? DefaultSize : Math.Min(size, MaxSize),
        };
}
