﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SCommon.PluginBase;

namespace SAutoCarry.Champions.Helpers
{
    public static class CardMgr
    {
        public enum Card
        {
            Normal,
            Blue,
            Red,
            Gold,
        }

        private static Champion s_Champion;
        private static Card s_CurrentCard;
        private static Card s_CardToSelect;
        private static int s_lastWSent;
        private static bool s_isSelecting;

        public static void Initialize(Champion champ)
        {
            s_Champion = champ;
            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        /// <summary>
        /// Gets current selected card
        /// </summary>
        public static Card CurrentCard
        {
            get { return s_CurrentCard; }
        }

        public static bool CanProcessAttack
        {
            get { return !s_isSelecting && Utils.TickCount - s_lastWSent > 300; }
        }

        public static void Select(Card card)
        {
            if (!s_isSelecting && s_Champion.Spells[1].IsReady() && s_Champion.Spells[1].Instance.Name == "PickACard")
            {
                s_CardToSelect = card;
                if (Utils.TickCount - s_lastWSent > 170 + Game.Ping / 2)
                {
                    s_Champion.Spells[1].Cast();
                    s_lastWSent = Utils.TickCount;
                }
            }
        }

        public static Card GetCardByName(string name)
        {
            switch(name)
            {
                case "goldcardlock":
                    return Card.Gold;
                case "bluecardlock":
                    return Card.Blue;
                case "redcardlock":
                    return Card.Red;
                default:
                    return Card.Normal;
            }
        }

        public static string GetCardName(Card card)
        {
            switch(card)
            {
                case Card.Gold:
                    return "goldcardlock";
                case Card.Blue:
                    return "bluecardlock";
                case Card.Red:
                    return "redcardlock";
                default:
                    return "PickACard";
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            string wName = s_Champion.Spells[1].Instance.Name;
            SpellState wState = ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.W);
            if (wName == "PickACard")
            {
                if (wState == SpellState.Ready && (!s_isSelecting || Utils.TickCount - s_lastWSent > 500))
                    s_isSelecting = false;
                else if (wState == SpellState.Cooldown)
                {
                    s_CardToSelect = Card.Normal;
                    s_isSelecting = false;
                }
            }
            else if (wName == GetCardName(s_CardToSelect))
            {
                s_Champion.Spells[1].Cast();
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if(sender.IsMe)
            {
                if (args.SData.Name == "PickACard")
                    s_isSelecting = true;
                else if(args.SData.Name == "goldcardlock" || args.SData.Name == "bluecardlock" || args.SData.Name == "redcardlock")
                {
                    s_isSelecting = false;
                    s_CardToSelect = Card.Normal;
                    s_CurrentCard = GetCardByName(args.SData.Name);
                }
            }
        }
    }
}
