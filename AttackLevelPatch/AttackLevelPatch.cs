using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;

namespace AttackLevelPatch
{
    // Declares our mod to Bepin
    [BepInPlugin(ModId, ModName, Version)]
    // The game our mod is associated with
    [BepInProcess("Rounds.exe")]
    public class AttackLevelPatch : BaseUnityPlugin
    {
        private const string ModId = "com.willuwontu.rounds.attacklevelPatch";
        private const string ModName = "AttackLevelPatch";
        public const string Version = "0.0.0"; // What version are we on (major.minor.patch)?

        public static AttackLevelPatch instance { get; private set; }

        void Awake()
        {
            instance = this;

            // Use this to call any harmony patch files your mod may have
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
        }
    }


    [HarmonyPatch(typeof(AttackLevel))]
    static class AttackLevel_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch("Start")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            var leaveOneBehind = AccessTools.Method(typeof(AttackLevel_Patch), nameof(AttackLevel_Patch.LeaveOneBehind), new Type[] { typeof(AttackLevel) });

            int start = 0;
            int end = 0;

            //for (var i = 0; i < codes.Count; i++)
            //{
            //    UnityEngine.Debug.Log($"{i}: {codes[i].opcode}, {codes[i].operand}");
            //}

            for (int i = 0; i< codes.Count(); i++)
            {
                var code = codes[i];

                if (code.opcode == OpCodes.Ldc_I4_0)
                {
                    start = i;
                }

                if (code.opcode == OpCodes.Ret)
                {
                    end = i;
                }
            }

            codes.RemoveRange(start, end - start);

            codes.InsertRange(start, new CodeInstruction[] { 
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, leaveOneBehind)
            });

            //for (var i = 0; i < codes.Count; i++)
            //{
            //    UnityEngine.Debug.Log($"{i}: {codes[i].opcode}, {codes[i].operand}");
            //}

            return codes;
        }

        static void LeaveOneBehind(AttackLevel attackLevel)
        {
            AttackLevel[] attackLevels = attackLevel.transform.root.GetComponentsInChildren<AttackLevel>();

            AttackLevel[] filteredLevels = attackLevels.Where(attack => attack.gameObject.name == attackLevel.gameObject.name).ToArray();

            if (!(filteredLevels.Length > 1))
            {
                return;
            }

            if (filteredLevels[0] == attackLevel)
            {
                return;
            }

            filteredLevels[0].LevelUp();
            UnityEngine.GameObject.Destroy(attackLevel.gameObject);
        }
    }
}
