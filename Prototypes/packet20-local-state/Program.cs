using ContextBindingPrototype;

var app = new DemoApp();
if (args.Any(static argument => string.Equals(argument, "--demo", StringComparison.OrdinalIgnoreCase)))
{
    app.RunDemo();
    return;
}

app.RunInteractive();

internal sealed class DemoApp
{
    private readonly Packet20Binder _binder = new();
    private ExternalPacket20Context _external = ExternalPacket20Context.CreateDefault();
    private PreparedPacket20? _submission;
    private BindResult? _lastBinding;
    private EncodeResult? _lastEncoding;
    private string _lastAction = "初始状态：外部数据尚未绑定。";

    public void RunInteractive()
    {
        Render();
        while (true)
        {
            var key = Console.ReadKey(intercept: true).KeyChar;
            if (!Apply(key))
            {
                return;
            }

            Render();
        }
    }

    public void RunDemo()
    {
        foreach (var action in new[] { 'b', 'e', 'm', 'e', 'v', 'b', 'r', 'l', 'b', 'e', 'z', 'b', 'e' })
        {
            if (!Apply(action))
            {
                return;
            }

            Render();
        }
    }

    private bool Apply(char action)
    {
        switch (char.ToLowerInvariant(action))
        {
            case 'b':
                _lastBinding = _binder.Bind(_external);
                _lastEncoding = null;
                if (_lastBinding.Succeeded)
                {
                    _submission = _lastBinding.Submission;
                    _lastAction = "绑定成功：已复制外部数据并生成 PreparedPacket20。";
                }
                else
                {
                    _lastAction = "绑定失败：旧的 submission 保留，未产生新的可编码提交物。";
                }

                return true;

            case 'e':
                _lastEncoding = _submission is null
                    ? EncodeResult.Failure("尚未绑定 submission，不能编码。")
                    : DemoPacket20Wire.Encode(_submission);
                _lastAction = "编码动作已执行。";
                return true;

            case 'm':
                _external.Tiles[0].HasColor = !_external.Tiles[0].HasColor;
                _external.Tiles[0].Color++;
                _external.Tiles[0].TileType++;
                _lastAction = "只修改了外部 Tile[0]；没有重新绑定。";
                _lastEncoding = null;
                return true;

            case 'v':
                _external.Tiles[1].HasWall = false;
                _external.Tiles[1].HasWallColor = true;
                _lastAction = "已制造非法字段组：Tile[1].WallColor 存在但 Wall 缺失。";
                _lastEncoding = null;
                return true;

            case 'l':
                _external.MaxPayloadBytes = 20;
                _lastAction = "外部预算已改为 20；需要重新绑定才能进入 submission。";
                _lastEncoding = null;
                return true;

            case 'h':
                _external.MaxPayloadBytes = 70;
                _lastAction = "外部预算已恢复为 70；需要重新绑定才能进入 submission。";
                _lastEncoding = null;
                return true;

            case 'z':
                foreach (var tile in _external.Tiles)
                {
                    tile.Active = false;
                    tile.HasColor = false;
                    tile.HasWall = false;
                    tile.HasWallColor = false;
                    tile.HasLiquid = false;
                }

                _lastAction = "所有 Tile 已失活；重新绑定后将验证 20 字节最小段约束。";
                _lastEncoding = null;
                return true;

            case 'r':
                _external = ExternalPacket20Context.CreateDefault();
                _submission = null;
                _lastBinding = null;
                _lastEncoding = null;
                _lastAction = "已重置演示状态。";
                return true;

            case 'q':
                return false;

            default:
                _lastAction = $"未知操作 '{action}'；状态未改变。";
                return true;
        }
    }

