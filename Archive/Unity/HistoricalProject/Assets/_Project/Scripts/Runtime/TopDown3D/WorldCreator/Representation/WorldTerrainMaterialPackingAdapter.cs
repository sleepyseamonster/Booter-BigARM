using UnityEngine;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// The rollback seam for the current terrain shader. Canonical material semantics stop here;
    /// RGBA channel meaning may change without changing world plans or material queries.
    /// </summary>
    public static class WorldTerrainMaterialPackingAdapter
    {
        public static Color Pack(WorldSurfaceMaterialSample sample)
        {
            var deposited = Mathf.Clamp01(
                sample.Deposit * 0.82f
                + sample.Shelter * sample.Sediment * 0.24f
                - sample.WindExposure * 0.12f);
            var gravel = Mathf.Clamp01(
                sample.Erosion * 0.62f
                + sample.Sediment * 0.28f
                + sample.StrataExposure * 0.18f);
            var bedrock = Mathf.Clamp01(
                sample.StrataExposure * 0.88f
                + sample.Erosion * 0.18f
                - sample.Deposit * 0.28f);
            return new Color(deposited, gravel, bedrock, sample.Weathering);
        }
    }
}
