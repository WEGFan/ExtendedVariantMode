﻿using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;

namespace ExtendedVariants.Variants {
    public class BoostMultiplier : AbstractExtendedVariant {
        private static Hook liftBoostHook;
        private static bool isBoostMultiplierApplied = true;

        public override Type GetVariantType() {
            return typeof(float);
        }

        public override object GetDefaultVariantValue() {
            return 1f;
        }

        public override object GetVariantValue() {
            return Settings.BoostMultiplier;
        }

        protected override void DoSetVariantValue(object value) {
            Settings.BoostMultiplier = (float) value;
        }

        public override void SetLegacyVariantValue(int value) {
            Settings.BoostMultiplier = (value / 10f);
        }

        public override void Load() {
            liftBoostHook = new Hook(
                typeof(Player).GetMethod("get_LiftBoost", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(BoostMultiplier).GetMethod("multiplyLiftBoost", BindingFlags.NonPublic | BindingFlags.Instance), this);

            IL.Celeste.Player.NormalUpdate += modPlayerNormalUpdate;
        }

        public override void Unload() {
            liftBoostHook?.Dispose();
            liftBoostHook = null;

            IL.Celeste.Player.NormalUpdate -= modPlayerNormalUpdate;
        }

        private Vector2 multiplyLiftBoost(Func<Player, Vector2> orig, Player self) {
            Vector2 result = orig(self);
            if (!isBoostMultiplierApplied) return result;

            if (Settings.BoostMultiplier < 0f) {
                // capping has to be flipped around too!
                result.Y = Calc.Clamp(self.LiftSpeed.Y, 0f, 130f);
            }
            return result * Settings.BoostMultiplier;
        }

        private void modPlayerNormalUpdate(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // disable the boost multiplier for the first part of the method: it is intended to be able to walk off from a block seamlessly...
            // and multiplying the boost makes it *a bit* less seamless.
            cursor.EmitDelegate<Action>(() => isBoostMultiplierApplied = false);
            cursor.GotoNext(instr => instr.MatchCallvirt<Player>("get_Holding"));
            cursor.EmitDelegate<Action>(() => isBoostMultiplierApplied = true);
        }
    }
}
