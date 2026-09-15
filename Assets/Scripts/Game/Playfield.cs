using System.Collections.Generic;
using UnityEngine;

namespace NoBall
{
    public sealed class Playfield : MonoBehaviour
    {
        Texture2D _texture;
        SpriteRenderer _fieldRenderer;
        Transform _colliderRoot;
        Transform _boundRoot;
        Color32[] _pixels;
        bool _dirty;
        Vector2Int? _previewOrigin;
        bool _previewHorizontal;

        public PlayfieldGrid Grid { get; private set; }
        public float CellSize { get; private set; }
        public Vector2 BottomLeft { get; private set; }
        public Vector2 Center { get; private set; }
        public Vector2 WorldSize { get; private set; }
        public float AtomRadius { get; private set; }

        public void Build(int columns, int rows, float cellSize)
        {
            CellSize = cellSize;
            Grid = new PlayfieldGrid(columns, rows);
            WorldSize = new Vector2(columns * cellSize, rows * cellSize);
            Center = Vector2.zero;
            BottomLeft = Center - WorldSize * 0.5f;
            AtomRadius = cellSize * 0.55f;

            _colliderRoot = new GameObject("FilledColliders").transform;
            _colliderRoot.SetParent(transform, false);
            _boundRoot = new GameObject("Bounds").transform;
            _boundRoot.SetParent(transform, false);

            BuildFrame();
            BuildTexture();
            BuildBounds();
            RefreshVisuals();
        }

        public bool TryWorldToCell(Vector2 world, out Vector2Int cell)
        {
            int x = Mathf.FloorToInt((world.x - BottomLeft.x) / CellSize);
            int y = Mathf.FloorToInt((world.y - BottomLeft.y) / CellSize);
            cell = new Vector2Int(x, y);
            return Grid.InBounds(cell);
        }

        public Vector2 CellCenter(Vector2Int cell)
        {
            return new Vector2(
                BottomLeft.x + (cell.x + 0.5f) * CellSize,
                BottomLeft.y + (cell.y + 0.5f) * CellSize);
        }

        public Rect CellWorldRect(Vector2Int cell)
        {
            return new Rect(
                BottomLeft.x + cell.x * CellSize,
                BottomLeft.y + cell.y * CellSize,
                CellSize,
                CellSize);
        }

        public void ResetField()
        {
            Grid.Clear();
            ClearPreview();
            RebuildColliders();
            RefreshVisuals();
        }

        public void SetCell(Vector2Int cell, CellState state)
        {
            if (!Grid.InBounds(cell))
                return;
            Grid[cell] = state;
            _dirty = true;
        }

        public void SetPreview(Vector2Int? origin, bool horizontal)
        {
            _previewOrigin = origin;
            _previewHorizontal = horizontal;
            _dirty = true;
        }

        public void ClearPreview()
        {
            if (_previewOrigin == null)
                return;
            _previewOrigin = null;
            _dirty = true;
        }

        public void CommitBuildingCells(IEnumerable<Vector2Int> cells, bool fill)
        {
            var state = fill ? CellState.Filled : CellState.Empty;
            foreach (var cell in cells)
            {
                if (Grid.InBounds(cell) && Grid[cell] == CellState.Building)
                    Grid[cell] = state;
            }

            _dirty = true;
        }

        public void CaptureFromWorldPositions(IReadOnlyList<Vector2> worldPositions)
        {
            var seeds = new List<Vector2Int>(worldPositions.Count);
            for (int i = 0; i < worldPositions.Count; i++)
            {
                if (TryWorldToCell(worldPositions[i], out var cell))
                    seeds.Add(cell);
            }

            Grid.CaptureUnreachable(seeds);
            RebuildColliders();
            _dirty = true;
        }

        public bool CircleHitsBuilding(Vector2 position, float radius, IReadOnlyList<Vector2Int> cells, out Vector2Int hit)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (CircleHitsCell(position, radius, cells[i]))
                {
                    hit = cells[i];
                    return true;
                }
            }

