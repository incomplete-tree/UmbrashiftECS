using System;

namespace UmbrashiftECS.GameLogic;

public static class Direction8Extensions
{
    public static (int X, int Y) ToAxes(this Direction8 direction) => direction switch
    {
        Direction8.None => (0, 0),
        Direction8.Up => (0, -1),
        Direction8.UpRight => (1, -1),
        Direction8.Right => (1, 0),
        Direction8.DownRight => (1, 1),
        Direction8.Down => (0, 1),
        Direction8.DownLeft => (-1, 1),
        Direction8.Left => (-1, 0),
        Direction8.UpLeft => (-1, -1),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction")
    };
}
