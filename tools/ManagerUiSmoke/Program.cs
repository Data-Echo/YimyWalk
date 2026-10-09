using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

internal static class Program
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static object host = null!;
    static Form manager = null!;
    [STAThread]
    static int Main(string[] args)
    {
        global::ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        var app = Assembly.Load("YimyWalk.App");
        if (args.Contains("--preview-pixels"))
        {
            RunPreviewPixelRegression(app);
            return 0;
        }
        host = Activator.CreateInstance(app.GetType("YimyWalk.App.Host.AppHost")!, true)!;
        var temp = Path.Combine(Path.GetTempPath(), "YimyWalk-manager-smoke-" + Guid.NewGuid().ToString("N"));
        var characterTemp = Path.Combine(Path.GetTempPath(), "YimyWalk-manager-characters-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(characterTemp);
        CopyCharacterFixture("slime", characterTemp);
        CopyCharacterFixture("cat", characterTemp);
        SetProp(host, "CharacterRootOverride", characterTemp);
        // Isolate all saves from the user's settings, including failure cases.
        var store = Activator.CreateInstance(app.GetType("YimyWalk.App.Settings.SettingsStore")!, new object[] { temp });
        host.GetType().GetField("_settingsStore", Flags)!.SetValue(host, store);
        try
        {
            Call(host, "Start");
            manager = (Form)Field(host, "_manager");
            Require(!manager.Visible, "manager starts hidden from tray");
            Call(host, "ShowManager");
            Pump(100);
            if (args.Contains("--baseline"))
            {
                AssertBrandLogo();
                foreach (var baselinePage in new[] { "Overview", "Pets", "Characters", "Interactions", "Settings" })
                {
                    Page(baselinePage);
                    SaveShot("antd-" + baselinePage.ToLowerInvariant());
                    AssertPageLayout(baselinePage);
                }
                Page("Characters");
                foreach (var c in Descendants(manager).Where(c => c.Visible && (c is Label || c is Button)))
                    Console.WriteLine($"BASELINE {c.GetType().Name} {c.Text} bounds={c.Bounds} preferred={c.GetPreferredSize(new Size(c.Width, 0))} parent={c.Parent?.ClientSize}");
                Require(Descendants(manager).OfType<Label>().Count(c => c.Visible && c.Text == "角色") == 1, "one main heading per page");
                return 0;
            }
            foreach (var page in new[] { "Overview", "Pets", "Characters", "Interactions", "Settings" })
            {
                Page(page);
                var ph = (Control)Field(manager, "_pageHost");
                Console.WriteLine($"GEOMETRY {page} dpi={manager.DeviceDpi} pageHost={manager.RectangleToClient(ph.RectangleToScreen(ph.ClientRectangle))}");
            }
            Page("Overview");
            SaveShot("overview");
            foreach (var label in Descendants(manager).OfType<Label>().Where(label => label.Text.Contains("YimyWalk", StringComparison.Ordinal)))
                Console.WriteLine($"LABEL text={label.Text} bounds={label.Bounds} parent={label.Parent?.GetType().Name} parentBounds={label.Parent?.Bounds} visible={label.Visible}");
            var sidebar = Descendants(manager).First(control => control.Name == "Sidebar");
            Console.WriteLine($"SIDEBAR bounds={sidebar.Bounds} client={sidebar.ClientSize}");
            manager.Close();
            Pump(40);
            Require(!manager.Visible && !manager.IsDisposed, "close hides manager to tray");
            for (var round = 0; round < 20; round++)
            {
                Call(host, "ShowManager");
                Pump(10);
                Call(host, "HideManagerToTray");
                Pump(10);
            }
            Call(host, "ShowManager");
            Page("Pets");
            Call(host, "QueueCreate", "cat", 100);
            Call(host, "ClearPets");
            Pump(120);
            Require((int)Prop(host, "ActivePetCount") == 0 && (int)Prop(host, "PendingCreationCount") == 0, "clear cancels queued creation");
            Console.WriteLine("CLEAR pending_queue=PASS");
            // Keep the mixed-role regression small enough that the global 1.8x
            // preview scale does not create dozens of multi-hundred-pixel
            // layered windows during this UI-only smoke.
            Call(host, "CreateMany", "cat", 2);
            Call(host, "CreateMany", "slime", 2);
            Pump(200);
            var activeInstances = ((IEnumerable)Prop(host, "ActivePets")).Cast<object>().ToArray();
            Require(activeInstances.Length >= 2, "mixed instances created");
            var customId = (Guid)Prop(activeInstances[0], "InstanceId");
            var inheritedId = (Guid)Prop(activeInstances[1], "InstanceId");
            Require((bool)Call(host, "SetPetScaleOverride", customId, 0.6f)!, "set instance scale override");
            SetProp(host, "GlobalScale", 1.8f);
            Require(Math.Abs((float)Prop(activeInstances[0], "ScaleOverride") - 0.6f) < 0.001f,
                "custom instance keeps its size when global scale changes");
            Require(Prop(activeInstances[1], "ScaleOverride") is null, "other instance still follows global size");
            Require(Math.Abs((float)Call(activeInstances[1], "ResolveScale", 1.8f)! - 1.8f) < 0.001f,
                "inherited instance resolves current global size");
            Require((bool)Call(host, "ResetPetScaleOverride", customId)!, "reset instance scale override");
            Require(Prop(activeInstances[0], "ScaleOverride") is null, "reset restores global size inheritance");
            Console.WriteLine("INSTANCE_SCALE custom=0.6 global=1.8 reset=PASS");
            var list = Descendants(manager).OfType<ListView>().Single(listView => listView.Visible);
            list.Items[0].Selected = true;
            Pump(40);
            var petsPage = VisiblePage();
            var scaleLabel = FindText(petsPage, "实例大小");
            Require(scaleLabel is { Visible: true } && IsFullyVisible(scaleLabel),
                "pets instance scale setting is visible after selecting an instance");
            var scaleMode = Field(petsPage, "_scaleMode");
            var scaleInput = Field(petsPage, "_scale");
            Require(IsFullyVisible((Control)scaleMode) && IsFullyVisible((Control)scaleInput),
                "pets instance scale controls are fully visible after selecting an instance");
            var selectedId = (Guid)list.Items[0].Tag!;
            SetProp(scaleMode, "SelectedIndex", 1);
            SetProp(scaleInput, "Value", 0.7m);
            Require(Math.Abs((float)Prop(Call(host, "FindPet", selectedId)!, "ScaleOverride") - 0.7f) < 0.001f,
                "pets page applies custom instance size through the host");
            SetProp(scaleMode, "SelectedIndex", 0);
            Require(Prop(Call(host, "FindPet", selectedId)!, "ScaleOverride") is null,
                "pets page reset returns instance size to global inheritance");
            Console.WriteLine("INSTANCE_SCALE_UI selected=0.7 reset=PASS");
            Page("Interactions");
            var interactions = VisiblePage();
            SetProp(Field(interactions, "_jumpDesire"), "Value", 35m);
            SetProp(Field(interactions, "_climbDesire"), "Value", 65m);
            SetProp(Field(interactions, "_windowJumpDesire"), "Value", 55m);
            Require(Math.Abs((float)Prop(host, "JumpDesire") - 0.35f) < 0.001f, "global jump desire maps UI percent to 0-1");
            Require(Math.Abs((float)Prop(host, "ClimbDesire") - 0.65f) < 0.001f, "global climb desire maps UI percent to 0-1");
            Require(Math.Abs((float)Prop(host, "WindowJumpDesire") - 0.55f) < 0.001f, "global window jump desire maps UI percent to 0-1");
            Console.WriteLine("GLOBAL_DESIRE_UI jump=35% climb=65% window=55% mapping=PASS");
            Page("Pets");
            list = Descendants(manager).OfType<ListView>().Single(listView => listView.Visible);
            if (list.Items.Count > 0) list.Items[0].Selected = true;
            Pump(30);
            var pets = VisiblePage();
            SetProp(Field(pets, "_jumpDesireMode"), "SelectedIndex", 1);
            SetProp(Field(pets, "_jumpDesire"), "Value", 70m);
            SetProp(Field(pets, "_climbDesireMode"), "SelectedIndex", 1);
            SetProp(Field(pets, "_climbDesire"), "Value", 80m);
            SetProp(Field(pets, "_windowJumpDesireMode"), "SelectedIndex", 1);
            SetProp(Field(pets, "_windowJumpDesire"), "Value", 90m);
            var behavior = Prop(Call(host, "FindPet", selectedId)!, "BehaviorOverrides");
            Require(Math.Abs((float)Prop(behavior, "JumpDesireOverride")! - 0.7f) < 0.001f, "pets page stores jump desire override");
            Require(Math.Abs((float)Prop(behavior, "ClimbDesireOverride")! - 0.8f) < 0.001f, "pets page stores climb desire override");
            Require(Math.Abs((float)Prop(behavior, "WindowJumpDesireOverride")! - 0.9f) < 0.001f, "pets page stores window jump desire override");
            Require((bool)Call(host, "ResetPetBehaviorOverrides", selectedId)!, "reset clears desire overrides");
            behavior = Prop(Call(host, "FindPet", selectedId)!, "BehaviorOverrides");
            Require(Prop(behavior, "JumpDesireOverride") is null && Prop(behavior, "ClimbDesireOverride") is null
                && Prop(behavior, "WindowJumpDesireOverride") is null,
                "reset returns desire overrides to global inheritance");
            Console.WriteLine("INSTANCE_DESIRE_UI jump=70% climb=80% window=90% reset=PASS");
            var runtime = Descendants(manager).OfType<Label>().First(label => label.Visible && label.Text.StartsWith("状态：", StringComparison.Ordinal));
            var runtimeBefore = runtime.Text;
            Pump(500);
            Require(runtime.Text != runtimeBefore, "visible manager samples changing runtime state without input");
            Console.WriteLine("AUTO_REFRESH interval=200ms DETAIL_STATE=PASS");
            SaveShot("pets");
            for (var step = 0; list.Items.Count > 0; step++)
            {
                var index = step < 4 ? 0 : step % 2 == 0 ? list.Items.Count - 1 : list.Items.Count / 2;
                foreach (ListViewItem row in list.Items) row.Selected = false;
                var target = list.Items[index];
                target.Selected = true;
                target.EnsureVisible();
                var id = (Guid)target.Tag!;
                Console.WriteLine($"DELETE step={step} count={list.Items.Count} index={index} id={id}");
                ((IButtonControl)ButtonControls(manager).Single(b => b.Text == "删除选中")).PerformClick();
                Pump(30);
                Require(Call(host, "FindPet", id) is null, "deleted target id");
                Require(list.Items.Count == (int)Prop(host, "ActivePetCount"), "list/core counts");
            }
            Console.WriteLine("PASS mixed head/middle/tail deletion");
            return 0;
        }
        catch (Exception error) { Console.WriteLine(error); return 1; }
        finally
        {
            ((IDisposable)host).Dispose();
            if (Directory.Exists(temp)) Directory.Delete(temp, true);
            if (Directory.Exists(characterTemp)) Directory.Delete(characterTemp, true);
        }
    }

    static void RunPreviewPixelRegression(Assembly app)
    {
        var cacheType = app.GetType("YimyWalk.App.Rendering.FrameSurfaceCache")!;
        var previewType = app.GetType("YimyWalk.App.UI.Controls.CharacterFramePreview")!;
        using var cache = (IDisposable)Activator.CreateInstance(cacheType, true)!;
        foreach (var source in new[]
        {
            new Size(16, 16), new Size(192, 192),
            new Size(64, 32), new Size(192, 96),
            new Size(32, 64), new Size(96, 192)
        })
        foreach (var dpiSize in new[] { 96, 120, 144, 192 })
        {
            using var definition = CreatePreviewDefinition(source);
            using var preview = (Control)Activator.CreateInstance(previewType, new[] { cache })!;
            preview.Size = new Size(dpiSize, dpiSize);
            preview.CreateControl();
            previewType.GetMethod("ShowIdleFrame", Flags)!.Invoke(preview, new object?[] { definition, 2f });
            using var rendered = new Bitmap(preview.ClientSize.Width, preview.ClientSize.Height);
            preview.DrawToBitmap(rendered, preview.ClientRectangle);
            foreach (var marker in new[] { Color.Red, Color.Lime, Color.Blue, Color.Magenta })
                Require(ContainsMarker(rendered, marker), $"preview preserves {source.Width}x{source.Height} {dpiSize}px marker {marker.Name}");
            var nonBackground = FindMarkerBounds(rendered);
            Require(nonBackground.Width > 0 && nonBackground.Height > 0, $"preview draws {source.Width}x{source.Height} at {dpiSize}px");
            Require(nonBackground.Left > 0 && nonBackground.Top > 0
                && nonBackground.Right < rendered.Width - 1 && nonBackground.Bottom < rendered.Height - 1,
                $"preview leaves visible margins for {source.Width}x{source.Height} at {dpiSize}px bounds={nonBackground}");
            var sourceRatio = source.Width / (double)source.Height;
            var renderedRatio = nonBackground.Width / (double)nonBackground.Height;
            Require(Math.Abs(sourceRatio - renderedRatio) < 0.08, $"preview preserves aspect ratio {source.Width}x{source.Height} at {dpiSize}px");
        }
        Console.WriteLine("PREVIEW_PIXELS square/wide/tall 96/120/144/192 DPI UI-render PASS (OS DPI matrix not exercised)");
    }

    static YimyWalk.Core.Characters.CharacterDefinition CreatePreviewDefinition(Size size)
    {
        var frames = Enumerable.Range(0, 9)
            .Select(index => new YimyWalk.Core.Characters.CharacterFrame(index, size.Width, size.Height,
                BuildPreviewPixels(size), index < 3 ? YimyWalk.Core.Characters.FrameKind.Idle : YimyWalk.Core.Characters.FrameKind.WalkRight))
            .ToArray();
        return new YimyWalk.Core.Characters.CharacterDefinition
        {
            Id = "preview",
            DisplayName = "Preview",
            CellWidth = size.Width,
            CellHeight = size.Height,
            Scale = 1,
            Anchor = new YimyWalk.Core.Characters.Anchor(0.5f, 0.9f),
            Frames = frames,
            Movement = new YimyWalk.Core.Characters.CharacterMovement(1, 1, 1, 1),
            Defaults = new YimyWalk.Core.Characters.CharacterDefaults(1, 2, 1),
            Dialogue = YimyWalk.Core.Characters.DialogueSet.Default,
        };
    }

    static ReadOnlyMemory<byte> BuildPreviewPixels(Size size)
    {
        var pixels = new byte[size.Width * size.Height * 4];
        for (var y = 0; y < size.Height; y++)
        for (var x = 0; x < size.Width; x++)
        {
            var color = x < size.Width / 2
                ? (y < size.Height / 2 ? Color.Red : Color.Blue)
                : (y < size.Height / 2 ? Color.Lime : Color.Magenta);
            var offset = (y * size.Width + x) * 4;
            pixels[offset] = color.R; pixels[offset + 1] = color.G; pixels[offset + 2] = color.B; pixels[offset + 3] = 255;
        }
        return pixels;
    }

    static bool ContainsMarker(Bitmap bitmap, Color expected)
    {
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
        {
            var actual = bitmap.GetPixel(x, y);
            if (actual.A > 200 && Math.Abs(actual.R - expected.R) < 8 && Math.Abs(actual.G - expected.G) < 8 && Math.Abs(actual.B - expected.B) < 8)
                return true;
        }
        return false;
    }

    static Rectangle FindMarkerBounds(Bitmap bitmap)
    {
        var bounds = Rectangle.Empty;
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
        {
            var pixel = bitmap.GetPixel(x, y);
            var marker = (pixel.R > 220 && pixel.G < 40 && pixel.B < 40) ||
                         (pixel.R < 40 && pixel.G > 220 && pixel.B < 40) ||
                         (pixel.R < 40 && pixel.G < 40 && pixel.B > 220) ||
                         (pixel.R > 180 && pixel.B > 180 && pixel.G < 80);
            if (marker) bounds = bounds.IsEmpty ? new Rectangle(x, y, 1, 1) : Rectangle.Union(bounds, new Rectangle(x, y, 1, 1));
        }
        return bounds;
    }
    static void Page(string name)
    {
        var type = manager.GetType().Assembly.GetType("YimyWalk.App.UI.ManagerPageId")!;
        Call(manager, "SelectPage", Enum.Parse(type, name));
        Pump(30);
    }
    static object Prop(object obj, string name) => obj.GetType().GetProperty(name, Flags)!.GetValue(obj)!;
    static object? Property(object obj, string name) => obj.GetType().GetProperty(name, Flags)!.GetValue(obj);
    static void SetProp(object obj, string name, object value) => obj.GetType().GetProperty(name, Flags)!.SetValue(obj, value);
    static object Field(object obj, string name) => obj.GetType().GetField(name, Flags)!.GetValue(obj)!;
    static object? Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags)!.Invoke(obj, args);
    static void CopyCharacterFixture(string role, string destinationRoot)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../"));
        var source = role.Equals("cat", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(repoRoot, "tests", "Fixtures", "Characters", role)
            : Path.Combine(repoRoot, "Characters", role);
        var destination = Path.Combine(destinationRoot, role);
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
    }
    static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    static IEnumerable<Control> ButtonControls(Control root) => Descendants(root).Where(control => control is IButtonControl);
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static void SaveShot(string name)
    {
        var dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../artifacts/ui-smoke"));
        Directory.CreateDirectory(dir);
        using var bitmap = new Bitmap(manager.ClientSize.Width, manager.ClientSize.Height);
        manager.DrawToBitmap(bitmap, manager.ClientRectangle);
        bitmap.Save(Path.Combine(dir, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
        Console.WriteLine($"SCREENSHOT {Path.Combine(dir, name + ".png")}");
    }
    static void AssertPageLayout(string name)
    {
        var page = VisiblePage();
        var required = name switch
        {
            "Overview" => new[] { "YimyWalk", "桌面状态", "快捷操作", "创建桌宠" },
            "Pets" => new[] { "桌宠", "未选择桌宠" },
            "Characters" => new[] { "角色", "角色列表", "角色 ID：" },
            "Interactions" => new[] { "互动", "全局默认", "运行级限制" },
            "Settings" => new[] { "设置", "应用外观与启动行为", "保存状态" },
            _ => Array.Empty<string>()
        };
        foreach (var text in required)
            Require(FindText(page, text) is { Visible: true, Height: > 0 }, $"{name} visible text: {text}");

        foreach (var label in Descendants(page).OfType<Label>().Where(label => label.Visible && label.Text.Length > 0))
        {
            if (label.Parent is ScrollableControl scroll && scroll.AutoScroll && !scroll.ClientRectangle.Contains(label.Bounds))
            {
                scroll.ScrollControlIntoView(label);
                Application.DoEvents();
            }
            Require(label.Parent is not null && label.Parent.ClientRectangle.Contains(label.Bounds),
                $"{name} label is inside parent: {label.Text}");
            var preferred = label.GetPreferredSize(new Size(Math.Max(1, label.Width), 0));
            Require(preferred.Height <= label.Height + 2, $"{name} label is not vertically clipped: {label.Text}");
        }
        foreach (var siblings in Descendants(page).Where(control => control.Visible && control is Label).GroupBy(control => control.Parent))
        {
            if (siblings.Key is Panel heading && heading.Height <= 80) continue;
            var labels = siblings.Cast<Label>().ToArray();
            for (var i = 0; i < labels.Length; i++)
            for (var j = i + 1; j < labels.Length; j++)
                Require(!labels[i].Bounds.IntersectsWith(labels[j].Bounds), $"{name} sibling labels do not overlap: {labels[i].Text} / {labels[j].Text}");
        }

        if (name == "Interactions")
        {
            var switchControls = Descendants(page).Where(control => control.GetType().FullName?.Contains("AntdUI", StringComparison.Ordinal) == true).ToArray();
            Require(switchControls.Count(control => control.GetType().Name == "Switch") >= 5, "interactions uses AntdUI switches");
            Require(Descendants(page).Any(control => control.GetType().Name == "InputNumber"), "interactions uses AntdUI numeric input");
            var desireInputs = Descendants(page).Where(control => control.GetType().Name == "InputNumber"
                && Convert.ToDecimal(Property(control, "Minimum")) == 0m
                && Convert.ToDecimal(Property(control, "Maximum")) == 100m).ToArray();
            Require(desireInputs.Length >= 3, "interactions exposes 0-100 jump/climb/window jump desire inputs");
            Require(page is ScrollableControl scrollable && scrollable.VerticalScroll.Maximum > 0, "interactions page exposes vertical scrolling for full content");
        }
        if (name == "Settings")
        {
            Require(Descendants(page).Any(control => control.GetType().Name == "Switch"), "settings uses AntdUI switch");
            Require(Descendants(page).Any(control => control.GetType().Name == "Select"), "settings uses AntdUI select");
            Require(Descendants(page).Any(control => control.GetType().Name == "InputNumber"), "settings uses AntdUI numeric input");
        }
        if (name == "Pets")
        {
            Require(FindText(page, "实例大小") is { Visible: true }, "pets exposes per-instance size setting");
            Require(Descendants(page).Any(control => control.GetType().Name == "Select"), "pets uses AntdUI scale source select");
            Require(Descendants(page).Any(control => control.GetType().Name == "InputNumber" &&
                Convert.ToDecimal(Property(control, "Minimum")) == 0.1m &&
                Convert.ToDecimal(Property(control, "Maximum")) == 8m), "pets uses AntdUI scale input");
            var desireInputs = Descendants(page).Where(control => control.GetType().Name == "InputNumber"
                && Convert.ToDecimal(Property(control, "Minimum")) == 0m
                && Convert.ToDecimal(Property(control, "Maximum")) == 100m).ToArray();
            Require(desireInputs.Length >= 3, "pets exposes per-instance jump/climb/window jump desire inputs");
        }
        if (name == "Characters")
        {
            var roleList = Descendants(page).OfType<ListView>().SingleOrDefault(list => list.Visible);
            Require(roleList is not null && roleList.Columns.Count >= 4 && roleList.Items.Count > 0, "characters uses a role list with status columns");
            Require(Descendants(page).Any(control => control.GetType().Name == "Button" && control.Text?.Contains("默认", StringComparison.Ordinal) == true), "characters has default-role action");
            var preview = Descendants(page).FirstOrDefault(control => control.GetType().Name == "CharacterFramePreview");
            Require(preview is not null && preview.Width >= 220 && preview.Height >= 220, "character preview has usable size");
        }
    }

    static void AssertBrandLogo()
    {
        Require(FindText(manager, "点击托盘图标打开主菜单") is null,
            "sidebar footer no longer shows the tray instruction");
        var logo = Descendants(manager).SingleOrDefault(control => control.Name == "BrandLogo");
        Require(logo is PictureBox picture && picture.Image is not null,
            "sidebar brand logo has an embedded image");
        Require(logo!.Visible && logo.Parent is not null && logo.Parent.ClientRectangle.Contains(logo.Bounds),
            "sidebar brand logo is fully inside its visible footer");
        Console.WriteLine($"BRAND_LOGO image=PASS bounds={logo.Bounds} parent={logo.Parent!.ClientSize}");
    }
    static Control VisiblePage()
    {
        var pageHost = (Control)Field(manager, "_pageHost");
        return pageHost.Controls.Cast<Control>().Single(page => page.Visible);
    }
    static Control? FindText(Control root, string text)
        => Descendants(root).FirstOrDefault(control => control.Text.Equals(text, StringComparison.Ordinal) || control.Text.StartsWith(text, StringComparison.Ordinal));
    static bool IsFullyVisible(Control control)
    {
        if (!control.Visible) return false;
        var bounds = control.RectangleToScreen(control.ClientRectangle);
        for (var parent = control.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is not ScrollableControl scrollable) continue;
            var clip = scrollable.RectangleToScreen(scrollable.ClientRectangle);
            if (!clip.Contains(bounds)) return false;
        }
        return true;
    }
    static void Pump(int milliseconds)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < milliseconds) { Application.DoEvents(); Thread.Sleep(5); }
    }
}
