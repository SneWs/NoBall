using System.Collections.Generic;
using UnityEngine;

namespace NoBall
{
    public enum CellState : byte
    {
        Empty = 0,
        Filled = 1,
        Building = 2
    }

    public sealed class PlayfieldGrid
    {
        readonly CellState[] _cells;

        public int Columns { get; }
        public int Rows { get; }
        public int CellCount => Columns * Rows;

        public PlayfieldGrid(int columns, int rows)
        {
            Columns = columns;
            Rows = rows;
            _cells = new CellState[columns * rows];
        }

        public CellState this[int x, int y]
        {
            get => _cells[y * Columns + x];
            set => _cells[y * Columns + x] = value;
        }

        public CellState this[Vector2Int cell]
        {
            get => this[cell.x, cell.y];
            set => this[cell.x, cell.y] = value;
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Columns && y < Rows;
        public bool InBounds(Vector2Int cell) => InBounds(cell.x, cell.y);

        public void Clear()
        {
            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = CellState.Empty;
        }

        public int Count(CellState state)
        {
            int n = 0;
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] == state)
                    n++;
            }

            return n;
        }

        public float FilledRatio => Count(CellState.Filled) / (float)CellCount;

        public List<Vector2Int> CaptureUnreachable(IReadOnlyList<Vector2Int> seeds)
        {
            int n = CellCount;
            var reach = new bool[n];
            var queue = new Queue<int>();

            void TryEnqueue(int x, int y)
            {
                if (!InBounds(x, y) || this[x, y] != CellState.Empty)
                    return;
                int i = y * Columns + x;
                if (reach[i])
                    return;
                reach[i] = true;
                queue.Enqueue(i);
            }

            for (int s = 0; s < seeds.Count; s++)
            {
                var seed = seeds[s];
                if (!InBounds(seed))
                    continue;
                if (this[seed] == CellState.Empty)
                {
                    TryEnqueue(seed.x, seed.y);
                }
                else
                {
                    TryEnqueue(seed.x + 1, seed.y);
                    TryEnqueue(seed.x - 1, seed.y);
                    TryEnqueue(seed.x, seed.y + 1);
                    TryEnqueue(seed.x, seed.y - 1);
                }
            }

            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                int x = i % Columns;
                int y = i / Columns;
                TryEnqueue(x + 1, y);
                TryEnqueue(x - 1, y);
                TryEnqueue(x, y + 1);
                TryEnqueue(x, y - 1);
            }

            var captured = new List<Vector2Int>();
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    int i = y * Columns + x;
                    if (_cells[i] == CellState.Empty && !reach[i])
                    {
                        _cells[i] = CellState.Filled;
                        captured.Add(new Vector2Int(x, y));
                    }
                }
            }

            return captured;
        }
    }
}
