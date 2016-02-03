using LeagueSharp;
using LeagueSharp.Common;
using SCommon.PluginBase;
using System;
using SCommon.Prediction;
using SharpDX;
using System.Linq;

using TargetSelector = SCommon.TS.TargetSelector;

namespace SAutoCarry.Champions
{
    public class Graves : Champion
    {
        public Graves()
            : base("Graves", "SAutoCarry - Graves")
        {
            OnUpdate += BeforeOrbwalk;
            OnCombo += Combo;
            OnHarass += Harass;

            SCommon.Prediction.Prediction.predMenu.Item("SPREDDRAWINGS").SetValue(false);
        }

        public override void CreateConfigMenu()
        {
            var combo = new Menu("Combo", "SAutoCarry.Graves.Combo");
            combo.AddItem(new MenuItem("SAutoCarry.Graves.Combo.UseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Graves.Combo.UseW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("SAutoCarry.Graves.Combo.UseE", "Use E").SetValue(true)).ValueChanged += (s, ar) => combo.Item("SAutoCarry.Graves.Combo.EMode").Show(ar.GetNewValue<bool>());
            combo.AddItem(new MenuItem("SAutoCarry.Graves.Combo.EMode", "E Mode").SetValue(new StringList(new[] { "Auto Pos", "Side Pos", "Cursor Pos" }))).Show(combo.Item("SAutoCarry.Graves.Combo.UseE").GetValue<bool>());
            combo.AddItem(new MenuItem("SAutoCarry.Graves.Combo.UseR", "Use R").SetValue(true));

            var harass = new Menu("Harass", "SAutoCarry.Graves.Harass");
            harass.AddItem(new MenuItem("SAutoCarry.Graves.Harass.UseQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("SAutoCarry.Graves.Harass.Toggle", "Toggle Harass").SetValue(new KeyBind('A', KeyBindType.Toggle)));
            harass.AddItem(new MenuItem("SAutoCarry.Graves.Harass.MinMana", "Min. Mana %").SetValue(new Slider(0, 40, 100)));

            ConfigMenu.AddSubMenu(combo);
            ConfigMenu.AddSubMenu(harass);
            ConfigMenu.AddToMainMenu();
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 820f);
            Spells[Q].SetSkillshot(0.26f, 10f * 2 * (float)Math.PI / 180, 1950, false, SkillshotType.SkillshotCone);

            Spells[W] = new Spell(SpellSlot.W, 850f);
            Spells[W].SetSkillshot(0.30f, 250f, 1650f, false, SkillshotType.SkillshotCircle);

            Spells[E] = new Spell(SpellSlot.E, 425f);

            Spells[R] = new Spell(SpellSlot.R, 1100f);
            Spells[R].SetSkillshot(0.22f, 150f, 2100, true, SkillshotType.SkillshotLine);
        }

        public void BeforeOrbwalk()
        {
            if (HarassToggle && Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.None)
                Harass();
        }

        protected override void OrbwalkingEvents_AfterAttack(SCommon.Orbwalking.AfterAttackArgs args)
        {
            if (args.Target != null && args.Target is Obj_AI_Hero && Orbwalker.ActiveMode == SCommon.Orbwalking.Orbwalker.Mode.Combo)
            {
                var t = args.Target as Obj_AI_Hero;
                if (Spells[E].IsReady() && ComboUseE)
                {
                    var pos = FindDashPosition(t);
                    if (pos.IsValid())
                    {
                        Spells[E].Cast(pos);
                        return;
                    }
                }
            }
        }

        public void Harass()
        {
            if (ObjectManager.Player.ManaPercent < HarassManaPercent)
                return;

            if (HarassUseQ && Spells[Q].IsReady())
            {
                var target = TargetSelector.GetTarget(Spells[Q].Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    Spells[Q].SPredictionCast(target, HitChance.High);
                }
            }
        }

        public void Combo()
        {
            if (ComboUseQ && Spells[Q].IsReady())
            {
                var target = TargetSelector.GetTarget(Spells[Q].Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    Spells[Q].SPredictionCast(target, HitChance.High);
                }
            }

            if (ComboUseW && Spells[W].IsReady())
            {
                var target = TargetSelector.GetTarget(Spells[W].Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    Spells[W].SPredictionCast(target, HitChance.High);
                }
            }

            if (ComboUseR && Spells[R].IsReady())
            {
                var target = TargetSelector.GetTarget(Spells[W].Range, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                if (target != null && target.Health < CalculateDamageR(target))
                {                    
                    Spells[R].SPredictionCast(target, HitChance.High);
                }
            }
        }

        public Vector3 FindDashPosition(Obj_AI_Hero target)
        {
            if (ComboEMode == 0)
            {
                Vector3 vec = target.ServerPosition;

                if (target.Path.Length > 0)
                {
                    if (ObjectManager.Player.Distance(vec) < ObjectManager.Player.Distance(target.Path.Last()))
                        return IsSafe(target, Game.CursorPos);
                    else
                        return IsSafe(target, Game.CursorPos.To2D().Rotated(Geometry.DegreeToRadian((vec - ObjectManager.Player.ServerPosition).To2D().AngleBetween((Game.CursorPos - ObjectManager.Player.ServerPosition).To2D()) % 90)).To3D());
                }
                else
                {
                    if (target.IsMelee)
                        return IsSafe(target, Game.CursorPos);
                }

                return IsSafe(target, ObjectManager.Player.ServerPosition + (target.ServerPosition - ObjectManager.Player.ServerPosition).Normalized().To2D().Rotated(Geometry.DegreeToRadian(90 - (vec - ObjectManager.Player.ServerPosition).To2D().AngleBetween((Game.CursorPos - ObjectManager.Player.ServerPosition).To2D()))).To3D() * 300f);
            }
            else if (ComboEMode == 1) //side e idea, credits hoola
            {
                return SCommon.Maths.Geometry.Deviation(ObjectManager.Player.ServerPosition.To2D(), target.ServerPosition.To2D(), 65).To3D();
            }
            else if (ComboEMode == 2)
            {
                return Game.CursorPos;
            }

            return Vector3.Zero;
        }

        public static Vector3 IsSafe(Obj_AI_Hero target, Vector3 vec)
        {
            if (target.ServerPosition.To2D().Distance(vec) <= target.AttackRange && vec.CountEnemiesInRange(1000) > 1)
                return Vector3.Zero;

            if (HeroManager.Enemies.Any(p => p.NetworkId != target.NetworkId && p.ServerPosition.To2D().Distance(vec) <= p.AttackRange) || vec.UnderTurret(true))
                return Vector3.Zero;

            return vec;
        }

        public bool ComboUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Graves.Combo.UseQ").GetValue<bool>(); }
        }

        public bool ComboUseW
        {
            get { return ConfigMenu.Item("SAutoCarry.Graves.Combo.UseW").GetValue<bool>(); }
        }

        public bool ComboUseE
        {
            get { return ConfigMenu.Item("SAutoCarry.Graves.Combo.UseE").GetValue<bool>(); }
        }

        public bool ComboUseR
        {
            get { return ConfigMenu.Item("SAutoCarry.Graves.Combo.UseR").GetValue<bool>(); }
        }

        public int ComboEMode
        {
            get { return ConfigMenu.Item("SAutoCarry.Graves.Combo.EMode").GetValue<StringList>().SelectedIndex; }
        }

        public bool HarassUseQ
        {
            get { return ConfigMenu.Item("SAutoCarry.Graves.Harass.UseQ").GetValue<bool>(); }
        }

        public bool HarassToggle
        {
            get { return ConfigMenu.Item("SAutoCarry.Graves.Harass.Toggle").GetValue<KeyBind>().Active; }
        }

        public int HarassManaPercent
        {
            get { return ConfigMenu.Item("SAutoCarry.Graves.Harass.MinMana").GetValue<Slider>().Value; }
        }
    }
}
