using System.Collections.Generic;
using UnityEngine;

namespace NoBall
{
    public sealed class GrowingWall
    {
        readonly Playfield _playfield;
        readonly List<Vector2Int> _positive = new List<Vector2Int>();
        readonly List<Vector2Int> _negative = new List<Vector2Int>();
        readonly List<Vector2Int> _all = new List<Vector2Int>();
        readonly Vector2Int _positiveDir;
        readonly Vector2Int _negativeDir;
        float _accumulator;
        bool _positiveActive = true;
        bool _negativeActive = true;

        public Vector2Int Origin { get; }
        public bool Horizontal { get; }
        public bool Finished => !_positiveActive && !_negativeActive;
        public bool LostLife { get; private set; }
        public bool AnyHalfCompleted { get; private set; }
        public IReadOnlyList<Vector2Int> AllCells => _all;

        public GrowingWall(Playfield playfield, Vector2Int origin, bool horizontal)
        {
            _playfield = playfield;
            Origin = origin;
            Horizontal = horizontal;
            _positiveDir = horizontal ? Vector2Int.right : Vector2Int.up;
            _negativeDir = horizontal ? Vector2Int.left : Vector2Int.down;
            _all.Add(origin);
            playfield.SetCell(origin, CellState.Building);
            playfield.ShowBuildingWall(Origin, Horizontal, _all);
            WallFx.GrowTip(playfield.CellCenter(origin), Vector2.zero);
        }

        public void Tick(float dt, IReadOnlyList<Atom> atoms)
        {
            if (Finished)
                return;

            if (HitsAtoms(atoms))
                return;

            _accumulator += dt;
            float step = 1f / GameConfig.WallCellsPerSecond;
            while (_accumulator >= step && !Finished)
            {
                _accumulator -= step;
                if (_negativeActive)
                    Step(ref _negativeActive, _negative, _negativeDir);
                if (_positiveActive)
                    Step(ref _positiveActive, _positive, _positiveDir);
                if (HitsAtoms(atoms))
                    return;
            }
        }

        void Step(ref bool active, List<Vector2Int> cells, Vector2Int dir)
        {
            if (!active)
                return;

            var last = cells.Count == 0 ? Origin : cells[cells.Count - 1];
            var next = last + dir;
            if (!_playfield.Grid.InBounds(next) || _playfield.Grid[next] == CellState.Filled)
            {
                active = false;
                AnyHalfCompleted = true;
                return;
            }

            if (_playfield.Grid[next] == CellState.Building)
            {
                active = false;
                AnyHalfCompleted = true;
                return;
            }

            cells.Add(next);
            _all.Add(next);
            _playfield.SetCell(next, CellState.Building);
            _playfield.ShowBuildingWall(Origin, Horizontal, _all);
            WallFx.GrowTip(_playfield.CellCenter(next), new Vector2(dir.x, dir.y));
        }

        bool HitsAtoms(IReadOnlyList<Atom> atoms)
        {
            for (int i = 0; i < atoms.Count; i++)
            {
                var atom = atoms[i];
                if (atom == null)
                    continue;
                if (!_playfield.CircleHitsBuilding(atom.Position, atom.Radius, _all, out var hit))
                    continue;

                LostLife = true;
                var rect = _playfield.CellWorldRect(hit);
                var crash = new Vector2(
                    Mathf.Clamp(atom.Position.x, rect.xMin, rect.xMax),
                    Mathf.Clamp(atom.Position.y, rect.yMin, rect.yMax));
                atom.PlayWallCrash();
                ImpactFx.WallCrash(crash);
                bool hitOrigin = hit == Origin;
                bool hitPositive = _positive.Contains(hit);
                bool hitNegative = _negative.Contains(hit);

                if (hitOrigin || (hitPositive && hitNegative))
                {
                    CancelHalf(ref _positiveActive, _positive, crash);
                    CancelHalf(ref _negativeActive, _negative, crash);
                    ShatterOrigin(crash);
                    _positiveActive = false;
                    _negativeActive = false;
                    return true;
                }

                if (hitPositive)
                    CancelHalf(ref _positiveActive, _positive, crash);
                if (hitNegative)
                    CancelHalf(ref _negativeActive, _negative, crash);
                return true;
            }

            return false;
        }

        void CancelHalf(ref bool active, List<Vector2Int> cells, Vector2 crash)
        {
            active = false;
            WallFx.Shatter(_playfield, cells, crash);
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                _playfield.SetCell(cell, CellState.Empty);
                _all.Remove(cell);
            }

            cells.Clear();
            _playfield.ShowBuildingWall(Origin, Horizontal, _all);
        }

        void ShatterOrigin(Vector2 crash)
        {
            if (!_playfield.Grid.InBounds(Origin) || _playfield.Grid[Origin] != CellState.Building)
                return;
            WallFx.Shatter(_playfield, new[] { Origin }, crash);
            _playfield.SetCell(Origin, CellState.Empty);
            _all.Remove(Origin);
            _playfield.ShowBuildingWall(Origin, Horizontal, _all);
        }
    }
}
