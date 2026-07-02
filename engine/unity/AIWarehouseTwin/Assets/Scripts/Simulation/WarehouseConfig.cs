using System;

namespace AIWarehouseTwin.Simulation
{
    /// <summary>
    /// Inspector-friendly warehouse scenario inputs for the Unity demo seam.
    /// </summary>
    [Serializable]
    public class WarehouseConfig
    {
        /// <summary>
        /// Warehouse length in meters.
        /// </summary>
        public float lengthM = 40f;

        /// <summary>
        /// Warehouse width in meters.
        /// </summary>
        public float widthM = 20f;

        /// <summary>
        /// Number of shelf rows in the warehouse.
        /// </summary>
        public int shelfRows = 3;

        /// <summary>
        /// Number of SKUs available in the scenario.
        /// </summary>
        public int skuCount = 200;

        /// <summary>
        /// Number of workers available in the scenario.
        /// </summary>
        public int workerCount = 5;

        /// <summary>
        /// Number of forklifts available in the scenario.
        /// </summary>
        public int forkliftCount = 2;

        /// <summary>
        /// Number of orders represented by the scenario.
        /// </summary>
        public int orderCount = 50;
    }
}
