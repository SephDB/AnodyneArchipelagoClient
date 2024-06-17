using AnodyneSharp.Cheatz;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Enemy;
using AnodyneSharp.Entities.Enemy.Apartment;
using AnodyneSharp.Entities.Enemy.Bedroom;
using AnodyneSharp.Entities.Enemy.Cell;
using AnodyneSharp.Entities.Enemy.Circus;
using AnodyneSharp.Entities.Enemy.Crowd;
using AnodyneSharp.Entities.Enemy.Etc;
using AnodyneSharp.Entities.Enemy.Go;
using AnodyneSharp.Entities.Enemy.Hotel;
using AnodyneSharp.Entities.Enemy.Hotel.Boss;
using AnodyneSharp.Entities.Enemy.Redcave;
using AnodyneSharp.Entities.Enemy.Suburb;
using AnodyneSharp.Entities.Interactive.Npc.RedSea;
using AnodyneSharp.MapData;
using AnodyneSharp.Registry;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnodyneArchipelago
{
    public static class DeathHelper
    {
        public static string GetDeathReason()
        {
            return GlobalState.DamageDealer switch
            {
                CheatzManager.DamageDealer => "",
                DashTrap.DamageDealer => "was impaled by a dash trap.",
                GasGuy.DamageDealer => "suffocated in the fumes.",
                Rat.DamageDealer => "got spooked by a cute little rat squeaking too loudly.",
                Silverfish.DamageDealer => "was swarmed by silverfish.",
                BaseSplitBoss.DamageDealer => "was immolated by the Watcher.",
                BaseSplitBoss.BulletDamageDealer => "was shot by the Watcher.",
                TeleGuy.DamageDealer => "got sneaked up on by a Tele Guy.",
                Annoyer.DamageDealer => "was swooped by an annoyer.",
                Annoyer.FireballDamageDealer => "was scorched by an annoyer.",
                PewLaser.DamageDealer => "got pew pew'd.",
                Seer.DamageDealer => "was menaced by the Seeing One.",
                Seer.WaveDamageDealer => "was flattened by the Seeing One.",
                Seer.OrbDamageDealer => "was pummelled by the Seeing One.",
                Shieldy.DamageDealer => "was pushed away by a Shieldy.",
                Slime.DamageDealer => "was absorbed by slime.",
                Slime.BulletDamageDealer => "was hit by a glob of red slime.",
                Chaser.DamageDealer => "was hugged to death by a Chaser.",
                CircusFolks.DamageDealer => "",
                CircusFolks.FlameDamageDealer => "was blasted by Arthur and Javiera.",
                CircusFolks.JavieraDamageDealer => "was vaulted by Javiera.",
                CircusFolks.ArthurDamageDealer => "was suplexed by Arthur.",
                Contort.DamageDealer => "got majorly clowned on.",
                Contort.SmallDamageDealer => "was juggled like a football.",
                FirePillar.DamageDealer => "",
                Lion.DamageDealer => "was eaten by a lion.",
                Lion.FireDamageDealer => "was toasted by a lion.",
                Dog.DamageDealer => "was bitten by a dog.",
                Frog.DamageDealer => "was sat on by a frog.",
                Frog.BulletDamageDealer => "was burped on by a frog.",
                Rotator.DamageDealer => "was pierced by a rotator bullet.",
                BaseSpikeRoller.DamageDealer => "was crushed by a spike roller.",
                WallBoss.BulletDamageDealer => "was shot by the Wall.",
                WallBoss.HandDamageDealer => "was punched by the Wall.",
                WallBoss.LaserDamageDealer => "was flattened by the Wall.",
                SageBoss.DamageDealer => "was judged by the Sage.",
                SageBoss.BulletDamageDealer => "was shot by the Sage.",
                SageBoss.LaserDamageDealer => "",
                SageBoss.EdgeDamageDealer => "",
                SageBoss.OrbDamageDealer => "",
                BriarBossMain.IceDamageDealer => "was bowled over by the Briar.",
                BriarBossMain.FireballDamageDealer => "was torched by the Briar.",
                BriarBossMain.BodyDamageDealer => "was repelled by the Briar.",
                BriarBossMain.GateDamageDealer => "stumbled into the Briar's thorns.",
                BriarBossMain.BulletDamageDealer => "was shot by the Briar.",
                BriarBossMain.ThornDamageDealer => "was pierced by the Briar.",
                Burst_Plant.DamageDealer => "had an allergic reaction to pollen.",
                Dustmaid.DamageDealer => "was sanitized by a dust maid.",
                WaterPhase.DamageDealer => "drowned in the hotel pool.",
                WaterPhase.BulletDamageDealer => "was shot by the Manager in the pool.",
                LandPhase.DamageDealer => "was trampled by the Manager.",
                LandPhase.BulletDamageDealer => "was shot by the Manager on land.",
                Four_Shooter.DamageDealer => "was shot by a four-shooter.",
                OnOffLaser.DamageDealer => "was evaporated by a laser.",
                Red_Boss.DamageDealer => "was traumatized by the Rogue.",
                Red_Boss.BulletDamageDealer => "got splashed by the Rogue.",
                Red_Boss.TentacleDamageDealer => "was squeezed to death by the Rogue.",
                Slasher.LongDamageDealer => "",
                Slasher.WideDamageDealer => "was cleaved in twain by a Slasher.",
                SuburbKiller.DamageDealer => "got killed. Yeah.",
                BombDude.DamageDealer => "was blown up by a rock creature and it's their own fault (sorry lmao).",
                Player.DrowningDamageDealer => "drowned.",
                Map.SpikeDamageDealer => "fell into some spikes.",
                _ => $"damaged by {GlobalState.DamageDealer}"
            };
        }
    }
}
