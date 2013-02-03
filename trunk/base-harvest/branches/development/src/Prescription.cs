//  Copyright 2005-2010 Portland State University, University of Wisconsin
//  Authors:  Robert M. Scheller, James B. Domingo

using Edu.Wisc.Forest.Flel.Util;
using Landis.Library.AgeOnlyCohorts;
using Landis.SpatialModeling;
using Landis.Core;
using Landis.Library.LandUses;
using Landis.Library.Succession;
using log4net;
using System.Reflection;

namespace Landis.Extension.BaseHarvest
{
    /// <summary>
    /// A prescription describes how stands are ranked, how sites are selected,
    /// and which cohorts are harvested.
    /// </summary>
    public class Prescription
        : ISpeciesCohortsDisturbance
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly bool isDebugEnabled = log.IsDebugEnabled;

        private static int nextNumber = 1;
        private int number;
        private string name;
        private IStandRankingMethod rankingMethod;
        private ISiteSelector siteSelector;
        private ICohortSelector cohortSelector;
        private Planting.SpeciesList speciesToPlant;
        private ActiveSite currentSite;
        private Stand currentStand;
        private int minTimeSinceDamage;
        private bool preventEstablishment;
        private LandUse landUseAfterHarvest;
        private static LandUse landUseMostRecentlyChecked = null;

        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's number.
        /// </summary>
        /// <remarks>
        /// Each prescription's number is unique, and is generated and assigned
        /// when the prescription is initialized.
        /// </remarks>
        public int Number
        {
            get {
                return number;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The total number of prescription.
        /// </summary>
        public static int Count
        {
            get {
                return nextNumber;
            }
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's name.
        /// </summary>
        public string Name
        {
            get {
                return name;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's method for ranking stands.
        /// </summary>
        public IStandRankingMethod StandRankingMethod
        {
            get {
                return rankingMethod;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's method for selecting sites in stands.
        /// </summary>
        public ISiteSelector SiteSelectionMethod
        {
            set {
                siteSelector = value;
            }
            get {
                return siteSelector;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Sets the cohorts that will be removed by the prescription.
        /// </summary>
        /// <remarks>
        /// The purpose of this property is to allow derived classes to change
        /// the cohort selector; for example, a single repeat-harvest switching
        /// between its two cohort selectors.
        /// </remarks>
        protected ICohortSelector CohortSelector
        {
            set {
                cohortSelector = value;
            }
        }

        protected ISiteSelector SiteSelector
        {
            set
            {
                siteSelector = value;
            }
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Sets the optional list of species that are planted at each site
        /// harvested by the prescription.
        /// </summary>
        /// <remarks>
        /// The purpose of this property is to allow derived classes to change
        /// the species list; for example, a single repeat-harvest switching
        /// between the lists for initial harvests and repeat harvests.
        /// </remarks>
        public Planting.SpeciesList SpeciesToPlant
        {
            set {
                speciesToPlant = value;
            }
            get {
                return speciesToPlant;
            }
        }
        

        //---------------------------------------------------------------------

        ExtensionType IDisturbance.Type
        {
            get {
                return PlugIn.ExtType;
            }
        }

        //---------------------------------------------------------------------

        ActiveSite IDisturbance.CurrentSite
        {
            get {
                return currentSite;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's time limit for harvesting damaged sites. If
        /// a site has been damaged less than this, it should not be harvested.
        /// </summary>
        public int MinTimeSinceDamage {
            get {
                return minTimeSinceDamage;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's boolean variable for preventing establishment
        /// </summary>
        public bool PreventEstablishment {
            get {
                return preventEstablishment;
            }
        }

        //---------------------------------------------------------------------

        public Prescription(string               name,
                            IStandRankingMethod  rankingMethod,
                            ISiteSelector        siteSelector,
                            ICohortSelector      cohortSelector,
                            Planting.SpeciesList speciesToPlant,
                            int                  minTimeSinceDamage,
                            bool                 preventEstablishment)
        {
            this.number = nextNumber;
            nextNumber++;

            this.name = name;
            this.rankingMethod = rankingMethod;
            this.siteSelector = siteSelector;
            this.cohortSelector = cohortSelector;
            this.speciesToPlant = speciesToPlant;
            this.minTimeSinceDamage = minTimeSinceDamage;
            this.preventEstablishment = preventEstablishment;

            this.landUseAfterHarvest = landUseMostRecentlyChecked;
        }

        //---------------------------------------------------------------------

        public static void CheckLandUseName(string name)
        {
            // scan the name for LU-->{LU-name}
            const string LUmarker = "LU-->";
            int positionOfLUmarker = name.IndexOf(LUmarker);
            int positionOfLUname = positionOfLUmarker + LUmarker.Length;
            string LUname = name.Substring(positionOfLUname);
            landUseMostRecentlyChecked = LandUse.LookupName(LUname);
            if (landUseMostRecentlyChecked == null)
                throw new InputValueException(LUname, "Unknown land-use name \"{0}\"", LUname);
        }

        //---------------------------------------------------------------------
        
        /// <summary>
        /// Harvests a stand (and possibly its neighbors) according to the
        /// prescription's site-selection method.
        /// </summary>
        /// <returns>
        /// The area that was harvested (units: hectares).
        /// </returns>
        // This is called by AppliedPrescription
        public virtual void Harvest(Stand stand)
        {
            if (isDebugEnabled)
                log.DebugFormat("  Prescription {0} is harvesting stand {1}", this.Name, stand.MapCode);

            //set prescription name for stand
            stand.PrescriptionName = this.Name;
            stand.HarvestedRank = PlugIn.CurrentRank;
            stand.LastPrescription = this;
            
            stand.MinTimeSinceDamage = this.minTimeSinceDamage;
            
            //set current stand
            currentStand = stand;
            currentStand.ClearDamageTable();

            // SelectSites(stand) is where either complete, complete stand spreading, or partial stand
            // spreading are activated.
            // tjs - This is what gets the sites that will be harvested
           

            foreach (ActiveSite site in siteSelector.SelectSites(stand)) {
                currentSite = site;

                SiteVars.Cohorts[site].RemoveMarkedCohorts(this);         

                //  In order for easement prescriptions to properly "spread", we need to count
                //  each site in its area harvested even if no cohorts are damaged.
                //if (SiteVars.CohortsDamaged[site] > 0)
                {
                    stand.LastAreaHarvested += PlugIn.ModelCore.CellArea;
                }    

                //  With land-use, a prescription doesn't necessarily damage a site's cohorts
                //  (for example, an easement prescription).  So, always assign the current
                //  prescription to the site, regardless if any cohorts were damaged.  This
                //  will cause the prescription to appear on the timestep's prescription map.
                SiteVars.Prescription[site] = this;

                if (speciesToPlant != null)
                    Reproduction.ScheduleForPlanting(speciesToPlant, site);

                if (landUseAfterHarvest != null)
                {
                    if (isDebugEnabled)
                    {
                        string landUseBefore = (LandUse.SiteVar[site] == null) ? "(null)" : LandUse.SiteVar[site].Name;
                        log.DebugFormat("    site {0}, land use: {1} --> {2}", site, landUseBefore, landUseAfterHarvest.Name);
                        if (landUseBefore != "forest")
                            log.DebugFormat("     stand rank = {0}", stand.Rank);
                    }
                    LandUse.SiteVar[site] = landUseAfterHarvest;

                    // Now that the site's land-use has changed, set its stand aside for the rest
                    // of the scenario.  Note: because of stand-spreading algorithm, the site may
                    // not belong to the stand that this method was called with.
                    SiteVars.Stand[site].SetAsideUntil(PlugIn.ModelCore.EndTime + 1);
                }
            } 
            return; 
        } 

        //---------------------------------------------------------------------
        void ISpeciesCohortsDisturbance.MarkCohortsForDeath(ISpeciesCohorts cohorts,
                                                         ISpeciesCohortBoolArray isDamaged)
        {
            cohortSelector.Harvest(cohorts, isDamaged);

            int cohortsDamaged = 0;
            for (int i = 0; i < isDamaged.Count; i++) {
                if (isDamaged[i]) {
                    
                    //if this cohort is killed, update the damage table (for the stand of this site) with this species name
                    SiteVars.Stand[currentSite].UpdateDamageTable(cohorts.Species.Name);
                    //PlugIn.ModelCore.UI.WriteLine("Damaged:  {0}.", cohorts.Species.Name);
                    
                    //and increment the cohortsDamaged
                    cohortsDamaged++;
                }
            }
            SiteVars.CohortsDamaged[currentSite] += cohortsDamaged;
        }
    }
}