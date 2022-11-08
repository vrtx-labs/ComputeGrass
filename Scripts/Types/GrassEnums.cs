namespace VRTX.ComputeGrass
{

    public enum Shapes
    {
        /// <summary>
        /// A tapered, stylised leaf blade (pointy tip and narrowed nodality)
        /// </summary>
        Tapered,
        /// <summary>
        /// A rectangular leaf blade template to support texture mapping
        /// </summary>
        Rectangular
    }
    public enum RecomputeEvents
    {
        /// <summary>
        /// Compute update is triggered OnEnable only. Wind and LOD Culling does not get updated in realtime in this mode
        /// </summary>
        Enable,
        /// <summary>
        /// Compute update is triggered in OnEnable and LateUpdate. Wind and LOD updates in realtime
        /// </summary>
        Update
    }
}