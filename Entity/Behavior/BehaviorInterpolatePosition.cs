﻿using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent
{
    public class EntityBehaviorInterpolatePosition : EntityBehavior, IRenderer
    {
        public double RenderOrder => 0;
        public int RenderRange => 9999;
        

        double posDiffX, posDiffY, posDiffZ;
        double yawDiff, rollDiff, pitchDiff;
        

        
        float accum;
        bool serverposApplied;
        
        
        public EntityBehaviorInterpolatePosition(Entity entity) : base(entity)
        {
            if (entity.World.Side == EnumAppSide.Server) throw new Exception("Not made for server side!");

            ICoreClientAPI capi = entity.Api as ICoreClientAPI;
            capi.Event.RegisterRenderer(this, EnumRenderStage.Before, "interpolateposition");
        }


        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            // Don't interpolate for ourselves
            if (entity == ((IClientWorldAccessor)entity.World).Player.Entity) return;
            if ((entity.World.Api as ICoreClientAPI).IsGamePaused) return;

            // When mounted, the entities position is set by the physics sim
            bool isMounted = (entity as EntityAgent)?.MountedOn != null;


            float interval = 0.2f;
            accum += deltaTime;

            if (accum > interval || !(entity is EntityAgent))
            {
                posDiffX = entity.ServerPos.X - entity.Pos.X;
                posDiffY = entity.ServerPos.Y - entity.Pos.Y;
                posDiffZ = entity.ServerPos.Z - entity.Pos.Z;
                rollDiff = entity.ServerPos.Roll - entity.Pos.Roll;
                yawDiff = entity.ServerPos.Yaw - entity.Pos.Yaw;
                pitchDiff = entity.ServerPos.Pitch - entity.Pos.Pitch;

                double posDiffSq = posDiffX * posDiffX + posDiffY * posDiffY + posDiffZ * posDiffZ;

                // "|| accum > 1" mitigates items at the edge of block constantly jumping up and down
                if (entity.ServerPos.BasicallySameAsIgnoreMotion(entity.Pos, 0.05f) || (accum > 1 && posDiffSq < 0.1 * 0.1))
                {
                    if (!serverposApplied && !isMounted)
                    {
                        entity.Pos.SetPos(entity.ServerPos);
                    }

                    serverposApplied = true;

                    return;
                }
            }

            

            double percentPosx = Math.Abs(posDiffX) * deltaTime / interval;
            double percentPosy = Math.Abs(posDiffY) * deltaTime / interval;
            double percentPosz = Math.Abs(posDiffZ) * deltaTime / interval;

            double percentyawdiff = Math.Abs(GameMath.AngleRadDistance(entity.Pos.Yaw, entity.ServerPos.Yaw)) * deltaTime / interval;
            double percentrolldiff = Math.Abs(rollDiff) * deltaTime / interval;
            double percentpitchdiff = Math.Abs(pitchDiff) * deltaTime / interval;


            int signPX = Math.Sign(percentPosx);
            int signPY = Math.Sign(percentPosy);
            int signPZ = Math.Sign(percentPosz);

            if (!isMounted)
            {
                entity.Pos.X += GameMath.Clamp(posDiffX, -signPX * percentPosx, signPX * percentPosx);
                entity.Pos.Y += GameMath.Clamp(posDiffY, -signPY * percentPosy, signPY * percentPosy);
                entity.Pos.Z += GameMath.Clamp(posDiffZ, -signPZ * percentPosz, signPZ * percentPosz);
            }


            int signR = Math.Sign(percentrolldiff); 
            int signY = Math.Sign(percentyawdiff);
            int signP = Math.Sign(percentpitchdiff);

            // Dunno why the 0.7, but it's too fast otherwise
            entity.Pos.Roll += 0.7f * (float)GameMath.Clamp(rollDiff, -signR * percentrolldiff, signR * percentrolldiff);
            entity.Pos.Yaw += 0.7f * (float)GameMath.Clamp(GameMath.AngleRadDistance(entity.Pos.Yaw, entity.ServerPos.Yaw), -signY * percentyawdiff, signY * percentyawdiff);
            entity.Pos.Yaw = entity.Pos.Yaw % GameMath.TWOPI;


            entity.Pos.Pitch += 0.7f * (float)GameMath.Clamp(GameMath.AngleRadDistance(entity.Pos.Pitch, entity.ServerPos.Pitch), -signP * percentpitchdiff, signP * percentpitchdiff);
            entity.Pos.Pitch = entity.Pos.Pitch % GameMath.TWOPI;
        }


        
        public override void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
        {
            // Don't interpolate for ourselves
            if (entity == ((IClientWorldAccessor)entity.World).Player.Entity) return;
            
            handled = EnumHandling.PreventDefault;


            posDiffX = entity.ServerPos.X - entity.Pos.X;
            posDiffY = entity.ServerPos.Y - entity.Pos.Y;
            posDiffZ = entity.ServerPos.Z - entity.Pos.Z;
            rollDiff = GameMath.AngleRadDistance(entity.Pos.Roll, entity.ServerPos.Roll);
            yawDiff = GameMath.AngleRadDistance(entity.Pos.Yaw, entity.ServerPos.Yaw);
            pitchDiff = GameMath.AngleRadDistance(entity.Pos.Pitch, entity.ServerPos.Pitch);

            serverposApplied = false;

            accum = 0;
        }


        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            base.OnEntityDespawn(despawn);
            (entity.Api as ICoreClientAPI).Event.UnregisterRenderer(this, EnumRenderStage.Before);
            Dispose();
        }

        public override string PropertyName()
        {
            return "lerppos";
        }

        public void Dispose()
        {
            
        }
    }
}
