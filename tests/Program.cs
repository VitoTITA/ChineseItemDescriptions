using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ChineseItemDescriptions;

static void Check(string label, string expected, string actual)
{
    if (actual != expected)
        throw new Exception($"{label}：预期 {expected}，实际 {actual}");
    Console.WriteLine($"通过：{label}");
}

var entry = new ModEntry();
var knife = new GameItemElement { identifier = "kitchen_knife", flavorText = "原版描述", name = "厨刀" };
var other = new GameItemElement { identifier = "other", flavorText = "其他物品的描述" };
var patch = typeof(ModEntry).Assembly.GetType("ChineseItemDescriptions.TooltipPatch")!;
var resolve = patch.GetMethod("GetDisplayFlavor", BindingFlags.NonPublic | BindingFlags.Static)!
    .CreateDelegate<Func<GameItem, string>>();
var row = File.ReadAllLines(args[0]).Single(line => line.StartsWith("kitchen_knife_desc,"));
ModHelper.Text = row.Split(',', 2)[1];
var transpiler = patch.GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static)!;
var getFlavor = AccessTools.PropertyGetter(typeof(GameItem), nameof(GameItem.flavorText));
var unmatched = new[] { new CodeInstruction(System.Reflection.Emit.OpCodes.Call, getFlavor) };
var preserved = (System.Collections.Generic.IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { unmatched })!;
Check("调用结构变化时保留原指令", getFlavor.Name, ((MethodInfo)preserved.Single().operand).Name);
var target = AccessTools.DeclaredMethod(typeof(GameItemElement), "GetTooltipBasic");
var original = PatchProcessor.GetOriginalInstructions(target);
var replaced = (System.Collections.Generic.IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { original })!;
Check("两处描述读取均改走同一替换逻辑", "2", replaced.Count(i => i.Calls(resolve.Method)).ToString());
new Harmony("test.zh_item_descriptions").PatchAll(typeof(ModEntry).Assembly);
entry.Init(new ModManifest { ID = "zh_item_descriptions" });
Check("启用前保留原描述", "原版描述", knife.GetTooltip());
entry.OnEnable();
Check("简体中文完整替换厨刀描述", "这是一把刀", knife.GetTooltip());
Check("未登记物品保持原描述", "其他物品的描述", other.GetTooltip());
Check("厨刀名称保持原值", "厨刀", knife.name);
Check("厨刀数据中的描述保持原值", "原版描述", knife.flavorText);
LocHelper.Code = "en";
Check("切到英文后保留游戏返回的描述", "原版描述", knife.GetTooltip());
LocHelper.Code = "zh-Hant";
Check("繁体中文不套用简体文案", "原版描述", knife.GetTooltip());
LocHelper.Code = "zh";
Check("切回简体中文后恢复替换", "这是一把刀", knife.GetTooltip());
ModHelper.Text = "";
Check("空翻译回退", "原版描述", knife.GetTooltip());
ModHelper.Text = "kitchen_knife_desc";
Check("缺键时回退", "原版描述", knife.GetTooltip());
ModHelper.Text = "这是一把刀";
entry.OnDisable();
Check("停用后恢复原描述", "原版描述", knife.GetTooltip());

// 只在独立测试中模拟游戏 API，发布的 DLL 始终引用游戏程序集。
public interface IMod { void Init(ModManifest manifest); void OnEnable(); void OnDisable(); }
public class ModManifest { public string ID = ""; }
public class ModLog
{
    public ModLog(ModManifest manifest) { }
    public void Warning(string message) { Console.WriteLine(message); }
}
public class GameItem
{
    public string identifier { get; set; } = "";
    public string flavorText { get; set; } = "";
    public string name { get; set; } = "";
}
public class RichTextBuilder
{
    public RichTextBuilder(string text) { Text = text; }
    public string Text { get; }
}
public class GameItemElement : GameItem
{
    public string GetTooltip() => GetTooltipBasic().Text;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private RichTextBuilder GetTooltipBasic() => new RichTextBuilder(flavorText == "" ? "" : flavorText);
}
public static class LocHelper
{
    public static string Code = "zh";
    public static string GetCurrentLocaleCode() => Code;
}
public static class ModHelper
{
    public static string Text = "";
    public static string GetLocalized(string modId, string key, params object[] args) => Text;
}
