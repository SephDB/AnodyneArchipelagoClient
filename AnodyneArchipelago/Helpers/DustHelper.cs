using AnodyneSharp.Entities;
using AnodyneSharp.States;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AnodyneArchipelago.Helpers
{
    public class DustHelper
    {
        //Plugin.Game.CurrentState(as PlayState)._groups.Get(typeof(Dust)).colliders
        static FieldInfo colissionField = typeof(PlayState).GetField("_groups", BindingFlags.NonPublic | BindingFlags.Instance)!;
        static MethodInfo colissiongroupsField = typeof(CollisionGroups).GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Instance)!;
        static FieldInfo collidersField = colissiongroupsField.ReturnType.GetField("colliders", BindingFlags.Instance | BindingFlags.Public)!;

        bool Rafting = true;
        bool Pickup = true;

        CollisionGroups? current = null;

        public void Update()
        {
            if (Plugin.Game.CurrentState is PlayState playState)
            {
                var next = colissionField.GetValue(playState) as CollisionGroups;
                if (next != null && next != current)
                {
                    current = next;
                    SetRafting(Rafting);
                    SetPickup(Pickup);
                }
            }
        }

        public void SetRafting(bool b)
        {
            Rafting = b;
            if(current == null)
            {
                return;
            }
            if(Rafting)
            {
                Enable(Plugin.Player);
            }
            else
            {
                Disable(Plugin.Player);
            }
        }

        public void SetPickup(bool b)
        {
            Pickup = b;
            if (current == null)
            {
                return;
            }
            if (Pickup)
            {
                Enable(Plugin.Player.broom);
            }
            else
            {
                Disable(Plugin.Player.broom);
            }
        }

        private List<Entity>? GetDustGroup()
        {
            if(current == null)
            {
                return null;
            }
            return collidersField.GetValue(colissiongroupsField.Invoke(current,[typeof(Dust)])) as List<Entity>;
        }

        private void Enable(Entity e)
        {
            var dust = GetDustGroup();
            if (dust != null && !dust.Contains(e))
            {
                dust.Add(e);
            }
        }

        private void Disable(Entity e)
        {
            GetDustGroup()?.Remove(e);
        }

    }
}
