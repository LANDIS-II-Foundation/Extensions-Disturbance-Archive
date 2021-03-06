//  Copyright 2005-2010 Portland State University, University of Wisconsin
//  Authors:  Robert M. Scheller, James B. Domingo

using Landis.Core;

namespace Landis.Extension.BaseHarvest
{
    /// <summary>
    /// A collection of parameters for computing the economic ranks of species.
    /// </summary>
    public class EconomicRankTable
    {
        private EconomicRankParameters[] parameters;

        //---------------------------------------------------------------------

        public EconomicRankParameters this[ISpecies species]
        {
            get {
                return parameters[species.Index];
            }

            set {
                parameters[species.Index] = value;
            }
        }

        //---------------------------------------------------------------------

        public EconomicRankTable()
        {
            parameters = new EconomicRankParameters[PlugIn.ModelCore.Species.Count];
        }
    }
}
