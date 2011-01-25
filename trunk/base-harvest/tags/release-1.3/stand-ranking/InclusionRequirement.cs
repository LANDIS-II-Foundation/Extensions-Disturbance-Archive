using Landis.Species;
using Landis.AgeCohort;
using Landis.Cohorts;
using Landis.Landscape;
using Edu.Wisc.Forest.Flel.Util;
using System;
using System.Collections.Generic;


namespace Landis.Harvest
{

    public class InclusionRequirement
        : IRequirement
    {
    
        //list or rules for this inclusion requirement
        List<InclusionRule> rule_list = new List<InclusionRule>();

        //---------------------------------------------------------------------

        /// <summary>
        /// constructor, assigns the list of inclusion rules
        /// </summary>
        
        public InclusionRequirement(List<InclusionRule> rule_list) {
            //assign the list of inclusion rules
            this.rule_list = rule_list;
        }

        //---------------------------------------------------------------------
        
        /// <summary>
        /// use combination of all rules in rule_list to determine if this stand meets this inclusion requirement
        /// </summary>
        bool IRequirement.MetBy(Stand stand) {
            
            int optionalStatements = 0;
            int optionalStatementsMet = 0;
            
            //loop through each rule checking it against this stand
            foreach (InclusionRule rule in rule_list) {

                    //boolean for condition checking
                    bool meets = false;
                    
                    //assign a bool for the generic condition: there are enough of this rule's species in the stand.
                    //meets = check_rule(stand, stand_cohorts, rule, get_key(key_start, rule.RuleAgeRange), stand.SiteCount, rule.PercentOfCells);
                    meets = CheckRule(stand, rule);
                    
                    //forbidden
                    if (rule.InclusionType == "Forbidden") {
                        if (meets) {
                            //this stand meets a 'forbidden' rule so it is disqualified (can return false right away)
//                          UI.WriteLine("returning false1");
                            return false;
                        }
                    }
                    //required
                    else if (rule.InclusionType == "Required") {
                        if (!meets) {
                            //this stand violates a 'required' rule so it is disqualified (can return false right away)
                            //UI.WriteLine("returning false2");
                            return false;
                        }
                        else {
                            //this stand does not violate this 'required' rule.  in case of no optionals, 
                            //it must know that it has fulfilled a required rule to return true.
//                          UI.WriteLine("returning true1");
                            //required_flag = true;
                        }
                    }
                    //optional
                    else if (rule.InclusionType == "Optional") {
                        //mark that there has been an optional rule even included in the requirement
                        optionalStatements++;
                        if (meets) {
                            //this stand met an 'optional' rule so it is still qualified (but can't return anything because it
                            //may not be done checking all rules against this stand)
                            //however, mark that at least 1 optional rule is fulfilled
                            optionalStatementsMet++;
                        }
                    }
                //}
                
                //do nothing if key is not found.
                //catch (KeyNotFoundException) {
                //    UI.WriteLine("Key Not Found Exception");
                //}
            }

            
            if (optionalStatements > 1 && optionalStatementsMet < 1) 
                return false;
            
//          UI.WriteLine("returning default truth!!");
            return true;
        }
        
        //---------------------------------------------------------------------
        // Re-written by R. Scheller to simplify and speed the stand processing.
        private bool CheckRule(Stand stand, InclusionRule rule)
        {
            //bool meets = false;
            int numCellsValid = 0;
            int numActiveCells = 0;
            
            int[] numCellsOtherSpecies = new int[Model.Core.Species.Count];
            
            foreach(ActiveSite site in stand.GetActiveSites())
            {
                //if(!site.IsActive)
                //    continue;

                numActiveCells++;
                    
                bool goodSite = false;
                //bool otherSpecies = false;
                    
                AgeCohort.ISiteCohorts siteCohorts = (AgeCohort.ISiteCohorts) Model.Core.SuccessionCohorts[site];
                
                foreach(ISpecies species in Model.Core.Species)
                {
                    if(siteCohorts[species] != null)
                    {
                        foreach (AgeCohort.ICohort cohort in siteCohorts[species])
                        {
                            if (rule.SpeciesList.Contains(species.Name) && rule.RuleAgeRange.Contains(cohort.Age)) 
                            {
                                goodSite = true;
                            }
                        
                            // Some other species, NOT in the list
                            if (!rule.SpeciesList.Contains(species.Name) && rule.RuleAgeRange.Contains(cohort.Age))   
                            {
                                //otherSpecies = true;
                                numCellsOtherSpecies[species.Index]++;
                            }
                        }
                    
                    }
                }
                
                if(goodSite) 
                    numCellsValid++;

                //if(otherSpecies)
                //numCellsOtherSpecies[species.Index]++;

                
            }  // done looping through sites
            
           if(numCellsValid == 0)  // There are no good cells whatsoever.
               return false;

            bool highest = true;

            //If percent != -1, compare to the Percent of Cells
            if (rule.PercentOfCells != -1) 
            {
                double targetNumCells = (double) numActiveCells * rule.PercentOfCells;
                
                if(targetNumCells > numActiveCells)
                {
                    string message = string.Format("  Harvest Inclusion Rule Error:  target number of cells {0} exceeds number in stand {1}", targetNumCells, numActiveCells);
                    throw new ApplicationException(message);
                }
                if(numCellsValid >= targetNumCells)
                {
                    //UI.WriteLine("       numGoodSites={0}, targetNumCells={1}", numCellsValid, targetNumCells);
                    return true;
                }
            }
            
            //If percent == -1, use 'highest' evaluation algorithm
            else 
            {
                    
                for(int i = 0; i < Model.Core.Species.Count; i++)
                {
                    if(numCellsValid < numCellsOtherSpecies[i])
                        highest = false;
                    //UI.WriteLine("       numGoodSites={0}, otherSppCnt={1}, true? {2}", numCellsValid, otherSpeciesCount[i], highest);
                }
            }
            
            return highest;
            
        }
        
    }
}

        

