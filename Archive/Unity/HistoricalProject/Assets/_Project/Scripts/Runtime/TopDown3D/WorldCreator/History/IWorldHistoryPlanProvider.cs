using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// Supplies deterministic, bounded causal-history plans to an on-demand terrain window.
    /// Future site generators may implement this seam without becoming a second terrain authority.
    /// </summary>
    public interface IWorldHistoryPlanProvider
    {
        IReadOnlyList<WorldHistoryPlan> GetPlans(
            WorldIdentity world,
            IWorldCoordinateModel coordinateModel,
            CanyonSystemCellIndex windowCell,
            double cellSpan);
    }
}
