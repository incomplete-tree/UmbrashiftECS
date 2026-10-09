using System;
using UmbrashiftECS.Components;

namespace UmbrashiftECS.GameLogic;

public static class Direction8Extensions
{
    extension (Direction8 dir)
    {
        public (int X, int Y) ToAxes() => dir switch
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
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, "Unknown direction")
        };
    }

    extension(Direction8)
    {
        public static Direction8 FromAxes(int x, int y) => (x, y) switch
        {
            (0, 0)   => Direction8.None,
            (0, -1)  => Direction8.Up,
            (1, -1)  => Direction8.UpRight,
            (1, 0)   => Direction8.Right,
            (1, 1)   => Direction8.DownRight,
            (0, 1)   => Direction8.Down,
            (-1, 1)  => Direction8.DownLeft,
            (-1, 0)  => Direction8.Left,
            (-1, -1) => Direction8.UpLeft,
            _ => throw new InvalidOperationException()
        };
    }
}
