using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CarryCapacity.Client
{
    public class EntityCarryRenderer : IRenderer
    {
        private static readonly Dictionary<CarrySlot, SlotRenderSettings> _renderSettings
            = new Dictionary<CarrySlot, SlotRenderSettings> {
                { CarrySlot.Hands    , new SlotRenderSettings("carrycapacity:FrontCarry", 0.05F, -0.5F, -0.5F) },
                { CarrySlot.Back     , new SlotRenderSettings("Back", 0.0F, -0.6F, -0.5F) },
                { CarrySlot.Shoulder , new SlotRenderSettings("carrycapacity:ShoulderL", -0.5F, 0.0F, -0.5F) },
            };

        private class SlotRenderSettings
        {
            public string AttachmentPoint { get; }
            public Vec3f Offset { get; }
            public SlotRenderSettings(string attachmentPoint, float xOffset, float yOffset, float zOffset)
            { AttachmentPoint = attachmentPoint; Offset = new Vec3f(xOffset, yOffset, zOffset); }
        }


        private ICoreClientAPI API { get; }

        public EntityCarryRenderer(ICoreClientAPI api)
        {
            API = api;
            API.Event.RegisterRenderer(this, EnumRenderStage.Opaque);
            API.Event.RegisterRenderer(this, EnumRenderStage.ShadowFar);
            API.Event.RegisterRenderer(this, EnumRenderStage.ShadowNear);
        }

        public void Dispose()
        {
            API.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            API.Event.UnregisterRenderer(this, EnumRenderStage.ShadowFar);
            API.Event.UnregisterRenderer(this, EnumRenderStage.ShadowNear);
        }


        private ItemRenderInfo GetRenderInfo(CarriedBlock carried)
        {
            // Alternative: Cache API.TesselatorManager.GetDefaultBlockMesh manually.
            var renderInfo = API.Render.GetItemStackRenderInfo(carried.ItemStack, EnumItemRenderTarget.Ground);
            var behavior = carried.Behavior;
            renderInfo.Transform = behavior.Slots[carried.Slot]?.Transform ?? behavior.DefaultTransform;
            return renderInfo;
        }


        // IRenderer implementation

        public double RenderOrder => 1.0;
        public int RenderRange => 99;

        private float moveWobble;

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            for (int i = 0; i < API.World.AllPlayers.Length; i++)
            {
                // Leaving the additional, more detailed exceptions in just in case other things end up breaking.
                IPlayer player = API.World.AllPlayers[i];
                if (player == null) throw new Exception("null player in API.World.AllPlayers!");

                // Player entity may be null in some circumstances.
                // Maybe the other player is too far away, so there's
                // no entity spawned for them on the client's side?
                if (player.Entity == null) continue;

                EntityPlayer entity = player.Entity;
                List<CarriedBlock> allCarried = entity.GetCarried().ToList();

                if (allCarried.Count == 0) continue; // Entity is not carrying anything.

                IRenderAPI renderApi = API.Render;
                bool isShadowPass = (stage != EnumRenderStage.Opaque);
                bool isFirstPerson = (player == API.World.Player) && (API.World.Player.CameraMode == EnumCameraMode.FirstPerson);

                EntityShapeRenderer renderer = (EntityShapeRenderer)entity.Properties.Client.Renderer;

                IAnimator animator = entity.AnimManager.Animator;
                if (animator == null) throw new Exception("entity.AnimManager.Animator is null!");

                for (int j = 0; j < allCarried.Count; j++)
                {
                    CarriedBlock carried = allCarried[j];

                    bool inHands = (carried.Slot == CarrySlot.Hands);
                    if (!inHands && isFirstPerson && !isShadowPass) continue;

                    SlotRenderSettings renderSettings = _renderSettings[carried.Slot];
                    ItemRenderInfo renderInfo = GetRenderInfo(carried);

                    float[] viewMat = Array.ConvertAll(API.Render.CameraMatrixOrigin, o => (float)o);
                    float[] modelMat;

                    if (inHands && isFirstPerson && !isShadowPass)
                    {
                        modelMat = Mat4f.Invert(Mat4f.Create(), viewMat);

                        if (entity.Controls.TriesToMove)
                        {
                            float moveSpeed = entity.Controls.MovespeedMultiplier * (float)entity.GetWalkSpeedMultiplier();
                            moveWobble += moveSpeed * deltaTime * 5.0F;
                        }
                        else
                        {
                            float target = (float)(Math.Round(moveWobble / Math.PI) * Math.PI);
                            float speed = deltaTime * (0.2F + Math.Abs(target - moveWobble) * 4);
                            if (Math.Abs(target - moveWobble) < speed) moveWobble = target;
                            else moveWobble += Math.Sign(target - moveWobble) * speed;
                        }
                        moveWobble = moveWobble % (GameMath.PI * 2);

                        float moveWobbleOffsetX = GameMath.Sin((moveWobble + GameMath.PI)) * 0.03F;
                        float moveWobbleOffsetY = GameMath.Sin(moveWobble * 2) * 0.02F;

                        Mat4f.Translate(modelMat, modelMat, moveWobbleOffsetX, moveWobbleOffsetY - 0.25F, -0.25F);
                        Mat4f.RotateY(modelMat, modelMat, 90.0F * GameMath.DEG2RAD);
                    }
                    else
                    {
                        modelMat = Mat4f.CloneIt(renderer.ModelMat);

                        AttachmentPointAndPose attachPointAndPose = animator.GetAttachmentPointPose(renderSettings.AttachmentPoint);
                        if (attachPointAndPose == null) continue;
                        float[] animModelMat = attachPointAndPose.CachedPose.AnimModelMatrix;
                        Mat4f.Mul(modelMat, modelMat, animModelMat);

                        // Apply attachment point transform.
                        AttachmentPoint attach = attachPointAndPose.AttachPoint;
                        Mat4f.Translate(modelMat, modelMat, (float)(attach.PosX / 16), (float)(attach.PosY / 16), (float)(attach.PosZ / 16));
                        Mat4f.RotateX(modelMat, modelMat, (float)attach.RotationX * GameMath.DEG2RAD);
                        Mat4f.RotateY(modelMat, modelMat, (float)attach.RotationY * GameMath.DEG2RAD);
                        Mat4f.RotateZ(modelMat, modelMat, (float)attach.RotationZ * GameMath.DEG2RAD);
                    }

                    // Apply carried block's behavior transform.
                    ModelTransform t = renderInfo.Transform;
                    Mat4f.Scale(modelMat, modelMat, t.ScaleXYZ.X, t.ScaleXYZ.Y, t.ScaleXYZ.Z);
                    Mat4f.Translate(modelMat, modelMat, renderSettings.Offset.X, renderSettings.Offset.Y, renderSettings.Offset.Z);
                    Mat4f.Translate(modelMat, modelMat, t.Origin.X, t.Origin.Y, t.Origin.Z);
                    Mat4f.RotateX(modelMat, modelMat, t.Rotation.X * GameMath.DEG2RAD);
                    Mat4f.RotateY(modelMat, modelMat, t.Rotation.Y * GameMath.DEG2RAD);
                    Mat4f.RotateZ(modelMat, modelMat, t.Rotation.Z * GameMath.DEG2RAD);
                    Mat4f.Translate(modelMat, modelMat, -t.Origin.X, -t.Origin.Y, -t.Origin.Z);
                    Mat4f.Translate(modelMat, modelMat, t.Translation.X, t.Translation.Y, t.Translation.Z);

                    if (isShadowPass)
                    {
                        IShaderProgram prog = renderApi.CurrentActiveShader;
                        Mat4f.Mul(modelMat, API.Render.CurrentShadowProjectionMatrix, modelMat);
                        prog.BindTexture2D("tex2d", renderInfo.TextureId, 0);
                        prog.UniformMatrix("mvpMatrix", modelMat);
                        prog.Uniform("origin", renderer.OriginPos);

                        API.Render.RenderMesh(renderInfo.ModelRef);
                    }
                    else
                    {
                        IStandardShaderProgram prog = renderApi.PreparedStandardShader((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z);
                        prog.Tex2D = renderInfo.TextureId;
                        prog.AlphaTest = 0.01f;
                        prog.ViewMatrix = viewMat;
                        prog.ModelMatrix = modelMat;
                        prog.DontWarpVertices = 1;

                        API.Render.RenderMesh(renderInfo.ModelRef);

                        prog.Stop();
                    }
                }
            }
        }
    }
}
