﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SCommon;
using SCommon.Database;
using SCommon.PluginBase;
using SCommon.Maths;
using SCommon.Prediction;
using SCommon.Orbwalking;
using SUtility.Drawings;
using SharpDX;


namespace SAutoCarry.Champions
{
    public class Jhin : Champion
    {
        public Jhin()
            : base ("Jhin", "SAutoCarry - Jhin")
        {
            OnUpdate += BeforeOrbwalk;
            OnCombo += Combo;
            OnHarass += Harass;
        }

        public override void CreateConfigMenu()
        {
            Menu combo = new Menu("Combo", "SAutoCarry.Jhin.Combo");
            combo.AddItem(new MenuItem("SAutoCarry.Jhin.Combo.UseQ", "UseQ").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Jhin.Combo.UseW", "Use W").SetValue(true)).ValueChanged += (s, ar) => ConfigMenu.Item("SAutoCarry.Jhin.Combo.UseWMarked").Show(ar.GetNewValue<bool>());
            combo.AddItem(new MenuItem("SAutoCarry.Jhin.Combo.UseWMarked", "Use W Only If Target Is Marked").SetValue(false)).Show(combo.Item("SAutoCarry.Jhin.Combo.UseW").GetValue<bool>());
            combo.AddItem(new MenuItem("SAutoCarry.Jhin.Combo.UseEImmobile", "Use E Immobile Targets").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Jhin.Combo.UseEDashing", "Use E Dashing Targets").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Jhin.Combo.UseR", "Use R Shoots If Ult Activated").SetValue(true));

            Menu harass = new Menu("Harass", "SAutoCarry.Jhin.Harass");
            harass.AddItem(new MenuItem("SAutoCarry.Jhin.Harass.UseQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("SAutoCarry.Jhin.Harass.Toggle", "Toggle Harass").SetValue(new KeyBind('T', KeyBindType.Toggle)));

            Menu misc = new Menu("Misc", "SAutoCarry.Jhin.Misc");
            misc.AddItem(new MenuItem("SAutoCarry.Jhin.Misc.AdjustW", "Adjust W Range").SetValue(new Slider(1000, 0, 2500))).ValueChanged += (s, ar) => Spells[W].Range = ar.GetNewValue<Slider>().Value;
            misc.AddItem(new MenuItem("SAutoCarry.Jhin.Misc.AdjustR", "Adjust R Range").SetValue(new Slider(1000, 0, 2000))).ValueChanged += (s, ar) => Spells[R].Range = ar.GetNewValue<Slider>().Value;
            misc.AddItem(new MenuItem("SAutoCarry.Jhin.Misc.KillStealRW", "KillSteal").SetValue(true));

            ConfigMenu.AddSubMenu(combo);
            ConfigMenu.AddSubMenu(harass);
            ConfigMenu.AddSubMenu(misc);
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q);

            Spells[W] = new Spell(SpellSlot.W, 2500f);
            Spells[W].SetSkillshot(0.25f, 40f, 0f, false, SkillshotType.SkillshotLine);

            Spells[E] = new Spell(SpellSlot.E);

            Spells[R] = new Spell(SpellSlot.R);
            Spells[R].SetSkillshot(0f, 40f, 1200f, false, SkillshotType.SkillshotLine);
        }

        public void BeforeOrbwalk()
        {
            if (HarassToggle)
                Harass();

            if (KillStealRW)
                KillSteal();
        }

        public void Combo()
        {
            if (Spells[Q].IsReady() && ComboUseQ)
            {
                var t = TargetSelector.GetTarget(Spells[Q].Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    Spells[Q].CastOnUnit(t);
            }

            if (Spells[W].IsReady() && ComboUseW)
            {
                var t = TargetSelector.GetTarget(Spells[W].Range, TargetSelector.DamageType.Physical);
                if (t != null && (!ComboUseWMarked || t.HasBuff("jhinespotteddebuff")))
                    Spells[W].SPredictionCast(t, HitChance.High);
            }

            if (Spells[E].IsReady() && (ComboUseEDashing || ComboUseEImmobile) && Spells[E].Instance.Ammo != 0)
            {
                var t = HeroManager.Enemies.Where(p => p.IsValidTarget(Spells[E].Range) && ((p.IsImmobilized() && ComboUseEImmobile) || (p.IsDashing() && ComboUseEDashing))).OrderBy(q => q.GetPriority()).FirstOrDefault();
                if (t != null)
                    Spells[E].Cast(t.ServerPosition);
            }

            if (IsRShootCastable)
            {
                var t = TargetSelector.GetTarget(Spells[R].Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    Spells[R].SPredictionCast(t, HitChance.Low);
            }
        }

        public void Harass()
        {
            var t = TargetSelector.GetTarget(Spells[Q].Range * 2f, TargetSelector.DamageType.Physical);

            if (t != null)
            {
                var minions = MinionManager.GetMinions(Spells[Q].Range * 2f);
                foreach (var minion in minions)
                {
                    if (minion.IsValidTarget(Spells[Q].Range) && minion.Distance(t.ServerPosition) <= Spells[Q].Range)
                    {
                        var hitbox = ClipperWrapper.DefineCircle(minion.ServerPosition.To2D(), Spells[Q].Range);
                        var possibleBounces = minions.Where(p => !hitbox.IsOutside(p.ServerPosition.To2D())).OrderBy(q => q.Distance(minion.ServerPosition));
                        if (possibleBounces.Count() < 3 || possibleBounces.FirstOrDefault().Distance(minion.ServerPosition) > t.Distance(minion.ServerPosition)) // <= 3 ?
                        {
                            Spells[Q].CastOnUnit(minion);
                            return;
                        }
                        else
                        {
                            for(int i = 0; i < 3; i++) // < 4 ? 
                            {
                                if (possibleBounces.ElementAt(i).Distance(minion.ServerPosition) > t.Distance(minion.ServerPosition))
                                {
                                    Spells[Q].CastOnUnit(minion);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void KillSteal()
        {
            if (IsRShootCastable)
            {
                foreach (Obj_AI_Hero target in HeroManager.Enemies.Where(x => x.IsValidTarget(2000 /* to do: real r range */) && !x.HasBuffOfType(BuffType.Invulnerability)))
                {
                    if (CalculateDamageR(target) > target.Health + 20)
                        Spells[R].SPredictionCast(target, HitChance.High);
                }
            }
            else if (Spells[W].IsReady())
            {
                foreach (Obj_AI_Hero target in HeroManager.Enemies.Where(x => x.IsValidTarget(1200 /* to do: real w range */) && !x.HasBuffOfType(BuffType.Invulnerability)))
                {
                    if (CalculateDamageW(target) > target.Health + 20)
                        Spells[W].SPredictionCast(target, HitChance.High);
                }
            }
        }

        public override double CalculateDamageW(Obj_AI_Hero target)
        {
            if (!Spells[W].IsReady())
                return 0.0d;

            return ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, new[] { 50, 85, 120, 155, 190 }[Spells[W].Level] + ObjectManager.Player.TotalAttackDamage * 0.7);
        }

        public override double CalculateDamageR(Obj_AI_Hero target)
        {
            if (!Spells[R].IsReady())
                return 0.0d;

            return ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, (new[] { 50, 125, 200 }[Spells[R].Level] + ObjectManager.Player.TotalAttackDamage * 0.25) * ((100f - target.HealthPercent) * 0.02f + (IsLastRShoot ? 1f : 0f)));
        }

        private bool IsRShootCastable
        {
            get { return Spells[R].Instance.Name == "JhinRShot" && /*delay condition ?*/ true; }
        }

        private bool IsLastRShoot
        {
            get { return IsRShootCastable && /*buff name ?*/ ObjectManager.Player.HasBuff("jhinrlast"); }
        }

        public bool ComboUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Jhin.Combo.UseQ").GetValue<bool>(); }
        }

        public bool ComboUseW
        {
            get { return ConfigMenu.Item("SAutoCarry.Jhin.Combo.UseW").GetValue<bool>(); }
        }

        public bool ComboUseWMarked
        {
            get { return ConfigMenu.Item("SAutoCarry.Jhin.Combo.UseWMarked").GetValue<bool>(); }
        }

        public bool ComboUseEImmobile
        {
            get { return ConfigMenu.Item("SAutoCarry.Jhin.Combo.UseEImmobile").GetValue<bool>(); }
        }

        public bool ComboUseEDashing
        {
            get { return ConfigMenu.Item("SAutoCarry.Jhin.Combo.UseEDashing").GetValue<bool>(); }
        }

        public bool ComboUseR
        {
            get { return ConfigMenu.Item("SAutoCarry.Jhin.Combo.UseR").GetValue<bool>(); }
        }

        public bool HarassUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Jhin.Harass.UseQ").GetValue<bool>(); }
        }

        public bool HarassToggle
        {
            get { return ConfigMenu.Item("SAutoCarry.Jhin.Harass.Toggle").GetValue<KeyBind>().Active; }
        }

        public bool KillStealRW
        {
            get { return ConfigMenu.Item("SAutoCarry.Jhin.Misc.KillStealRW").GetValue<bool>(); }
        }
    }
}
