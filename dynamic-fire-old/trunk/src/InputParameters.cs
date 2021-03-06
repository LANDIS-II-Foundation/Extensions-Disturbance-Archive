//  Copyright 2006-2010 USFS Portland State University, Northern Research Station, University of Wisconsin
//  Authors:  Robert M. Scheller, Brian R. Miranda 

using Edu.Wisc.Forest.Flel.Util;
using System.Collections.Generic;

namespace Landis.Extension.DynamicFire
{

    public enum SizeType {size_based, duration_based};
    public enum Distribution {gamma, lognormal, normal, Weibull};

    /// <summary>
    /// Parameters for the plug-in.
    /// </summary>
    public interface IInputParameters
    {
        int Timestep{get;set;}
        SizeType FireSizeType{get;set;}
        bool BUI{get;set;}
        double SeverityCalibrate { get;set;}
        List<IDynamicFireRegion> DynamicFireRegions {get;}
        List<IDynamicWeather> DynamicWeather { get;}
        ISeasonParameters[] SeasonParameters{get;}
        IFuelType[] FuelTypeParameters{get;}
        List<IFireDamage> FireDamages{get;}
        string MapNamesTemplate{get;set;}
        string LogFileName{get;set;}
        string SummaryLogFileName{get;set;}
        string InitialWeatherPath{get;set;}
    }
}

namespace Landis.Extension.DynamicFire
{
    /// <summary>
    /// Parameters for the plug-in.
    /// </summary>
    public class InputParameters
        : IInputParameters
    {
        private int timestep;
        private SizeType fireSizeType;
        private bool buildUpIndex;
        private double severityCalibrate;
        private List<IDynamicFireRegion> dynamicFireRegions;
        private List<IDynamicWeather> dynamicWeather;
        private ISeasonParameters[] seasons;
        private IFuelType[] fuelTypeParameters;
        private List<IFireDamage> damages;
        private string mapNamesTemplate;
        private string logFileName;
        private string summaryLogFileName;
        private string initialWeatherPath;


        //---------------------------------------------------------------------

        /// <summary>
        /// Timestep (years)
        /// </summary>
        public int Timestep
        {
            get {
                return timestep;
            }
            set {
                    if (value < 0)
                        throw new InputValueException(value.ToString(),
                                                      "Value must be = or > 0.");
                timestep = value;
            }
        }
        //---------------------------------------------------------------------
        public SizeType FireSizeType
        {
            get {
                return fireSizeType;
            }
            set {
                fireSizeType = value;
            }
        }
        //---------------------------------------------------------------------
        
        public bool BUI
        {
            get {
                return buildUpIndex;
            }
            set {
                buildUpIndex = value;
            }
        }

        //---------------------------------------------------------------------
        /*
        public int WeatherRandomizer
        {
            get {
                return weatherRandomizer;
            }
        }*/
        //---------------------------------------------------------------------
        
        public double SeverityCalibrate
        {
            get {
                return severityCalibrate;
            }
            set
            {
                severityCalibrate = value;
            }
        }
        
        //---------------------------------------------------------------------
        
        public List<IDynamicFireRegion> DynamicFireRegions
        {
            get {
                return dynamicFireRegions;
            }
        }
        //---------------------------------------------------------------------
        public List<IDynamicWeather> DynamicWeather
        {
            get
            {
                return dynamicWeather;
            }
        }
        //---------------------------------------------------------------------

        public ISeasonParameters[] SeasonParameters
        {
            get {
                return seasons;
            }
            set {
                seasons = value;
            }
         
        }
        //---------------------------------------------------------------------
        public IFuelType[] FuelTypeParameters
        {
            get {
                return fuelTypeParameters;
            }
        }

        //---------------------------------------------------------------------
        public List<IFireDamage> FireDamages
        {
            get {
                return damages;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Template for the filenames for output maps.
        /// </summary>
        public string MapNamesTemplate
        {
            get {
                return mapNamesTemplate;
            }
            set {
                    MapNames.CheckTemplateVars(value);
                mapNamesTemplate = value;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Name of log file.
        /// </summary>
        public string LogFileName
        {
            get {
                return logFileName;
            }
            set {
                    // FIXME: check for null or empty path (value);
                logFileName = value;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Name of log file.
        /// </summary>
        public string InitialWeatherPath
        {
            get {
                return initialWeatherPath;
            }
            set {
                    // FIXME: check for null or empty path (value);
                initialWeatherPath = value;
            }
        }
        /// <summary>
        /// Name of log file.
        /// </summary>
        public string SummaryLogFileName
        {
            get {
                return summaryLogFileName;
            }
            set {
                    // FIXME: check for null or empty path (value);
                summaryLogFileName = value;
            }
        }
        //---------------------------------------------------------------------

        public InputParameters()
        {
            seasons = new SeasonParameters[3];
            damages = new List<IFireDamage>();
            dynamicFireRegions = new List<IDynamicFireRegion>();
            dynamicWeather = new List<IDynamicWeather>();
            
            fuelTypeParameters = new FuelType[100];
            for(int i=0; i<100; i++)
                fuelTypeParameters[i] = new FuelType();
        }
        //---------------------------------------------------------------------
/*
        public Parameters(int               timestep,
                          SizeType          fireSizeType,
                          bool              buildUpIndex,
                          //int               weatherRandomizer,
                          double            severityCalibrate,
                          IDynamicFireRegion[]      dynamicFireRegions,
                          IDynamicWeather[]    dynamicWeather,
                          ISeasonParameters[]  seasonParameters,
                          IFuelType[]  fuelTypeParameters,
                          IFireDamage[]    damages,
                          string            mapNameTemplate,
                          string            logFileName,
                          string            summaryLogFileName,
                          string            initialWeatherPath
                          )
        {
            this.timestep = timestep;
            this.fireSizeType = fireSizeType;
            this.buildUpIndex = buildUpIndex;
            //this.weatherRandomizer = weatherRandomizer;
            this.severityCalibrate = severityCalibrate;
            this.dynamicFireRegions = dynamicFireRegions;
            this.dynamicWeather = dynamicWeather;
            this.seasonParameters = seasonParameters;
            this.fuelTypeParameters = fuelTypeParameters;
            this.damages = damages;
            this.mapNamesTemplate = mapNameTemplate;
            this.logFileName = logFileName;
            this.summaryLogFileName = summaryLogFileName;
            this.initialWeatherPath = initialWeatherPath;
        }*/
    }
}
