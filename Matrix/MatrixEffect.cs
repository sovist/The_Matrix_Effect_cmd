using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Matrix
{
    public sealed class MatrixConfig
    {
        public string Glyphs { get; init; } = "ｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜﾝ0123456789@#$%&*+=?";
        public int MaxBrightness { get; init; } = 20;
        public int FrameDelayMs { get; init; } = 60;
        public int MaxDropsPerColumn { get; init; } = 3;
        public int SpawnChancePercent { get; init; } = 3;
        public int FlickerDensity { get; init; } = 2; // flicker iterations = width / this value
    }

    public sealed class MatrixEffect
    {
        private static class Ansi
        {
            public const string ClearScreen = "\x1b[2J";
            public const string BlackBackground = "\x1b[48;5;0m";
            public const string CursorHome = "\x1b[H";
            public const string Reset = "\x1b[0m";
            public const string Black = "\x1b[30m";
            public const string White = "\x1b[97m";
            public const string BrightGreen = "\x1b[92m";
            public const string Green = "\x1b[32m";
            public const string DarkGreen = "\x1b[38;5;22m";
        }

        private readonly MatrixConfig _config;
        private readonly Random _rng = new();
        private readonly StringBuilder _buffer = new();

        private Cell[,] _screen = null!;
        private List<Drop>[] _columns = null!;
        private int _width;
        private int _height;

        public MatrixEffect(MatrixConfig config)
        {
            _config = config;
        }

        public void Run()
        {
            InitConsole();
            InitState(Console.WindowWidth, Console.WindowHeight - 1);

            while (!Console.KeyAvailable)
            {
                HandleResize();

                Fade();

                AdvanceDrops();

                Flicker();

                Render();

                Thread.Sleep(_config.FrameDelayMs);
            }

            Console.ReadKey(true);
            Console.Write(Ansi.Reset);
            Console.CursorVisible = true;
            Console.Clear();
        }

        private static void InitConsole()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;
            Console.Title = "The Matrix";
            Console.Write(Ansi.ClearScreen);
            Console.Write(Ansi.BlackBackground);
        }

        private void InitState(int width, int height)
        {
            _width = width;
            _height = Math.Max(height, 1);
            _screen = new Cell[_height, _width];
            _columns = new List<Drop>[_width];

            for (int x = 0; x < _width; x++)
            {
                _columns[x] = CreateColumnDrops();
            }
        }

        private void HandleResize()
        {
            int newW = Console.WindowWidth;
            int newH = Console.WindowHeight - 1;

            if (newW == _width && newH == _height)
            {
                return;
            }

            int oldW = _width;
            InitState(newW, newH);

            for (int x = 0; x < Math.Min(oldW, _width); x++)
            {
                _columns[x] ??= CreateColumnDrops();
            }

            Console.Write(Ansi.ClearScreen);
        }

        private void Fade()
        {
            for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
            {
                ref var cell = ref _screen[y, x];

                if (cell.Brightness > 0)
                {
                    cell = cell with { Brightness = cell.Brightness - 1 };
                }
            }
        }

        private void AdvanceDrops()
        {
            for (int x = 0; x < _width; x++)
            {
                var drops = _columns[x];

                for (int d = drops.Count - 1; d >= 0; d--)
                {
                    var drop = drops[d];

                    var stampFrom = drop.Head + 1;

                    drop.Advance();

                    StampNewCells(x, stampFrom, drop.Head);

                    if (drop.HasFadedOff(_height, _config.MaxBrightness))
                    {
                        drops.RemoveAt(d);
                    }
                    else
                    {
                        drops[d] = drop;
                    }
                }

                if (NeedsNewDrop(drops))
                {
                    drops.Add(CreateDrop(startNearTop: true));
                }
            }
        }

        private void StampNewCells(int x, int fromY, int toY)
        {
            int yStart = Math.Max(0, fromY);
            int yEnd = Math.Min(toY, _height - 1);

            for (int y = yStart; y <= yEnd; y++)
            {
                _screen[y, x] = new Cell(RandomGlyph(), _config.MaxBrightness);
            }
        }

        private void Flicker()
        {
            int iterations = _width / _config.FlickerDensity;
            int threshold = _config.MaxBrightness / 2;

            for (int i = 0; i < iterations; i++)
            {
                int x = _rng.Next(_width);
                int y = _rng.Next(_height);

                ref var cell = ref _screen[y, x];

                if (cell.Brightness > 0 && cell.Brightness < threshold)
                {
                    cell = cell with { Glyph = RandomGlyph() };
                }
            }
        }

        private void Render()
        {
            _buffer.Clear();
            _buffer.Append(Ansi.CursorHome);

            int lastTier = -1;

            for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
            {
                var cell = _screen[y, x];
                var tier = BrightnessToTier(cell.Brightness);

                if (tier != lastTier)
                {
                    _buffer.Append(TierToAnsi(tier));

                    lastTier = tier;
                }

                _buffer.Append(tier == 0 ? ' ' : cell.Glyph);
            }

            Console.Write(_buffer);
        }

        private int BrightnessToTier(int brightness) => brightness switch
        {
            0 => 0,
            _ when brightness >= _config.MaxBrightness - 1 => 4,
            _ when brightness >= _config.MaxBrightness * 2 / 3 => 3,
            _ when brightness >= _config.MaxBrightness / 3 => 2,
            _ => 1
        };

        private static string TierToAnsi(int tier) => tier switch
        {
            4 => Ansi.White,
            3 => Ansi.BrightGreen,
            2 => Ansi.Green,
            1 => Ansi.DarkGreen,
            _ => Ansi.Black
        };

        private bool NeedsNewDrop(List<Drop> drops) =>
            drops.Count == 0 ||
            (drops.Count < _config.MaxDropsPerColumn && _rng.Next(100) < _config.SpawnChancePercent);

        private List<Drop> CreateColumnDrops()
        {
            int count = _rng.Next(1, 3);
            var drops = new List<Drop>(count);

            for (int i = 0; i < count; i++)
            {
                int head = _rng.Next(-_height * 2, 0) - i * _rng.Next(10, 30);

                drops.Add(new Drop(head, _rng.Next(1, 4)));
            }

            return drops;
        }

        private Drop CreateDrop(bool startNearTop) => new(_rng.Next(-_height / 2, -3), _rng.Next(1, 4));

        private char RandomGlyph() => _config.Glyphs[_rng.Next(_config.Glyphs.Length)];

        private readonly record struct Cell(char Glyph, int Brightness);

        private struct Drop
        {
            public int Head { get; private set; }

            public int Speed { get; }

            public Drop(int head, int speed)
            {
                Head = head;
                Speed = speed;
            }

            public void Advance() => Head += Speed;

            public bool HasFadedOff(int screenHeight, int maxBrightness) => Head > screenHeight + maxBrightness + 10;
        }
    }
}