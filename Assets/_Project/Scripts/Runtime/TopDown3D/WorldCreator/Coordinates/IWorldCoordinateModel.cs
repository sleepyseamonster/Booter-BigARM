namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// The sole replaceable authority between the future thematic coordinate system and engine planning space.
    /// Implementations own their canonical notation, projection, bounds, and wrapping rules.
    /// </summary>
    public interface IWorldCoordinateModel
    {
        string ModelId { get; }
        int ModelVersion { get; }

        WorldCoordinateAddress Encode(AbsoluteWorldPosition absolutePosition);
        bool TryResolve(WorldCoordinateAddress address, out AbsoluteWorldPosition absolutePosition);
    }
}
