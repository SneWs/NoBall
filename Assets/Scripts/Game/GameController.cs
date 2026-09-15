using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoBall
{
    public sealed class GameController : MonoBehaviour
    {
        readonly List<Atom> _atoms = new List<Atom>();
        readonly List<Vector2> _atomPositions = new List<Vector2>();

        Camera _camera;
        Playfield _playfield;
        SwipeReader _swipe;
        GameHud _hud;
        SfxPlayer _sfx;
        GrowingWall _wall;
        PhysicsMaterial2D _bounce;
        int _lives;
        int _atomCount;
        int _level;
        int _score;
        bool _ended;
        int _lastWidth;
        int _lastHeight;

        void Awake()
        {
            _camera = UiFactory.EnsureCamera(GameColors.CameraBg);
            UiFactory.EnsureEventSystem();

            _bounce = new PhysicsMaterial2D("AtomBounce")
            {
                friction = 0f,
                bounciness = 1f,
                frictionCombine = PhysicsMaterialCombine2D.Minimum,
                bounceCombine = PhysicsMaterialCombine2D.Maximum
            };

            var fieldGo = new GameObject("Playfield");
            _playfield = fieldGo.AddComponent<Playfield>();
            BuildPlayfield();

            var backgroundGo = new GameObject("Background");
            backgroundGo.AddComponent<GameBackgroundView>().BuildPlayfield(_playfield.WorldSize);

            var swipeGo = new GameObject("Swipe");
            _swipe = swipeGo.AddComponent<SwipeReader>();
            _swipe.Build(_camera);
            _swipe.Swiped += OnSwiped;

            _hud = gameObject.AddComponent<GameHud>();
            _hud.Build(ReturnToMenu, Restart);
            _sfx = GameAudio.Ensure().Sfx;

            ResetRun();
            StartLevel("Swipe horizontally or vertically to build a wall", 4.5f);
        }

        void OnDestroy()
        {
            if (_swipe != null)
                _swipe.Swiped -= OnSwiped;
        }

        void Update()
        {
            if (_lastWidth != Screen.width || _lastHeight != Screen.height)
            {
                _lastWidth = Screen.width;
                _lastHeight = Screen.height;
                FitCamera();
                _hud.RefreshSafeArea();
            }

            if (_ended)
                return;

            if (_swipe.IsDragging && _wall == null)
                UpdatePreview();
            else
                _playfield.ClearPreview();

            if (_wall != null)
            {
                _wall.Tick(Time.deltaTime, _atoms);
                if (_wall.Finished)
                    FinishWall();
            }

            _playfield.LateUpdateVisuals();
        }

        void BuildPlayfield()
        {
            bool landscape = Screen.width >= Screen.height;
            int columns = landscape ? GameConfig.LandscapeColumns : GameConfig.LandscapeRows;
            int rows = landscape ? GameConfig.LandscapeRows : GameConfig.LandscapeColumns;
            _playfield.Build(columns, rows, GameConfig.CellSize);
            FitCamera();
        }

        void FitCamera()
        {
            float aspect = Screen.height <= 0 ? 1.777f : Screen.width / (float)Screen.height;
            float pad = 0.55f;
            float sizeForHeight = _playfield.WorldSize.y * 0.5f + pad;
            float sizeForWidth = (_playfield.WorldSize.x * 0.5f + pad) / Mathf.Max(aspect, 0.1f);
            _camera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
        }

        void ResetRun()
        {
            _level = 1;
            _atomCount = GameConfig.StartingAtoms;
            _score = 0;
        }

        void StartLevel(string hint = null, float hintSeconds = 3.5f)
        {
            _ended = false;
            _wall = null;
            _playfield.ResetField();
            ClearAtoms();
            SpawnAtoms(_atomCount);
            _lives = _atomCount;
            _hud.HideEnd();
            RefreshHud();
            if (!string.IsNullOrEmpty(hint))
                _hud.ShowHint(hint, hintSeconds);
        }

        void RefreshHud()
        {
            _hud.SetStats(_lives, _playfield.Grid.FilledRatio, GameConfig.CaptureTarget, _score);
        }

        void Restart()
        {
            _sfx.PlayClick();
            ResetRun();
            StartLevel("Swipe horizontally or vertically to build a wall", 4.5f);
        }

        void ReturnToMenu()
        {
            _sfx.PlayClick();
            SceneManager.LoadScene(GameConfig.MainMenuScene);
        }

        void OnSwiped(Vector2 start, Vector2 end)
        {
            if (_ended || _wall != null)
                return;
            if (!_playfield.TryWorldToCell(start, out var origin))
                return;
            if (_playfield.Grid[origin] != CellState.Empty)
                return;

            var delta = end - start;
            bool horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);

            for (int i = 0; i < _atoms.Count; i++)
            {
                if (_playfield.CircleHitsCell(_atoms[i].Position, _atoms[i].Radius, origin))
                    return;
            }

            _playfield.ClearPreview();
            _wall = new GrowingWall(_playfield, origin, horizontal);
            _sfx.PlayGrowStart();
        }

        void UpdatePreview()
        {
            if (!_playfield.TryWorldToCell(_swipe.StartWorld, out var origin))
            {
                _playfield.ClearPreview();
                return;
            }

            var delta = _swipe.CurrentWorld - _swipe.StartWorld;
            if (delta.sqrMagnitude < 0.0001f)
            {
                _playfield.ClearPreview();
                return;
            }

            bool horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            _playfield.SetPreview(origin, horizontal);
        }

        void FinishWall()
        {
            var wall = _wall;
            _wall = null;
            _sfx.StopGrow();
            _playfield.CommitBuildingCells(wall.AllCells, wall.AnyHalfCompleted);
            if (!wall.AnyHalfCompleted && _playfield.Grid.InBounds(wall.Origin) && _playfield.Grid[wall.Origin] == CellState.Building)
                _playfield.SetCell(wall.Origin, CellState.Empty);

            if (wall.AnyHalfCompleted)
            {
                CopyAtomPositions();
                _playfield.CaptureFromWorldPositions(_atomPositions);
                _sfx.PlayComplete();
            }

            if (wall.LostLife)
            {
                _lives--;
                _sfx.PlayHit();
            }

            RefreshHud();

            if (_playfield.Grid.FilledRatio >= GameConfig.CaptureTarget)
            {
                CompleteLevel();
                return;
            }

            if (_lives <= 0)
                FailLevel();
        }

        void CompleteLevel()
        {
            _swipe.Cancel();
            _playfield.ClearPreview();

            int percent = Mathf.FloorToInt(_playfield.Grid.FilledRatio * 100f);
            int multiplier = GameConfig.LevelMultiplier(percent);
            int gained = GameConfig.ScoreForLevel(percent);
            _score += gained;
            _sfx.PlayWin();

            _level++;
            _atomCount = Mathf.Min(_atomCount + 1, GameConfig.MaxAtoms);
            StartLevel(
                "Level " + _level + "  •  +" + gained.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) +
                "  (" + multiplier + "×)",
                3.5f);
        }

        void FailLevel()
        {
            _ended = true;
            _swipe.Cancel();
            _playfield.ClearPreview();
            for (int i = 0; i < _atoms.Count; i++)
                _atoms[i].SetPaused(true);

            int percent = Mathf.FloorToInt(_playfield.Grid.FilledRatio * 100f);
            _sfx.PlayLose();
            _hud.ShowEnd(
                "Atoms got through",
                "Claimed " + percent + "%.  Score  " +
                _score.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + ".");
        }

        void SpawnAtoms(int count)
        {
            var dirs = new[]
            {
                new Vector2(1f, 1f),
                new Vector2(-1f, 1f),
                new Vector2(1f, -1f),
                new Vector2(-1f, -1f)
            };

            var used = new List<Vector2Int>();
            for (int i = 0; i < count; i++)
            {
                Vector2Int cell = FindSpawnCell(used, i);
                used.Add(cell);
                var go = new GameObject("Atom " + (i + 1));
                var atom = go.AddComponent<Atom>();
                atom.Build(
                    _playfield.CellCenter(cell),
                    _playfield.AtomRadius,
                    GameConfig.AtomSpeed,
                    _bounce,
                    dirs[i % dirs.Length]);
                atom.Bounced += OnAtomBounced;
                _atoms.Add(atom);
            }
        }

        Vector2Int FindSpawnCell(List<Vector2Int> used, int index)
        {
            int columns = _playfield.Grid.Columns;
            int rows = _playfield.Grid.Rows;
            int margin = 3;
            int minDist = Mathf.Max(2, 7 - used.Count / 3);
            int x = index % 2 == 0
                ? Random.Range(margin, Mathf.Max(margin + 1, columns / 2 - 1))
                : Random.Range(Mathf.Min(columns / 2 + 1, columns - margin - 1), columns - margin);
            if (used.Count >= 2)
                x = Random.Range(margin, columns - margin);
            int y = Random.Range(margin, rows - margin);
            var cell = new Vector2Int(x, y);
            for (int n = 0; n < 24; n++)
            {
                bool clash = false;
                for (int u = 0; u < used.Count; u++)
                {
                    if (Mathf.Abs(used[u].x - cell.x) + Mathf.Abs(used[u].y - cell.y) < minDist)
                    {
                        clash = true;
                        break;
                    }
                }

                if (!clash)
                    return cell;
                x = Random.Range(margin, columns - margin);
                y = Random.Range(margin, rows - margin);
                cell = new Vector2Int(x, y);
            }

            return cell;
        }

        void CopyAtomPositions()
        {
            _atomPositions.Clear();
            for (int i = 0; i < _atoms.Count; i++)
                _atomPositions.Add(_atoms[i].Position);
        }

        void OnAtomBounced()
        {
            _sfx.PlayBounce();
        }

        void ClearAtoms()
        {
            for (int i = 0; i < _atoms.Count; i++)
            {
                if (_atoms[i] != null)
                {
                    _atoms[i].Bounced -= OnAtomBounced;
                    Destroy(_atoms[i].gameObject);
                }
            }

            _atoms.Clear();
        }
    }
}
