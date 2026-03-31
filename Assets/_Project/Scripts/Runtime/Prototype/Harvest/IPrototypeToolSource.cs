namespace BooterBigArm.Runtime
{
    /// <summary>
    /// Optional seam for tool-gated harvesting. Return an empty string when no tool is equipped.
    /// </summary>
    public interface IPrototypeToolSource
    {
        string EquippedToolId { get; }
    }
}
