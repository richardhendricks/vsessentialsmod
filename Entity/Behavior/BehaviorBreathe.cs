﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent
{
    public class EntityBehaviorBreathe : EntityBehavior
    {
        Cuboidd tmp = new Cuboidd();
        float breathAccum = 0;
        //the padding that the collisionbox is adjusted by for suffocation damage.  Can be adjusted as necessary - don't set to exactly 0.
        float padding = 0.1f; 

        
        public EntityBehaviorBreathe(Entity entity) : base(entity)
        {
        }

        public void Check()
        {
            if (entity is EntityPlayer)
            {
                EntityPlayer plr = (EntityPlayer)entity;
                EnumGameMode mode = entity.World.PlayerByUid(plr.PlayerUID).WorldData.CurrentGameMode;
                if (mode == EnumGameMode.Creative || mode == EnumGameMode.Spectator) return;
            }
            
            BlockPos pos = new BlockPos(
                (int)(entity.ServerPos.X + entity.LocalEyePos.X),
                (int)(entity.ServerPos.Y + entity.LocalEyePos.Y),
                (int)(entity.ServerPos.Z + entity.LocalEyePos.Z)
            );

            Block block = entity.World.BlockAccessor.GetBlock(pos);
            Cuboidf[] collisionboxes = block.GetCollisionBoxes(entity.World.BlockAccessor, pos);

            Cuboidf box = new Cuboidf();

            if (collisionboxes == null) return;

            for (int i = 0; i < collisionboxes.Length; i++)
            {
                box.Set(collisionboxes[i]);
                box.OmniGrowBy(-padding);
                tmp.Set(pos.X + box.X1, pos.Y + box.Y1, pos.Z + box.Z1, pos.X + box.X2, pos.Y + box.Y2, pos.Z + box.Z2);
                box.OmniGrowBy(padding);

                if (tmp.Contains(entity.ServerPos.X + entity.LocalEyePos.X, entity.ServerPos.Y + entity.LocalEyePos.Y, entity.ServerPos.Z + entity.LocalEyePos.Z))
                {
                    Cuboidd EntitySuffocationBox = entity.SelectionBox.ToDouble();

                    if (tmp.Intersects(EntitySuffocationBox))
                    {
                        DamageSource dmgsrc = new DamageSource() { Source = EnumDamageSource.Block, SourceBlock = block, Type = EnumDamageType.Suffocation };
                        entity.ReceiveDamage(dmgsrc, 1f);
                        break;
                    }


                }

            }

        }


        public override void OnGameTick(float deltaTime)
        {
            if (entity.State == EnumEntityState.Inactive)
            {
                return;
            }


            base.OnGameTick(deltaTime);

            breathAccum += deltaTime;

            if (breathAccum > 1)
            {
                breathAccum = 0;
                Check();
            }
        }

        public override string PropertyName()
        {
            return "breathe";
        }
    }
}
