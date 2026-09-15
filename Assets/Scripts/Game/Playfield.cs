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
        WallRibbon _buildingRibbon;
        WallRibbon _previewRibbon;

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
            _buildingRibbon = new WallRibbon(transform, 4, GameColors.Building);
            _previewRibbon = new WallRibbon(transform, 3, GameColors.Preview);
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
            if (origin == null || Grid == null)
                _previewRibbon?.Hide();
            else if (horizontal)
                _previewRibbon.Layout(this, origin.Value.y, 0, Grid.Columns - 1, true);
            else
                _previewRibbon.Layout(this, origin.Value.x, 0, Grid.Rows - 1, false);
        }

        public void ClearPreview()
        {
            if (_previewOrigin == null)
                return;
            _previewOrigin = null;
            _previewRibbon?.Hide();
        }

        public void ShowBuildingWall(Vector2Int origin, bool horizontal, IReadOnlyList<Vector2Int> cells)
        {
            if (_buildingRibbon == null || cells == null || cells.Count == 0)
            {
                _buildingRibbon?.Hide();
                return;
            }

            int min;
            int max;
            if (horizontal)
            {
                min = max = origin.x;
                for (int i = 0; i < cells.Count; i++)
                {
                    min = Mathf.Min(min, cells[i].x);
                    max = Mathf.Max(max, cells[i].x);
                }

                _buildingRibbon.Layout(this, origin.y, min, max, true);
            }
            else
            {
                min = max = origin.y;
                for (int i = 0; i < cells.Count; i++)
                {
                    min = Mathf.Min(min, cells[i].y);
                    max = Mathf.Max(max, cells[i].y);
                }

                _buildingRibbon.Layout(this, origin.x, min, max, false);
            }
        }

        public void HideBuildingWall()
        {
            _buildingRibbon?.Hide();
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

        public List<Vector2Int> CaptureFromWorldPositions(IReadOnlyList<Vector2> worldPositions)
        {
            var seeds = new List<Vector2Int>(worldPositions.Count);
            for (int i = 0; i < worldPositions.Count; i++)
            {
                if (TryWorldToCell(worldPositions[i], out var cell))
                    seeds.Add(cell);
            }

            var captured = Grid.CaptureUnreachable(seeds);
            RebuildColliders();
            _dirty = true;
            return captured;
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
            var filled = (Color32)GameColors.Captured;
            var seeThrough = new Color32(0, 0, 0, 36);

            for (int y = 0; y < Grid.Rows; y++)
            {
                for (int x = 0; x < Grid.Columns; x++)
                {
                    var state = Grid[x, y];
                    Color32 color = state == CellState.Filled
                        ? filled
                        : (seeBackground ? seeThrough : empty);

                    for (int py = 0; py < ppc; py++)
                    {
                        for (int px = 0; px < ppc; px++)
                        {
                            int ix = x * ppc + px;
                            int iy = y * ppc + py;
                            _pixels[iy * width + ix] = color;
                        }
                    }
                }
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply(false, false);
            _dirty = false;
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

    sealed class WallRibbon
    {
        readonly Transform _root;
        readonly SpriteRenderer _shaft;
        readonly SpriteRenderer _capA;
        readonly SpriteRenderer _capB;

        public WallRibbon(Transform parent, int sorting, Color color)
        {
            _root = new GameObject("WallRibbon").transform;
            _root.SetParent(parent, false);
            _shaft = MakePart("Shaft", _root, SpriteFactory.White, sorting, color);
            _capA = MakePart("CapA", _root, SpriteFactory.Circle, sorting, color);
            _capB = MakePart("CapB", _root, SpriteFactory.Circle, sorting, color);
            Hide();
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        public void Layout(Playfield field, int lane, int min, int max, bool horizontal)
        {
            if (max < min)
            {
                Hide();
                return;
            }

            _root.gameObject.SetActive(true);
            float t = field.CellSize * 0.92f;
            float z = -0.03f;

            if (horizontal)
            {
                float x0 = field.BottomLeft.x + min * field.CellSize;
                float x1 = field.BottomLeft.x + (max + 1) * field.CellSize;
                float y = field.BottomLeft.y + (lane + 0.5f) * field.CellSize;
                Place(x0, x1, y, y, t, z, true);
            }
            else
            {
                float y0 = field.BottomLeft.y + min * field.CellSize;
                float y1 = field.BottomLeft.y + (max + 1) * field.CellSize;
                float x = field.BottomLeft.x + (lane + 0.5f) * field.CellSize;
                Place(x, x, y0, y1, t, z, false);
            }
        }

        void Place(float x0, float x1, float y0, float y1, float thickness, float z, bool horizontal)
        {
            float length = horizontal ? Mathf.Abs(x1 - x0) : Mathf.Abs(y1 - y0);
            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            float half = thickness * 0.5f;

            Vector3 capStart = horizontal
                ? new Vector3(Mathf.Min(x0, x1) + half, cy, z)
                : new Vector3(cx, Mathf.Min(y0, y1) + half, z);
            Vector3 capEnd = horizontal
                ? new Vector3(Mathf.Max(x0, x1) - half, cy, z)
                : new Vector3(cx, Mathf.Max(y0, y1) - half, z);

            if (length <= thickness + 0.001f)
            {
                _shaft.enabled = false;
                _capA.enabled = true;
                _capB.enabled = false;
                _capA.transform.position = new Vector3(cx, cy, z);
                _capA.transform.localScale = Vector3.one * thickness;
                return;
            }

            _shaft.enabled = true;
            _capA.enabled = true;
            _capB.enabled = true;
            _capA.transform.position = capStart;
            _capB.transform.position = capEnd;
            _capA.transform.localScale = Vector3.one * thickness;
            _capB.transform.localScale = Vector3.one * thickness;

            _shaft.transform.position = new Vector3(cx, cy, z);
            if (horizontal)
                _shaft.transform.localScale = new Vector3(Mathf.Max(0.001f, length - thickness), thickness, 1f);
            else
                _shaft.transform.localScale = new Vector3(thickness, Mathf.Max(0.001f, length - thickness), 1f);
        }

        static SpriteRenderer MakePart(string name, Transform parent, Sprite sprite, int sorting, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = SpriteFactory.SpriteMaterial;
            sr.color = color;
            sr.sortingOrder = sorting;
            return sr;
        }
    }
}
