//  Copyright 2005-2009 University of Wisconsin, Portland State University
//  Authors:  Jimm Domingo, Robert M. Scheller
//  License:  Available at
//  http://www.landis-ii.org/developers/LANDIS-IISourceCodeLicenseAgreement.pdf

namespace Landis.Wind
{
    /// <summary>
    /// Size and frequency parameters for wind events in an ecoregion.
    /// </summary>
    public interface IEventParameters
    {
        /// <summary>
        /// Maximum event size (hectares).
        /// </summary>
        double MaxSize
        {
            get;set;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Mean event size (hectares).
        /// </summary>
        double MeanSize
        {
            get;set;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Minimum event size (hectares).
        /// </summary>
        double MinSize
        {
            get;set;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Wind rotation period (years).
        /// </summary>
        int RotationPeriod
        {
            get;set;
        }
    }
}
