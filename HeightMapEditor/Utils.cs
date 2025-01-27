// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//Changes by Idomeneas

using System;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media.Media3D;
using TerrainEditor;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;

namespace HeightMapEditor
{
    public enum ClickType
    {
        /// <summary>
        /// The result didn't hit anything
        /// </summary>
        Empty,

        /// <summary>
        /// The result hit a ground object
        /// </summary>
        Ground,

        /// <summary>
        /// The result hit a treasure chest object
        /// </summary>
        LootCrate,
    }

    /// <summary>
    /// Result of the user clicking/tapping on the world
    /// </summary>
    public struct ClickResult
    {
        /// <summary>
        /// The world-space position of the click, where the raycast hits the collision body
        /// </summary>
        public Vector3 WorldPosition;

        /// <summary>
        /// The Entity containing the collision body which was hit
        /// </summary>
        public Entity ClickedEntity;

        /// <summary>
        /// What kind of object did we hit
        /// </summary>
        public ClickType Type;

        /// <summary>
        /// The HitResult received from the physics simulation
        /// </summary>
        public HitResult HitResult;

        /// <summary>
        /// index into the mess, (i,j) point
        /// </summary>
        public Int2 index;
    }
    public static class Utils
    {
         public static BoundingBox FromPoints(VertexTypePosTexNormColor[] verts)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            for (int i = 0; i < verts.Length; ++i)
            {
                var v = verts[i];
                Vector3.Min(ref min, ref v.Position, out min);
                Vector3.Max(ref max, ref v.Position, out max);
            }

            return new BoundingBox(min, max);
        }
        
        public static BoundingBox FromPoints(VertexPositionNormalTexture[] verts)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            for (int i = 0; i < verts.Length; ++i)
            {
                var v = verts[i];
                Vector3.Min(ref min, ref v.Position, out min);
                Vector3.Max(ref max, ref v.Position, out max);
            }

