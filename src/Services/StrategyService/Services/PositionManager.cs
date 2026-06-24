using StrategyService.Models;

namespace StrategyService.Services;

public sealed class PositionManager
{
    private readonly List<Position> _positions = [];
    private readonly object _lock = new();

    public IReadOnlyList<Position> GetActivePositions()
    {
        lock (_lock)
        {
            return _positions
                .Where(x => !x.Closed)
                .ToList();
        }
    }

    public int CountBySide(string side)
        => GetActivePositions()
            .Count(x => x.Side.Equals(side, StringComparison.OrdinalIgnoreCase));

    public void Add(Position position)
    {
        lock (_lock)
        {
            _positions.Add(position);
        }
    }

    public void CloseOpposite(string side)
    {
        var opposite = side.Equals("LONG", StringComparison.OrdinalIgnoreCase)
            ? "SHORT"
            : "LONG";

        lock (_lock)
        {
            foreach (var position in _positions.Where(x => x.Side == opposite && !x.Closed))
            {
                position.Closed = true;
            }
        }
    }
}