using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace UnityEngine
{
    /// <summary>
    ///     Generic visual tile for creating different tilesets like terrain, pipeline, random or animated tiles.
    ///     This is templated to accept a Neighbor Rule Class for Custom Rules.
    /// </summary>
    /// <typeparam name="T">Neighbor Rule Class for Custom Rules</typeparam>
    public class RuleTile<T> : RuleTile
    {
        /// <summary>
        ///     Returns the Neighbor Rule Class type for this Rule Tile.
        /// </summary>
        public sealed override Type m_NeighborType => typeof(T);
    }

    /// <summary>
    ///     Generic visual tile for creating different tilesets like terrain, pipeline, random or animated tiles.
    /// </summary>
    [Serializable]
    [HelpURL(
        "https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@latest/index.html?subfolder=/manual/RuleTile.html")]
    public class RuleTile : TileBase
    {
        private static Dictionary<Tilemap, KeyValuePair<HashSet<TileBase>, HashSet<Vector3Int>>>
            m_CacheTilemapsNeighborPositions = new();

        private static TileBase[] m_AllocatedUsedTileArr = Array.Empty<TileBase>();

        /// <summary>
        ///     The Default Sprite set when creating a new Rule.
        /// </summary>
        public Sprite m_DefaultSprite;

        /// <summary>
        ///     The Default GameObject set when creating a new Rule.
        /// </summary>
        public GameObject m_DefaultGameObject;

        /// <summary>
        ///     The Default Collider Type set when creating a new Rule.
        /// </summary>
        public Tile.ColliderType m_DefaultColliderType = Tile.ColliderType.Sprite;

        /// <summary>
        ///     A list of Tiling Rules for the Rule Tile.
        /// </summary>
        [HideInInspector] public List<TilingRule> m_TilingRules = new();

        private HashSet<Vector3Int> m_NeighborPositions = new();

        /// <summary>
        ///     Returns the default Neighbor Rule Class type.
        /// </summary>
        public virtual Type m_NeighborType => typeof(TilingRuleOutput.Neighbor);

        /// <summary>
        ///     Angle in which the RuleTile is rotated by for matching in Degrees.
        /// </summary>
        public virtual int m_RotationAngle => 90;

        /// <summary>
        ///     Number of rotations the RuleTile can be rotated by for matching.
        /// </summary>
        public int m_RotationCount => 360 / m_RotationAngle;

        /// <summary>
        ///     Returns a set of neighboring positions for this RuleTile
        /// </summary>
        public HashSet<Vector3Int> neighborPositions
        {
            get
            {
                if (m_NeighborPositions.Count == 0)
                    UpdateNeighborPositions();

                return m_NeighborPositions;
            }
        }

        /// <summary>
        ///     Updates the neighboring positions of this RuleTile
        /// </summary>
        public void UpdateNeighborPositions()
        {
            m_CacheTilemapsNeighborPositions.Clear();

            var positions = m_NeighborPositions;
            positions.Clear();

            foreach (var rule in m_TilingRules)
            foreach (var neighbor in rule.GetNeighbors())
            {
                var position = neighbor.Key;
                positions.Add(position);

                // Check rule against rotations of 0, 90, 180, 270
                if (rule.m_RuleTransform == TilingRuleOutput.Transform.Rotated)
                {
                    for (var angle = m_RotationAngle; angle < 360; angle += m_RotationAngle)
                        positions.Add(GetRotatedPosition(position, angle));
                }
                // Check rule against x-axis, y-axis mirror
                else if (rule.m_RuleTransform == TilingRuleOutput.Transform.MirrorXY)
                {
                    positions.Add(GetMirroredPosition(position, true, true));
                    positions.Add(GetMirroredPosition(position, true, false));
                    positions.Add(GetMirroredPosition(position, false, true));
                }
                // Check rule against x-axis mirror
                else if (rule.m_RuleTransform == TilingRuleOutput.Transform.MirrorX)
                {
                    positions.Add(GetMirroredPosition(position, true, false));
                }
                // Check rule against y-axis mirror
                else if (rule.m_RuleTransform == TilingRuleOutput.Transform.MirrorY)
                {
                    positions.Add(GetMirroredPosition(position, false, true));
                }
                else if (rule.m_RuleTransform == TilingRuleOutput.Transform.RotatedMirror)
                {
                    var mirroredPosition = GetMirroredPosition(position, true, false);
                    for (var angle = m_RotationAngle; angle < 360; angle += m_RotationAngle)
                    {
                        positions.Add(GetRotatedPosition(position, angle));
                        positions.Add(GetRotatedPosition(mirroredPosition, angle));
                    }
                }
            }
        }

        /// <summary>
        ///     StartUp is called on the first frame of the running Scene.
        /// </summary>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        /// <param name="instantiatedGameObject">The GameObject instantiated for the Tile.</param>
        /// <returns>Whether StartUp was successful</returns>
        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject instantiatedGameObject)
        {
            if (instantiatedGameObject != null)
            {
                var tmpMap = tilemap.GetComponent<Tilemap>();
                var orientMatrix = tmpMap.orientationMatrix;

                var iden = Matrix4x4.identity;
                var gameObjectTranslation = new Vector3();
                var gameObjectRotation = new Quaternion();
                var gameObjectScale = new Vector3();

                var ruleMatched = false;
                var transform = iden;
                foreach (var rule in m_TilingRules)
                    if (RuleMatches(rule, position, tilemap, ref transform))
                    {
                        transform = orientMatrix * transform;

                        // Converts the tile's translation, rotation, & scale matrix to values to be used by the instantiated GameObject
                        gameObjectTranslation = new Vector3(transform.m03, transform.m13, transform.m23);
                        gameObjectRotation = Quaternion.LookRotation(
                            new Vector3(transform.m02, transform.m12, transform.m22),
                            new Vector3(transform.m01, transform.m11, transform.m21));
                        gameObjectScale = transform.lossyScale;

                        ruleMatched = true;
                        break;
                    }

                if (!ruleMatched)
                {
                    // Fallback to just using the orientMatrix for the translation, rotation, & scale values.
                    gameObjectTranslation = new Vector3(orientMatrix.m03, orientMatrix.m13, orientMatrix.m23);
                    gameObjectRotation = Quaternion.LookRotation(
                        new Vector3(orientMatrix.m02, orientMatrix.m12, orientMatrix.m22),
                        new Vector3(orientMatrix.m01, orientMatrix.m11, orientMatrix.m21));
                    gameObjectScale = orientMatrix.lossyScale;
                }

                instantiatedGameObject.transform.localPosition = gameObjectTranslation +
                                                                 tmpMap.CellToLocalInterpolated(position +
                                                                     tmpMap.tileAnchor);
                instantiatedGameObject.transform.localRotation = gameObjectRotation;
                instantiatedGameObject.transform.localScale = gameObjectScale;
            }

            return true;
        }

        /// <summary>
        ///     Retrieves any tile rendering data from the scripted tile.
        /// </summary>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        /// <param name="tileData">Data to render the tile.</param>
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            var iden = Matrix4x4.identity;

            tileData.sprite = m_DefaultSprite;
            tileData.gameObject = m_DefaultGameObject;
            tileData.colliderType = m_DefaultColliderType;
            tileData.flags = TileFlags.LockTransform;
            tileData.transform = iden;

            var transform = iden;
            foreach (var rule in m_TilingRules)
                if (RuleMatches(rule, position, tilemap, ref transform))
                {
                    switch (rule.m_Output)
                    {
                        case TilingRuleOutput.OutputSprite.Single:
                        case TilingRuleOutput.OutputSprite.Animation:
                            tileData.sprite = rule.m_Sprites[0];
                            break;
                        case TilingRuleOutput.OutputSprite.Random:
                            var index = Mathf.Clamp(
                                Mathf.FloorToInt(GetPerlinValue(position, rule.m_PerlinScale, 100000f) *
                                                 rule.m_Sprites.Length), 0, rule.m_Sprites.Length - 1);
                            tileData.sprite = rule.m_Sprites[index];
                            if (rule.m_RandomTransform != TilingRuleOutput.Transform.Fixed)
                                transform = ApplyRandomTransform(rule.m_RandomTransform, transform, rule.m_PerlinScale,
                                    position);
                            break;
                    }

                    tileData.transform = transform;
                    tileData.gameObject = rule.m_GameObject;
                    tileData.colliderType = rule.m_ColliderType;
                    break;
                }
        }

        /// <summary>
        ///     Returns a Perlin Noise value based on the given inputs.
        /// </summary>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="scale">The Perlin Scale factor of the Tile.</param>
        /// <param name="offset">Offset of the Tile on the Tilemap.</param>
        /// <returns>A Perlin Noise value based on the given inputs.</returns>
        public static float GetPerlinValue(Vector3Int position, float scale, float offset)
        {
            return Mathf.PerlinNoise((position.x + offset) * scale, (position.y + offset) * scale);
        }

        private static bool IsTilemapUsedTilesChange(Tilemap tilemap,
            out KeyValuePair<HashSet<TileBase>, HashSet<Vector3Int>> hashSet)
        {
            if (!m_CacheTilemapsNeighborPositions.TryGetValue(tilemap, out hashSet))
                return true;

            var oldUsedTiles = hashSet.Key;
            var newUsedTilesCount = tilemap.GetUsedTilesCount();
            if (newUsedTilesCount != oldUsedTiles.Count)
                return true;

            if (m_AllocatedUsedTileArr.Length < newUsedTilesCount)
                Array.Resize(ref m_AllocatedUsedTileArr, newUsedTilesCount);

            tilemap.GetUsedTilesNonAlloc(m_AllocatedUsedTileArr);
            for (var i = 0; i < newUsedTilesCount; i++)
            {
                var newUsedTile = m_AllocatedUsedTileArr[i];
                if (!oldUsedTiles.Contains(newUsedTile))
                    return true;
            }

            return false;
        }

        private static KeyValuePair<HashSet<TileBase>, HashSet<Vector3Int>> CachingTilemapNeighborPositions(
            Tilemap tilemap)
        {
            var usedTileCount = tilemap.GetUsedTilesCount();
            var usedTiles = new HashSet<TileBase>();
            var neighborPositions = new HashSet<Vector3Int>();

            if (m_AllocatedUsedTileArr.Length < usedTileCount)
                Array.Resize(ref m_AllocatedUsedTileArr, usedTileCount);

            tilemap.GetUsedTilesNonAlloc(m_AllocatedUsedTileArr);

            for (var i = 0; i < usedTileCount; i++)
            {
                var tile = m_AllocatedUsedTileArr[i];
                usedTiles.Add(tile);
                RuleTile ruleTile = null;

                if (tile is RuleTile rt)
                    ruleTile = rt;
                else if (tile is RuleOverrideTile ot)
                    ruleTile = ot.m_Tile;

                if (ruleTile)
                    foreach (var neighborPosition in ruleTile.neighborPositions)
                        neighborPositions.Add(neighborPosition);
            }

            var value = new KeyValuePair<HashSet<TileBase>, HashSet<Vector3Int>>(usedTiles, neighborPositions);
            m_CacheTilemapsNeighborPositions[tilemap] = value;
            return value;
        }

        private static bool NeedRelease()
        {
            foreach (var keypair in m_CacheTilemapsNeighborPositions)
                if (keypair.Key == null)
                    return true;

            return false;
        }

        private static void ReleaseDestroyedTilemapCacheData()
        {
            if (!NeedRelease())
                return;

            var hasCleared = false;
            var keys = m_CacheTilemapsNeighborPositions.Keys.ToArray();
            foreach (var key in keys)
                if (key == null && m_CacheTilemapsNeighborPositions.Remove(key))
                    hasCleared = true;

            if (hasCleared)
                // TrimExcess
                m_CacheTilemapsNeighborPositions =
                    new Dictionary<Tilemap, KeyValuePair<HashSet<TileBase>, HashSet<Vector3Int>>>(
                        m_CacheTilemapsNeighborPositions);
        }

        /// <summary>
        ///     Retrieves any tile animation data from the scripted tile.
        /// </summary>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        /// <param name="tileAnimationData">Data to run an animation on the tile.</param>
        /// <returns>Whether the call was successful.</returns>
        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap,
            ref TileAnimationData tileAnimationData)
        {
            var transform = Matrix4x4.identity;
            foreach (var rule in m_TilingRules)
                if (rule.m_Output == TilingRuleOutput.OutputSprite.Animation)
                    if (RuleMatches(rule, position, tilemap, ref transform))
                    {
                        tileAnimationData.animatedSprites = rule.m_Sprites;
                        tileAnimationData.animationSpeed =
                            Random.Range(rule.m_MinAnimationSpeed, rule.m_MaxAnimationSpeed);
                        return true;
                    }

            return false;
        }

        /// <summary>
        ///     This method is called when the tile is refreshed.
        /// </summary>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            base.RefreshTile(position, tilemap);

            var baseTilemap = tilemap.GetComponent<Tilemap>();

            ReleaseDestroyedTilemapCacheData(); // Prevent memory leak

            if (IsTilemapUsedTilesChange(baseTilemap, out var neighborPositionsSet))
                neighborPositionsSet = CachingTilemapNeighborPositions(baseTilemap);

            var neighborPositionsRuleTile = neighborPositionsSet.Value;
            foreach (var offset in neighborPositionsRuleTile)
            {
                var offsetPosition = GetOffsetPositionReverse(position, offset);
                var tile = tilemap.GetTile(offsetPosition);
                RuleTile ruleTile = null;

                if (tile is RuleTile rt)
                    ruleTile = rt;
                else if (tile is RuleOverrideTile ot)
                    ruleTile = ot.m_Tile;

                if (ruleTile != null)
                    if (ruleTile == this || ruleTile.neighborPositions.Contains(offset))
                        base.RefreshTile(offsetPosition, tilemap);
            }
        }

        /// <summary>
        ///     Does a Rule Match given a Tiling Rule and neighboring Tiles.
        /// </summary>
        /// <param name="rule">The Tiling Rule to match with.</param>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The tilemap to match with.</param>
        /// <param name="transform">A transform matrix which will match the Rule.</param>
        /// <returns>True if there is a match, False if not.</returns>
        public virtual bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
        {
            if (RuleMatches(rule, position, tilemap, 0))
            {
                transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, 0f), Vector3.one);
                return true;
            }

            // Check rule against rotations of 0, 90, 180, 270
            if (rule.m_RuleTransform == TilingRuleOutput.Transform.Rotated)
            {
                for (var angle = m_RotationAngle; angle < 360; angle += m_RotationAngle)
                    if (RuleMatches(rule, position, tilemap, angle))
                    {
                        transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle), Vector3.one);
                        return true;
                    }
            }
            // Check rule against x-axis, y-axis mirror
            else if (rule.m_RuleTransform == TilingRuleOutput.Transform.MirrorXY)
            {
                if (RuleMatches(rule, position, tilemap, true, true))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, -1f, 1f));
                    return true;
                }

                if (RuleMatches(rule, position, tilemap, true, false))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, 1f, 1f));
                    return true;
                }

                if (RuleMatches(rule, position, tilemap, false, true))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f));
                    return true;
                }
            }
            // Check rule against x-axis mirror
            else if (rule.m_RuleTransform == TilingRuleOutput.Transform.MirrorX)
            {
                if (RuleMatches(rule, position, tilemap, true, false))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, 1f, 1f));
                    return true;
                }
            }
            // Check rule against y-axis mirror
            else if (rule.m_RuleTransform == TilingRuleOutput.Transform.MirrorY)
            {
                if (RuleMatches(rule, position, tilemap, false, true))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f));
                    return true;
                }
            }
            // Check rule against x-axis mirror with rotations of 0, 90, 180, 270
            else if (rule.m_RuleTransform == TilingRuleOutput.Transform.RotatedMirror)
            {
                for (var angle = 0; angle < 360; angle += m_RotationAngle)
                {
                    if (angle != 0 && RuleMatches(rule, position, tilemap, angle))
                    {
                        transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle), Vector3.one);
                        return true;
                    }

                    if (RuleMatches(rule, position, tilemap, angle, true))
                    {
                        transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle),
                            new Vector3(-1f, 1f, 1f));
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Returns a random transform matrix given the random transform rule.
        /// </summary>
        /// <param name="type">Random transform rule.</param>
        /// <param name="original">The original transform matrix.</param>
        /// <param name="perlinScale">The Perlin Scale factor of the Tile.</param>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <returns>A random transform matrix.</returns>
        public virtual Matrix4x4 ApplyRandomTransform(TilingRuleOutput.Transform type, Matrix4x4 original,
            float perlinScale, Vector3Int position)
        {
            var perlin = GetPerlinValue(position, perlinScale, 200000f);
            switch (type)
            {
                case TilingRuleOutput.Transform.MirrorXY:
                    return original * Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                        new Vector3(Math.Abs(perlin - 0.5) > 0.25 ? 1f : -1f, perlin < 0.5 ? 1f : -1f, 1f));
                case TilingRuleOutput.Transform.MirrorX:
                    return original * Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                        new Vector3(perlin < 0.5 ? 1f : -1f, 1f, 1f));
                case TilingRuleOutput.Transform.MirrorY:
                    return original * Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                        new Vector3(1f, perlin < 0.5 ? 1f : -1f, 1f));
                case TilingRuleOutput.Transform.Rotated:
                {
                    var angle = Mathf.Clamp(Mathf.FloorToInt(perlin * m_RotationCount), 0, m_RotationCount - 1) *
                                m_RotationAngle;
                    return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle), Vector3.one);
                }
                case TilingRuleOutput.Transform.RotatedMirror:
                {
                    var angle = Mathf.Clamp(Mathf.FloorToInt(perlin * m_RotationCount), 0, m_RotationCount - 1) *
                                m_RotationAngle;
                    return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle),
                        new Vector3(perlin < 0.5 ? 1f : -1f, 1f, 1f));
                }
            }

            return original;
        }

        /// <summary>
        ///     Returns custom fields for this RuleTile
        /// </summary>
        /// <param name="isOverrideInstance">Whether override fields are returned</param>
        /// <returns>Custom fields for this RuleTile</returns>
        public FieldInfo[] GetCustomFields(bool isOverrideInstance)
        {
            return GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                       BindingFlags.NonPublic)
                .Where(field => typeof(RuleTile).GetField(field.Name) == null)
                .Where(field => field.IsPublic || field.IsDefined(typeof(SerializeField)))
                .Where(field => !field.IsDefined(typeof(HideInInspector)))
                .Where(field => !isOverrideInstance || !field.IsDefined(typeof(DontOverride)))
                .ToArray();
        }

        /// <summary>
        ///     Checks if there is a match given the neighbor matching rule and a Tile.
        /// </summary>
        /// <param name="neighbor">Neighbor matching rule.</param>
        /// <param name="other">Tile to match.</param>
        /// <returns>True if there is a match, False if not.</returns>
        public virtual bool RuleMatch(int neighbor, TileBase other)
        {
            if (other is RuleOverrideTile ot)
                other = ot.m_InstanceTile;

            switch (neighbor)
            {
                case TilingRuleOutput.Neighbor.This: return other == this;
                case TilingRuleOutput.Neighbor.NotThis: return other != this;
            }

            return true;
        }

        /// <summary>
        ///     Checks if there is a match given the neighbor matching rule and a Tile with a rotation angle.
        /// </summary>
        /// <param name="rule">Neighbor matching rule.</param>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">Tilemap to match.</param>
        /// <param name="angle">Rotation angle for matching.</param>
        /// <param name="mirrorX">Mirror X Axis for matching.</param>
        /// <returns>True if there is a match, False if not.</returns>
        public bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, int angle, bool mirrorX = false)
        {
            var minCount = Math.Min(rule.m_Neighbors.Count, rule.m_NeighborPositions.Count);
            for (var i = 0; i < minCount; i++)
            {
                var neighbor = rule.m_Neighbors[i];
                var neighborPosition = rule.m_NeighborPositions[i];
                if (mirrorX)
                    neighborPosition = GetMirroredPosition(neighborPosition, true, false);
                var positionOffset = GetRotatedPosition(neighborPosition, angle);
                var other = tilemap.GetTile(GetOffsetPosition(position, positionOffset));
                if (!RuleMatch(neighbor, other)) return false;
            }

            return true;
        }

        /// <summary>
        ///     Checks if there is a match given the neighbor matching rule and a Tile with mirrored axii.
        /// </summary>
        /// <param name="rule">Neighbor matching rule.</param>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">Tilemap to match.</param>
        /// <param name="mirrorX">Mirror X Axis for matching.</param>
        /// <param name="mirrorY">Mirror Y Axis for matching.</param>
        /// <returns>True if there is a match, False if not.</returns>
        public bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, bool mirrorX, bool mirrorY)
        {
            var minCount = Math.Min(rule.m_Neighbors.Count, rule.m_NeighborPositions.Count);
            for (var i = 0; i < minCount; i++)
            {
                var neighbor = rule.m_Neighbors[i];
                var positionOffset = GetMirroredPosition(rule.m_NeighborPositions[i], mirrorX, mirrorY);
                var other = tilemap.GetTile(GetOffsetPosition(position, positionOffset));
                if (!RuleMatch(neighbor, other)) return false;
            }

            return true;
        }

        /// <summary>
        ///     Gets a rotated position given its original position and the rotation in degrees.
        /// </summary>
        /// <param name="position">Original position of Tile.</param>
        /// <param name="rotation">Rotation in degrees.</param>
        /// <returns>Rotated position of Tile.</returns>
        public virtual Vector3Int GetRotatedPosition(Vector3Int position, int rotation)
        {
            switch (rotation)
            {
                case 0:
                    return position;
                case 90:
                    return new Vector3Int(position.y, -position.x, 0);
                case 180:
                    return new Vector3Int(-position.x, -position.y, 0);
                case 270:
                    return new Vector3Int(-position.y, position.x, 0);
            }

            return position;
        }

        /// <summary>
        ///     Gets a mirrored position given its original position and the mirroring axii.
        /// </summary>
        /// <param name="position">Original position of Tile.</param>
        /// <param name="mirrorX">Mirror in the X Axis.</param>
        /// <param name="mirrorY">Mirror in the Y Axis.</param>
        /// <returns>Mirrored position of Tile.</returns>
        public virtual Vector3Int GetMirroredPosition(Vector3Int position, bool mirrorX, bool mirrorY)
        {
            if (mirrorX)
                position.x *= -1;
            if (mirrorY)
                position.y *= -1;
            return position;
        }

        /// <summary>
        ///     Get the offset for the given position with the given offset.
        /// </summary>
        /// <param name="position">Position to offset.</param>
        /// <param name="offset">Offset for the position.</param>
        /// <returns>The offset position.</returns>
        public virtual Vector3Int GetOffsetPosition(Vector3Int position, Vector3Int offset)
        {
            return position + offset;
        }

        /// <summary>
        ///     Get the reversed offset for the given position with the given offset.
        /// </summary>
        /// <param name="position">Position to offset.</param>
        /// <param name="offset">Offset for the position.</param>
        /// <returns>The reversed offset position.</returns>
        public virtual Vector3Int GetOffsetPositionReverse(Vector3Int position, Vector3Int offset)
        {
            return position - offset;
        }

        /// <summary>
        ///     The data structure holding the Rule information for matching Rule Tiles with
        ///     its neighbors.
        /// </summary>
        [Serializable]
        public class TilingRuleOutput
        {
            /// <summary>
            ///     The Output for the Tile which fits this Rule.
            /// </summary>
            public enum OutputSprite
            {
                /// <summary>
                ///     A Single Sprite will be output.
                /// </summary>
                Single,

                /// <summary>
                ///     A Random Sprite will be output.
                /// </summary>
                Random,

                /// <summary>
                ///     A Sprite Animation will be output.
                /// </summary>
                Animation
            }

            /// <summary>
            ///     The enumeration for the transform rule used when matching Rule Tiles.
            /// </summary>
            public enum Transform
            {
                /// <summary>
                ///     The Rule Tile will match Tiles exactly as laid out in its neighbors.
                /// </summary>
                Fixed,

                /// <summary>
                ///     The Rule Tile will rotate and match its neighbors.
                /// </summary>
                Rotated,

                /// <summary>
                ///     The Rule Tile will mirror in the X axis and match its neighbors.
                /// </summary>
                MirrorX,

                /// <summary>
                ///     The Rule Tile will mirror in the Y axis and match its neighbors.
                /// </summary>
                MirrorY,

                /// <summary>
                ///     The Rule Tile will mirror in the X or Y axis and match its neighbors.
                /// </summary>
                MirrorXY,

                /// <summary>
                ///     The Rule Tile will rotate and mirror in the X and match its neighbors.
                /// </summary>
                RotatedMirror
            }

            /// <summary>
            ///     Id for this Rule.
            /// </summary>
            public int m_Id;

            /// <summary>
            ///     The output Sprites for this Rule.
            /// </summary>
            public Sprite[] m_Sprites = new Sprite[1];

            /// <summary>
            ///     The output GameObject for this Rule.
            /// </summary>
            public GameObject m_GameObject;

            /// <summary>
            ///     The output minimum Animation Speed for this Rule.
            /// </summary>
            [FormerlySerializedAs("m_AnimationSpeed")]
            public float m_MinAnimationSpeed = 1f;

            /// <summary>
            ///     The output maximum Animation Speed for this Rule.
            /// </summary>
            [FormerlySerializedAs("m_AnimationSpeed")]
            public float m_MaxAnimationSpeed = 1f;

            /// <summary>
            ///     The perlin scale factor for this Rule.
            /// </summary>
            public float m_PerlinScale = 0.5f;

            /// <summary>
            ///     The output type for this Rule.
            /// </summary>
            public OutputSprite m_Output = OutputSprite.Single;

            /// <summary>
            ///     The output Collider Type for this Rule.
            /// </summary>
            public Tile.ColliderType m_ColliderType = Tile.ColliderType.Sprite;

            /// <summary>
            ///     The randomized transform output for this Rule.
            /// </summary>
            public Transform m_RandomTransform;

            /// <summary>
            ///     The enumeration for matching Neighbors when matching Rule Tiles
            /// </summary>
            public class Neighbor
            {
                /// <summary>
                ///     The Rule Tile will check if the contents of the cell in that direction is an instance of this Rule Tile.
                ///     If not, the rule will fail.
                /// </summary>
                public const int This = 1;

                /// <summary>
                ///     The Rule Tile will check if the contents of the cell in that direction is not an instance of this Rule Tile.
                ///     If it is, the rule will fail.
                /// </summary>
                public const int NotThis = 2;
            }
        }

        /// <summary>
        ///     The data structure holding the Rule information for matching Rule Tiles with
        ///     its neighbors.
        /// </summary>
        [Serializable]
        public class TilingRule : TilingRuleOutput
        {
            /// <summary>
            ///     The matching Rule conditions for each of its neighboring Tiles.
            /// </summary>
            public List<int> m_Neighbors = new();

            /// <summary>
            ///     * Preset this list to RuleTile backward compatible, but not support for HexagonalRuleTile backward compatible.
            /// </summary>
            public List<Vector3Int> m_NeighborPositions = new()
            {
                new(-1, 1, 0),
                new(0, 1, 0),
                new(1, 1, 0),
                new(-1, 0, 0),
                new(1, 0, 0),
                new(-1, -1, 0),
                new(0, -1, 0),
                new(1, -1, 0)
            };

            /// <summary>
            ///     The transform matching Rule for this Rule.
            /// </summary>
            public Transform m_RuleTransform;

            /// <summary>
            ///     This clones a copy of the TilingRule.
            /// </summary>
            /// <returns>A copy of the TilingRule.</returns>
            public TilingRule Clone()
            {
                var rule = new TilingRule
                {
                    m_Neighbors = new List<int>(m_Neighbors),
                    m_NeighborPositions = new List<Vector3Int>(m_NeighborPositions),
                    m_RuleTransform = m_RuleTransform,
                    m_Sprites = new Sprite[m_Sprites.Length],
                    m_GameObject = m_GameObject,
                    m_MinAnimationSpeed = m_MinAnimationSpeed,
                    m_MaxAnimationSpeed = m_MaxAnimationSpeed,
                    m_PerlinScale = m_PerlinScale,
                    m_Output = m_Output,
                    m_ColliderType = m_ColliderType,
                    m_RandomTransform = m_RandomTransform
                };
                Array.Copy(m_Sprites, rule.m_Sprites, m_Sprites.Length);
                return rule;
            }

            /// <summary>
            ///     Returns all neighbors of this Tile as a dictionary
            /// </summary>
            /// <returns>A dictionary of neighbors for this Tile</returns>
            public Dictionary<Vector3Int, int> GetNeighbors()
            {
                var dict = new Dictionary<Vector3Int, int>();

                for (var i = 0; i < m_Neighbors.Count && i < m_NeighborPositions.Count; i++)
                    dict.Add(m_NeighborPositions[i], m_Neighbors[i]);

                return dict;
            }

            /// <summary>
            ///     Applies the values from the given dictionary as this Tile's neighbors
            /// </summary>
            /// <param name="dict">Dictionary to apply values from</param>
            public void ApplyNeighbors(Dictionary<Vector3Int, int> dict)
            {
                m_NeighborPositions = dict.Keys.ToList();
                m_Neighbors = dict.Values.ToList();
            }

            /// <summary>
            ///     Gets the cell bounds of the TilingRule.
            /// </summary>
            /// <returns>Returns the cell bounds of the TilingRule.</returns>
            public BoundsInt GetBounds()
            {
                var bounds = new BoundsInt(Vector3Int.zero, Vector3Int.one);
                foreach (var neighbor in GetNeighbors())
                {
                    bounds.xMin = Mathf.Min(bounds.xMin, neighbor.Key.x);
                    bounds.yMin = Mathf.Min(bounds.yMin, neighbor.Key.y);
                    bounds.xMax = Mathf.Max(bounds.xMax, neighbor.Key.x + 1);
                    bounds.yMax = Mathf.Max(bounds.yMax, neighbor.Key.y + 1);
                }

                return bounds;
            }
        }

        /// <summary>
        ///     Attribute which marks a property which cannot be overridden by a RuleOverrideTile
        /// </summary>
        public class DontOverride : Attribute
        {
        }
    }
}