            return new BoundingBox(min, max);
        }
        public static BoundingBox FromPoints(VertexPositionNormalColor[] verts)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            for (int i = 0; i < verts.Length; ++i)
            {
                var v = verts[i];
                Vector3.Min(ref min, ref v.Position, out min);
                Vector3.Max(ref max, ref v.Position, out max);
            }

            return new BoundingBox(min, max);
        }

        public static void SpawnPrefabModel(this ScriptComponent script, Prefab source, Entity attachEntity, Matrix localMatrix, Vector3 forceImpulse)
        {
            if (source == null)
                return;

            // Clone
            var spawnedEntities = source.Instantiate();

            // Add
            foreach (var prefabEntity in spawnedEntities)
            {
                prefabEntity.Transform.UpdateLocalMatrix();
                var entityMatrix = prefabEntity.Transform.LocalMatrix * localMatrix;
                entityMatrix.Decompose(out prefabEntity.Transform.Scale, out prefabEntity.Transform.Rotation, out prefabEntity.Transform.Position);

                if (attachEntity != null)
                {
                    attachEntity.AddChild(prefabEntity);
                }
                else
                {
                    script.SceneSystem.SceneInstance.RootScene.Entities.Add(prefabEntity);
                }

                var physComp = prefabEntity.Get<RigidbodyComponent>();
                if (physComp != null)
                {
                    physComp.ApplyImpulse(forceImpulse);
                }
            }
        }

        public static void SpawnPrefabInstance(this ScriptComponent script, Prefab source, Entity attachEntity, float timeout, Matrix localMatrix)
        {
            if (source == null)
                return;

            Func<Task> spawnTask = async () =>
            {
                // Clone
                var spawnedEntities = source.Instantiate();

                // Add
                foreach (var prefabEntity in spawnedEntities)
                {
                    prefabEntity.Transform.UpdateLocalMatrix();
                    var entityMatrix = prefabEntity.Transform.LocalMatrix * localMatrix;
                    entityMatrix.Decompose(out prefabEntity.Transform.Scale, out prefabEntity.Transform.Rotation, out prefabEntity.Transform.Position);

                    if (attachEntity != null)
                    {
                        attachEntity.AddChild(prefabEntity);
                    }
                    else
                    {
                        script.SceneSystem.SceneInstance.RootScene.Entities.Add(prefabEntity);
                    }
                }

                // Countdown
                var secondsCountdown = timeout;
                while (secondsCountdown > 0f)
                {
                    await script.Script.NextFrame();
                    secondsCountdown -= (float)script.Game.UpdateTime.Elapsed.TotalSeconds;
                }

                // Remove
                foreach (var clonedEntity in spawnedEntities)
                {
                    if (attachEntity != null)
                    {
                        attachEntity.RemoveChild(clonedEntity);
                    }
                    else
                    {
                        script.SceneSystem.SceneInstance.RootScene.Entities.Remove(clonedEntity);
                    }
                }

                // Cleanup
                spawnedEntities.Clear();
            };

            script.Script.AddTask(spawnTask);
        }

        /// <summary>
        /// Removes an entity, together with its children, from the Game's scene graph
        /// </summary>
        /// <param name="game">The game instance containing the entity</param>
        /// <param name="entity">Entity to remove</param>
        public static void RemoveEntity(this IGame game, Entity entity)
        {
            var parent = entity.GetParent();
            if (parent != null)
            {
                parent.RemoveChild(entity);
                return;
            }

            ((Game)game).SceneSystem.SceneInstance.RootScene.Entities.Remove(entity);
        }

        public static async Task WaitTime(this IGame game, TimeSpan time)
        {
            var g = (Game)game;
            var goal = game.UpdateTime.Total + time;
            while (game.UpdateTime.Total < goal)
            {
                await g.Script.NextFrame();
            }
        }

        public static Vector3 LogicDirectionToWorldDirection(Vector2 logicDirection, CameraComponent camera, Vector3 upVector)
        {
            camera.Update();
            var inverseView = Matrix.Invert(camera.ViewMatrix);

            var forward = Vector3.Cross(upVector, inverseView.Right);
            forward.Normalize();

            var right = Vector3.Cross(forward, upVector);
            var worldDirection = forward * logicDirection.Y + right * logicDirection.X;
            worldDirection.Normalize();
            return worldDirection;
        }

        public static bool ScreenPositionToWorldPositionRaycast(Vector2 screenPos, CameraComponent camera, Simulation simulation, out ClickResult clickResult)
        {
            Matrix invViewProj = Matrix.Invert(camera.ViewProjectionMatrix);

            Vector3 sPos;
            sPos.X = screenPos.X * 2f - 1f;
            sPos.Y = 1f - screenPos.Y * 2f;

            sPos.Z = 0f;
            var vectorNear = Vector3.Transform(sPos, invViewProj);
            vectorNear /= vectorNear.W;

            sPos.Z = 1f;
            var vectorFar = Vector3.Transform(sPos, invViewProj);
            vectorFar /= vectorFar.W;

            clickResult.ClickedEntity = null;
            clickResult.WorldPosition = Vector3.Zero;
            clickResult.index = Int2.Zero;
            clickResult.Type = ClickType.Empty;
            clickResult.HitResult = new HitResult();

            var minDistance = float.PositiveInfinity;

            var result = new FastList<HitResult>();
            simulation.RaycastPenetrating(vectorNear.XYZ(), vectorFar.XYZ(), result);
            foreach (var hitResult in result)
            {
                ClickType type = ClickType.Empty;

                var staticBody = hitResult.Collider as StaticColliderComponent;
                if (staticBody != null)
                {
                    if (staticBody.CollisionGroup == CollisionFilterGroups.CustomFilter1)
                        type = ClickType.Ground;

                    if (staticBody.CollisionGroup == CollisionFilterGroups.CustomFilter2)
                        type = ClickType.LootCrate;

                    if (type != ClickType.Empty)
                    {
                        var distance = (vectorNear.XYZ() - hitResult.Point).LengthSquared();
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            clickResult.Type = type;
                            clickResult.HitResult = hitResult;
                            clickResult.WorldPosition = hitResult.Point;
                            clickResult.ClickedEntity = hitResult.Collider.Entity;
                        }
                    }
                }
            }

            return (clickResult.Type != ClickType.Empty);
        }

        public static bool IntersectTriangle(Vector3 orig, Vector3 dir, Vector3 v0, Vector3 v1,
            Vector3 v2, out float t, out float u, out float v)
        {
            t = 0;
            u = 0;
            v = 0;
            // Find vectors for two edges sharing vert0
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;

            // Begin calculating determinant - also used to calculate U parameter
            Vector3 pvec;
            Vector3.Cross(ref dir, ref edge2, out pvec);

            // If determinant is near zero, ray lies in plane of triangle
            Vector3 tvec;
            float det;
            Vector3.Dot(ref edge1, ref pvec, out det); 
            if (det > 0)
            {
                tvec = orig - v0;
            }
            else
            {
                tvec = v0 - orig;
                det = -det;
            }
            if (det < 0.0001f) return false;
            // Calculate U parameter and test bounds
            Vector3.Dot(ref tvec, ref pvec, out u);
            if (u < 0.0f || u > det) return false;
            // Prepare to test V parameter
            Vector3 qvec;
            Vector3.Cross( ref tvec, ref edge1, out qvec);
            // Calculate V parameter and test bounds
            Vector3.Dot(ref dir, ref qvec, out v);
            if (v < 0.0f || u + v > det) return false;
            // Calculate t, scale parameters, ray intersects triangle
            Vector3.Dot(ref edge2, ref qvec, out t); 
            float fInvDet = 1.0f / det;
            t *= fInvDet;
            u *= fInvDet;
            v *= fInvDet;
            return true;
        }
    }
}
