namespace Focus.Application.Common;

public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, long TotalCount)
{
    public Page<TResult> Select<TResult>(Func<T, TResult> map) =>
        new(Items.Select(map).ToArray(), PageNumber, PageSize, TotalCount);
}

public sealed record PageRequest
{
    public PageRequest(int number, int size)
    {
        if (number is < 1 or > 100_000 || size is < 1 or > 500)
            throw new UseCaseException(Failure.Invalid, "Pagination is outside the supported bounds.");

        Number = number;
        Size = size;
    }

    public int Number { get; }
    public int Size { get; }
    public int Offset => (Number - 1) * Size;
}
