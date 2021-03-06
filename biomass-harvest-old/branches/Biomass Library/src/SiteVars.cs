// Copyright 2008 Green Code LLC
// Copyright 2010 Portland State University
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Contributors:
//   James Domingo, Green Code LLC
//   Robert M. Scheller, Portland State University


using Landis.SpatialModeling;
using Landis.Library.BiomassCohorts;
using Landis.Library.Biomass;

namespace Landis.Extension.BiomassHarvest
{
    public class SiteVars : Landis.Extension.BaseHarvest.SiteVars
    {
        private static ISiteVar<double> biomassRemoved;
        private static ISiteVar<int> cohortsPartiallyDamaged;
        private static ISiteVar<double> capacityReduction;
        private static ISiteVar<Pool> woodyDebris;
        private static ISiteVar<Pool> litter;

        private static ISiteVar<ISiteCohorts> cohorts;

        //---------------------------------------------------------------------

        public static new void Initialize()
        {

            woodyDebris = PlugIn.ModelCore.GetSiteVar<Pool>("Succession.WoodyDebris");
            litter = PlugIn.ModelCore.GetSiteVar<Pool>("Succession.Litter");
            cohorts = PlugIn.ModelCore.GetSiteVar<ISiteCohorts>("Succession.BiomassCohorts");

            biomassRemoved = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            cohortsPartiallyDamaged = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            capacityReduction = PlugIn.ModelCore.Landscape.NewSiteVar<double>();

            SiteVars.CapacityReduction.ActiveSiteValues = 0.0;

            PlugIn.ModelCore.RegisterSiteVar(SiteVars.CapacityReduction, "Harvest.CapacityReduction");

            if (cohorts == null)
            {
                string mesg = string.Format("Cohorts are empty.  Please double-check that this extension is compatible with your chosen succession extension.");
                throw new System.ApplicationException(mesg);
            }


        }
        //---------------------------------------------------------------------
        public static new ushort GetMaxAge(ActiveSite site)
        {
            int maxAge = 0;
            foreach (ISpeciesCohorts sppCo in SiteVars.Cohorts[site])
                foreach (ICohort cohort in sppCo)
                    if (cohort.Age > maxAge)
                        maxAge = cohort.Age;

            return (ushort) maxAge;

        }

        //---------------------------------------------------------------------

        public static ISiteVar<double> BiomassRemoved
        {
            get {
                return biomassRemoved;
            }
        }
        //---------------------------------------------------------------------

        public static ISiteVar<int> CohortsPartiallyDamaged
        {
            get {
                return cohortsPartiallyDamaged;
            }
        }
        //---------------------------------------------------------------------

        public static ISiteVar<double> CapacityReduction
        {
            get {
                return capacityReduction;
            }
        }

        //---------------------------------------------------------------------
        public static new ISiteVar<ISiteCohorts> Cohorts
        {
            get
            {
                return cohorts;
            }
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// The intact dead woody pools for the landscape's sites.
        /// </summary>
        public static ISiteVar<Pool> WoodyDebris
        {
            get
            {
                return woodyDebris;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The dead non-woody pools for the landscape's sites.
        /// </summary>
        public static ISiteVar<Pool> Litter
        {
            get
            {
                return litter;
            }
        }

    }
}
