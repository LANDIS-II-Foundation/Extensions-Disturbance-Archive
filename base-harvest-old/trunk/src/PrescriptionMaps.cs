// This file is part of the Base Harvest extension for LANDIS-II.
// For copyright and licensing information, see the NOTICE and LICENSE
// files in this project's top-level directory, and at:
//   http://landis-extensions.googlecode.com/svn/trunk/base-harvest/trunk/

using Landis.SpatialModeling;

namespace Landis.Extension.BaseHarvest
{
    /// <summary>
    /// Utility class for prescription maps.
    /// </summary>
    public class PrescriptionMaps
    {
        private string nameTemplate;

        //---------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="nameTemplate">
        /// The template for the pathnames to the maps.
        /// </param>
        public PrescriptionMaps(string nameTemplate)
        {
            this.nameTemplate = nameTemplate;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Writes an output map of prescriptions that harvested each active
        /// site.
        /// </summary>
        /// <param name="timestep">
        /// Timestep to use in the map's name.
        /// </param>
        public void WriteMap(int timestep)
        {
            string path = MapNames.ReplaceTemplateVars(nameTemplate, timestep);
            using (IOutputRaster<ShortPixel> outputRaster = PlugIn.ModelCore.CreateRaster<ShortPixel>(path, PlugIn.ModelCore.Landscape.Dimensions))
            {
                ShortPixel pixel = outputRaster.BufferPixel;
            
                foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
                {
                    if (site.IsActive) {
                        Prescription prescription = SiteVars.Prescription[site];
                        if (prescription == null)
                            pixel.MapCode.Value = 1;
                        else
                            pixel.MapCode.Value = (short)(prescription.Number + 1);
                    }
                    else {
                        //  Inactive site
                        pixel.MapCode.Value = 0;
                    }
                    outputRaster.WriteBufferPixel();
                }
            }
        }

    }
}