    private void Render()
    {
        ClearFrame();
        WriteHeader("上下文绑定 API 原型（THROWAWAY）");
        Console.WriteLine("问题：外部只提供数据，框架绑定后是否能形成脱离外部上下文的可重复提交物？");
        Console.WriteLine();

        WriteSection("当前动作");
        Console.WriteLine(_lastAction);
        Console.WriteLine();

        WriteSection("1. 外部提供的上下文数据（可变）");
        Console.WriteLine($"Region: {_external.Width}x{_external.Height} | Tiles: {_external.Tiles.Count} | MaxBudget: {_external.MaxPayloadBytes}");
        for (var index = 0; index < _external.Tiles.Count; index++)
        {
            Console.WriteLine($"  Tile[{index}] {Format(_external.Tiles[index])}");
        }

        Console.WriteLine();
        WriteSection("2. Binder 结果");
        if (_lastBinding is null)
        {
            Console.WriteLine("尚未执行 bind。");
        }
        else if (_lastBinding.Succeeded)
        {
            Console.WriteLine("成功：PreparedPacket20 已生成，数据已复制到不可变数组。");
        }
        else
        {
            Console.WriteLine("失败：");
            foreach (var error in _lastBinding.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        Console.WriteLine();
        WriteSection("3. 当前 PreparedPacket20（编码不再读取外部数据）");
        if (_submission is null)
        {
            Console.WriteLine("无 submission。");
        }
        else
        {
            Console.WriteLine($"Snapshot: {_submission.Width}x{_submission.Height} | Budget: {_submission.Budget.MaxBytes}");
            foreach (var tile in _submission.Tiles)
            {
                Console.WriteLine($"  Tile[{tile.Index}] Presence={tile.Presence} {Format(tile)}");
            }
        }

        Console.WriteLine();
        WriteSection("4. Wire 编码 / SegmentComposer");
        Console.WriteLine("Schema bounds: [20,70]；失败时 staging 不提交到 final output。");
        if (_lastEncoding is null)
        {
            Console.WriteLine("尚未执行 encode。");
        }
        else
        {
            Console.WriteLine($"{(_lastEncoding.Succeeded ? "成功" : "失败")}: {_lastEncoding.Message}");
            if (_lastEncoding.Succeeded)
            {
                Console.WriteLine($"  Hex: {Convert.ToHexString(_lastEncoding.Bytes)}");
            }
        }

        Console.WriteLine();
        WriteSection("操作");
        Console.WriteLine("[b] bind  [e] encode  [m] 修改外部数据  [v] 非法字段组  [l] budget=20  [h] budget=70");
        Console.WriteLine("[z] 全部失活  [r] reset  [q] quit");
    }

    private static void ClearFrame()
    {
        if (!Console.IsOutputRedirected)
        {
            try
            {
                Console.Clear();
                return;
            }
            catch (IOException)
            {
                // Some hosted terminals report a console but do not expose a
                // clearable buffer. A separator still gives each frame a boundary.
            }
        }

        Console.WriteLine();
        Console.WriteLine(new string('-', 72));
        Console.WriteLine();
    }

    private static string Format(ExternalTileContext tile) =>
        $"Active={tile.Active}, Type={tile.TileType}, Color={(tile.HasColor ? tile.Color : "-")}, " +
        $"Wall={(tile.HasWall ? tile.WallType : "-")}, WallColor={(tile.HasWallColor ? tile.WallColor : "-")}, " +
        $"Liquid={(tile.HasLiquid ? tile.Liquid : "-")}";

    private static string Format(PreparedTile20 tile) =>
        $"Active={tile.Active}, Type={tile.TileType}, Color={(tile.HasColor ? tile.Color : "-")}, " +
        $"Wall={(tile.HasWall ? tile.WallType : "-")}, WallColor={(tile.HasWallColor ? tile.WallColor : "-")}, " +
        $"Liquid={(tile.HasLiquid ? tile.Liquid : "-")}";

    private static void WriteHeader(string text)
    {
        Console.WriteLine(new string('=', text.Length));
        Console.WriteLine(text);
        Console.WriteLine(new string('=', text.Length));
    }

    private static void WriteSection(string text)
    {
        Console.WriteLine($"--- {text} ---");
    }
}
