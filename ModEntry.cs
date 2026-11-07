using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ChineseItemDescriptions
{
    public sealed class ModEntry : IMod
    {
        private string? modId;

        public void Init(ModManifest manifest)
        {
            modId = manifest.ID;
            if (!TooltipPatch.Applied)
                new ModLog(manifest).Warning("当前版本的物品提示框结构与预期不符，将显示原版描述。");
        }

        public void OnEnable() => TooltipPatch.ActiveModId = modId;

        public void OnDisable() => TooltipPatch.ActiveModId = null;
    }

    [HarmonyPatch(typeof(GameItemElement), "GetTooltipBasic", new Type[0])]
    internal static class TooltipPatch
    {
        private static readonly Dictionary<string, string> DescriptionKeys = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["kitchen_knife"] = "kitchen_knife_desc"
        };

        internal static string? ActiveModId { get; set; }
        internal static bool Applied { get; private set; }

        private static bool Prepare()
        {
            var target = AccessTools.DeclaredMethod(typeof(GameItemElement), "GetTooltipBasic", Type.EmptyTypes);
            return target != null && !target.IsStatic && target.ReturnType == typeof(RichTextBuilder);
        }

        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var getter = AccessTools.PropertyGetter(typeof(GameItem), nameof(GameItem.flavorText));
            var replacement = AccessTools.DeclaredMethod(typeof(TooltipPatch), nameof(GetDisplayFlavor));

            // 当前游戏先检查描述是否为空，再读一次用于绘制。调用结构变化时保留原指令。
            Applied = getter != null && replacement != null && code.Count(i => i.Calls(getter)) == 2;
            return Applied ? code.MethodReplacer(getter, replacement) : code;
        }

        private static string GetDisplayFlavor(GameItem item)
        {
            var modId = ActiveModId;
            var itemId = item.identifier;
            if (modId != null && itemId != null && DescriptionKeys.TryGetValue(itemId, out var key)
                && LocHelper.GetCurrentLocaleCode() == "zh")
            {
                var text = ModHelper.GetLocalized(modId, key);
                if (!string.IsNullOrWhiteSpace(text) && text != key)
                    return text;
            }

            return item.flavorText;
        }
    }
}