            hit = default;
            return false;
        }

        public bool CircleHitsCell(Vector2 position, float radius, Vector2Int cell)
        {
            var rect = CellWorldRect(cell);
            float cx = Mathf.Clamp(position.x, rect.xMin, rect.xMax);
            float cy = Mathf.Clamp(position.y, rect.yMin, rect.yMax);
            float dx = position.x - cx;
            float dy = position.y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        public void RebuildColliders()
        {
            for (int i = _colliderRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(_colliderRoot.GetChild(i).gameObject);

            int columns = Grid.Columns;
            int rows = Grid.Rows;
            var used = new bool[columns, rows];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (used[x, y] || Grid[x, y] != CellState.Filled)
                        continue;

                    int w = 1;
                    while (x + w < columns && Grid[x + w, y] == CellState.Filled && !used[x + w, y])
                        w++;

                    int h = 1;
                    bool grow = true;
                    while (y + h < rows && grow)
                    {
                        for (int i = 0; i < w; i++)
                        {
                            if (Grid[x + i, y + h] != CellState.Filled || used[x + i, y + h])
                            {
                                grow = false;
                                break;
                            }
                        }

                        if (grow)
                            h++;
                    }

                    for (int yy = 0; yy < h; yy++)
                    {
                        for (int xx = 0; xx < w; xx++)
                            used[x + xx, y + yy] = true;
                    }

                    CreateFilledBox(x, y, w, h);
                }
            }

            Physics2D.SyncTransforms();
        }

        public void LateUpdateVisuals()
        {
            if (_dirty)
                RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            if (_texture == null || Grid == null)
                return;

            int ppc = GameConfig.PixelsPerCell;
            int width = Grid.Columns * ppc;
            int height = Grid.Rows * ppc;
            bool seeBackground = GameSettings.ShowsBackground;
            var empty = (Color32)GameColors.Playfield;
            var gridLine = (Color32)GameColors.PlayfieldGrid;
            var filled = (Color32)GameColors.Captured;
            var building = (Color32)GameColors.Building;
            var preview = seeBackground
                ? (Color32)GameColors.Preview
                : (Color32)Color.Lerp(GameColors.Playfield, GameColors.Building, 0.45f);
            var seeThrough = new Color32(0, 0, 0, 36);
            var seeThroughGrid = new Color32(gridLine.r, gridLine.g, gridLine.b, 110);

            for (int y = 0; y < Grid.Rows; y++)
            {
                for (int x = 0; x < Grid.Columns; x++)
                {
                    var state = Grid[x, y];
                    Color32 color = state switch
                    {
                        CellState.Filled => filled,
                        CellState.Building => building,
                        _ => seeBackground ? seeThrough : empty
                    };

                    if (state == CellState.Empty && IsPreviewCell(x, y))
                        color = preview;

                    for (int py = 0; py < ppc; py++)
                    {
                        for (int px = 0; px < ppc; px++)
                        {
                            Color32 pixel = color;
                            if (state == CellState.Empty && (px == 0 || py == 0))
                                pixel = seeBackground ? seeThroughGrid : gridLine;
                            int ix = x * ppc + px;
                            int iy = y * ppc + py;
                            _pixels[iy * width + ix] = pixel;
                        }
                    }
                }
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply(false, false);
            _dirty = false;
        }

        bool IsPreviewCell(int x, int y)
        {
            if (_previewOrigin == null)
                return false;
            var origin = _previewOrigin.Value;
            return _previewHorizontal ? y == origin.y : x == origin.x;
        }

        void BuildTexture()
        {
            int ppc = GameConfig.PixelsPerCell;
            int width = Grid.Columns * ppc;
            int height = Grid.Rows * ppc;
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _pixels = new Color32[width * height];

            float ppu = ppc / CellSize;
            var sprite = Sprite.Create(
                _texture,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                ppu,
                0,
                SpriteMeshType.FullRect);

            var field = new GameObject("FieldVisual");
            field.transform.SetParent(transform, false);
            field.transform.position = new Vector3(Center.x, Center.y, 0f);
            _fieldRenderer = field.AddComponent<SpriteRenderer>();
            _fieldRenderer.sprite = sprite;
            _fieldRenderer.sharedMaterial = SpriteFactory.SpriteMaterial;
            _fieldRenderer.sortingOrder = 2;
        }

        void BuildFrame()
        {
            float t = 0.18f;
            float w = WorldSize.x;
            float h = WorldSize.y;
            CreateFrameBar("FrameLeft", new Vector2(BottomLeft.x - t * 0.5f, Center.y), new Vector2(t, h + t * 2f));
            CreateFrameBar("FrameRight", new Vector2(BottomLeft.x + w + t * 0.5f, Center.y), new Vector2(t, h + t * 2f));
            CreateFrameBar("FrameBottom", new Vector2(Center.x, BottomLeft.y - t * 0.5f), new Vector2(w + t * 2f, t));
            CreateFrameBar("FrameTop", new Vector2(Center.x, BottomLeft.y + h + t * 0.5f), new Vector2(w + t * 2f, t));
        }

        void CreateFrameBar(string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.White;
            sr.sharedMaterial = SpriteFactory.SpriteMaterial;
            sr.color = GameColors.Frame;
            sr.sortingOrder = 3;
        }

        void BuildBounds()
        {
            float w = WorldSize.x;
            float h = WorldSize.y;
            float t = 0.6f;
            CreateBound("Left", new Vector2(BottomLeft.x - t * 0.5f, Center.y), new Vector2(t, h + t * 2f));
            CreateBound("Right", new Vector2(BottomLeft.x + w + t * 0.5f, Center.y), new Vector2(t, h + t * 2f));
            CreateBound("Bottom", new Vector2(Center.x, BottomLeft.y - t * 0.5f), new Vector2(w + t * 2f, t));
            CreateBound("Top", new Vector2(Center.x, BottomLeft.y + h + t * 0.5f), new Vector2(w + t * 2f, t));
        }

        void CreateBound(string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_boundRoot, false);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        void CreateFilledBox(int x, int y, int w, int h)
        {
            var go = new GameObject($"Filled_{x}_{y}_{w}x{h}");
            go.transform.SetParent(_colliderRoot, false);
            go.transform.position = new Vector3(
                BottomLeft.x + (x + w * 0.5f) * CellSize,
                BottomLeft.y + (y + h * 0.5f) * CellSize,
                0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(w * CellSize, h * CellSize);
        }
    }
}
