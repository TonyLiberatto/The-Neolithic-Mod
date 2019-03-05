using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BehaviorsLib
{
    public class AiTaskFleePOI : AiTaskBase
    {
        POIRegistry porregistry;
        IPointOfInterest target;
        //Vec3d pos;

        float moveSpeed = 0.02f;
        float fleeingDistance = 31f;
        long stuckatMs = 0;
        float seekingRange = 25f;
        bool nowStuck = false;
        long fleeStartMs;
        float fleeDurationMs = 5000;
        Vec3d pos;
        bool stuck;

        Dictionary<IPointOfInterest, FailedAttempt> failedSeekTargets = new Dictionary<IPointOfInterest, FailedAttempt>();

        public AiTaskFleePOI(EntityAgent entity) : base(entity)
        {
            porregistry = entity.Api.ModLoader.GetModSystem<POIRegistry>();
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            base.LoadConfig(taskConfig, aiConfig);
            if (taskConfig["movespeed"] != null)
            {
                moveSpeed = taskConfig["movespeed"].AsFloat(0.02f);
            }
            if (taskConfig["seekingRange"] != null)
            {
                seekingRange = taskConfig["seekingRange"].AsFloat(25);
            }
            if (taskConfig["fleeingDistance"] != null)
            {
                fleeingDistance = taskConfig["fleeingDistance"].AsFloat(25f);
            }
            else fleeingDistance = seekingRange + 6;
            if (taskConfig["fleeDurationMs"] != null)
            {
                fleeDurationMs = taskConfig["fleeDurationMs"].AsInt(5000);
            }
        }

        public override bool ShouldExecute()
        {
            IPointOfInterest nearestPoi = porregistry.GetNearestPoi(entity.ServerPos.XYZ, 256, (poi) =>
        {
            target = poi as IPointOfInterest;
            if (poi.Type != "scary") return false;
            Vec3d pos = target.Position;
            Cuboidd targetBox = entity.CollisionBox.ToDouble().Translate(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
            double distance = targetBox.ShortestDistanceFrom(pos);
            float minDist = MinDistanceToTarget();
            if (distance < minDist) return false;

            if (target.Position != null)
            {
                failedSeekTargets.TryGetValue(target, out FailedAttempt attempt);
                if (attempt == null || (attempt.Count < 4 || attempt.LastTryMs < world.ElapsedMilliseconds - 60000)) return true;
            }

            return false;
        });


            return nearestPoi != null;
        }



        public float MinDistanceToTarget()
        {
            return System.Math.Max(0.8f, (entity.CollisionBox.X2 - entity.CollisionBox.X1) / 2 + 0.05f);
        }

        public override void StartExecute()
        {
            Vec3d pos = target.Position;
            UpdateTargetPos(pos);
            base.StartExecute();
            entity.World.Logger.Notification("AiTaskFleePOI:" + entity.ServerPos.XYZ + ": I Started :^|");
            stuckatMs = -9999;
            nowStuck = false;
            pathTraverser.GoTo(pos, moveSpeed, MinDistanceToTarget(), OnGoalReached, OnStuck);
            fleeStartMs = entity.World.ElapsedMilliseconds;
            stuck = false;

        }

        public override bool ContinueExecute(float dt)
        {
            Vec3d pos = target.Position;
            UpdateTargetPos(pos);
            //if (pos == null) return false;
            pathTraverser.CurrentTarget.X = pos.X;
            pathTraverser.CurrentTarget.Y = pos.Y;
            pathTraverser.CurrentTarget.Z = pos.Z;
            //entity.World.Logger.Notification("" + pathTraverser.CurrentTarget);

            Cuboidd targetBox = entity.CollisionBox.ToDouble().Translate(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
            double distance = targetBox.ShortestDistanceFrom(pos);
            float minDist = MinDistanceToTarget();
            entity.World.Logger.Notification(""+entity.ServerPos.XYZ.SquareDistanceTo(pos));
            if (entity.ServerPos.XYZ.SquareDistanceTo(pos) > fleeingDistance * fleeingDistance)
            {
                return false;
            }
            if (nowStuck && entity.World.ElapsedMilliseconds > stuckatMs)
            {
                return false;
            }


            return !stuck && (entity.World.ElapsedMilliseconds - fleeStartMs < fleeDurationMs);
        }
        public void UpdateTargetPos(Vec3d pos)
        {
            if (pos == null) { pos = target.Position; }
            Vec3d diff = pos.Sub(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
            float yaw = (float)Math.Atan2(diff.X, diff.Z);

            pos = pos.Ahead(10, 0, yaw - GameMath.PI / 2);
        }


        public override void FinishExecute(bool cancelled)
        {
            base.FinishExecute(cancelled);
            pathTraverser.Stop();

            if (cancelled)
            {
                cooldownUntilTotalHours = 0;
            }
        }



        private void OnStuck()
        {
            stuckatMs = entity.World.ElapsedMilliseconds;
            nowStuck = true;

            FailedAttempt attempt = null;
            failedSeekTargets.TryGetValue(target, out attempt);
            if (attempt == null)
            {
                failedSeekTargets[target] = attempt = new FailedAttempt();
            }

            attempt.Count++;
            attempt.LastTryMs = world.ElapsedMilliseconds;

        }

        private void OnGoalReached()
        {
            pathTraverser.Active = true;

            failedSeekTargets.Remove(target);
        }


    }
}
