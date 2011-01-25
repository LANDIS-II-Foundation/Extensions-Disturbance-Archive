using Edu.Wisc.Forest.Flel.Util;
using Landis.Species;
using Landis.Succession;

using System.Collections.Generic;
using System.Text;

using FormatException = System.FormatException;

namespace Landis.Harvest
{
    /// <summary>
    /// A parser that reads harvest parameters from text input.
    /// </summary>
    public class ParametersParser
        : Landis.TextParser<IParameters>
    {
        private static ParseMethod<ushort> uShortParse;

        private Species.IDataset speciesDataset;
        private InputVar<string> speciesName;
        private Dictionary<string, int> speciesLineNumbers;
        private int startTime;
        private int endTime;
        private List<RoundedInterval> roundedIntervals;

        private static class Names
        {
            public const string HarvestImplementations = "HarvestImplementations";
            public const string MaximumAge = "MaximumAge";
            public const string MinimumAge = "MinimumAge";
            public const string MultipleRepeat = "MultipleRepeat";
            public const string Plant = "Plant";
            public const string Prescription = "Prescription";
            public const string PrescriptionMaps = "PrescriptionMaps";
            public const string SingleRepeat = "SingleRepeat";
            public const string SiteSelection = "SiteSelection";
        }

        //---------------------------------------------------------------------

        public override string LandisDataValue
        {
            get {
                return "Harvesting";
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// A list of the repeat-harvest intervals that were rounded up during
        /// the most recent call to the Parse method.
        /// </summary>
        public List<RoundedInterval> RoundedRepeatIntervals
        {
            get {
                return roundedIntervals;
            }
        }

        //---------------------------------------------------------------------

        static ParametersParser()
        {
			// FIXME: Hack to ensure that Percentage is registered with InputValues
			Edu.Wisc.Forest.Flel.Util.Percentage p = new Edu.Wisc.Forest.Flel.Util.Percentage();

			//  Register the local method for parsing a cohort age or age range.
			InputValues.Register<AgeRange>(ParseAgeOrRange);
			Type.SetDescription<AgeRange>("cohort age or age range");
			uShortParse = InputValues.GetParseMethod<ushort>();
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Parses a word for a cohort age or an age range (format: age-age).
        /// </summary>
        public static AgeRange ParseAgeOrRange(string word)
        {
            int delimiterIndex = word.IndexOf('-');
            if (delimiterIndex == -1) {
                ushort age = ParseAge(word);
                if (age == 0)
                    throw new FormatException("Cohort age must be > 0");
                return new AgeRange(age, age);
            }

            string startAge = word.Substring(0, delimiterIndex);
            string endAge = word.Substring(delimiterIndex + 1);
            if (endAge.Contains("-"))
                throw new FormatException("Valid format for age range: #-#");
            if (startAge == "") {
                if (endAge == "")
                    throw new FormatException("The range has no start and end ages");
                else
                    throw new FormatException("The range has no start age");
            }
            ushort start = ParseAge(startAge);
            if (start == 0)
                throw new FormatException("The start age in the range must be > 0");
            if (endAge == "")
                    throw new FormatException("The range has no end age");
            ushort end = ParseAge(endAge);
            if (start > end)
                throw new FormatException("The start age in the range must be <= the end age");
            return new AgeRange(start, end);
        }

        //---------------------------------------------------------------------

        public static ushort ParseAge(string text)
        {
            try {
                return uShortParse(text);
            }
            catch (System.OverflowException) {
                throw new FormatException(text + " is too large for an age; max = 65,535");
            }
            catch (System.Exception) {
                throw new FormatException(text + " is not a valid integer");
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="speciesDataset">
        /// The dataset of species to look up species' names in.
        /// </param>
        /// <param name="startTime">
        /// The year that the model scenario starts.
        /// </param>
        /// <param name="endTime">
        /// The year that the model scenario ends.
        /// </param>
        public ParametersParser(Species.IDataset speciesDataset,
                                int              startTime,
                                int              endTime)
        {
            this.speciesDataset = speciesDataset;
            this.speciesName = new InputVar<string>("Species");
            this.speciesLineNumbers = new Dictionary<string, int>();
            this.startTime = startTime;
            this.endTime = endTime;
            this.roundedIntervals = new List<RoundedInterval>();
        }

        //---------------------------------------------------------------------

        protected override IParameters Parse()
        {
            roundedIntervals.Clear();
            ReadLandisDataVar();

            EditableParameters parameters = new EditableParameters();

            InputVar<int> timestep = new InputVar<int>("Timestep");
            ReadVar(timestep);
            parameters.Timestep = timestep.Value;

            InputVar<string> mgmtAreaMap = new InputVar<string>("ManagementAreas");
            ReadVar(mgmtAreaMap);
            parameters.ManagementAreaMap = mgmtAreaMap.Value;

            InputVar<string> standMap = new InputVar<string>("Stands");
            ReadVar(standMap);
            parameters.StandMap = standMap.Value;

            ReadPrescriptions(parameters.Prescriptions, timestep.Value.Actual);

            ReadHarvestImplementations(parameters.ManagementAreas,
                                       parameters.Prescriptions);

            //  Output file parameters

            InputVar<string> prescriptionMapNames = new InputVar<string>(Names.PrescriptionMaps);
            ReadVar(prescriptionMapNames);
            parameters.PrescriptionMapNames = prescriptionMapNames.Value;

            InputVar<string> eventLogFile = new InputVar<string>("EventLog");
            ReadVar(eventLogFile);
            parameters.EventLog = eventLogFile.Value;

            CheckNoDataAfter("the " + eventLogFile.Name + " parameter");
            return parameters.GetComplete();
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Reads 0 or more prescriptions from text input.
        /// </summary>
        protected void ReadPrescriptions(List<Prescription> prescriptions,
                                         int                harvestTimestep)
        {
            Dictionary<string, int> lineNumbers = new Dictionary<string, int>();

            InputVar<int> singleRepeat = new InputVar<int>(Names.SingleRepeat);
            InputVar<int> multipleRepeat = new InputVar<int>(Names.MultipleRepeat);

            int nameLineNumber = LineNumber;
            InputVar<string> prescriptionName = new InputVar<string>(Names.Prescription);
            while (ReadOptionalVar(prescriptionName)) {
                string name = prescriptionName.Value.Actual;
                int lineNumber;
                if (lineNumbers.TryGetValue(name, out lineNumber))
                    throw new InputValueException(prescriptionName.Value.String,
                                                  "The name {0} was previously used on line {1}",
                                                  prescriptionName.Value.String, lineNumber);
                else
                    lineNumbers[name] = nameLineNumber;

                IStandRankingMethod rankingMethod = ReadRankingMethod();
                ISiteSelector siteSelector = ReadSiteSelector();
                ICohortSelector cohortSelector = ReadCohortSelector(false);
                Planting.SpeciesList speciesToPlant = ReadSpeciesToPlant();

                //  Repeat harvest?
                int repeatParamLineNumber = LineNumber;
                if (ReadOptionalVar(singleRepeat)) {
                    int interval = ValidateRepeatInterval(singleRepeat.Value,
                                                          repeatParamLineNumber,
                                                          harvestTimestep);
                    ICohortSelector additionalCohortSelector = ReadCohortSelector(true);
                    Planting.SpeciesList additionalSpeciesToPlant = ReadSpeciesToPlant();
                    prescriptions.Add(new SingleRepeatHarvest(name,
                                                              rankingMethod,
                                                              siteSelector,
                                                              cohortSelector,
                                                              speciesToPlant,
                                                              additionalCohortSelector,
                                                              additionalSpeciesToPlant,
                                                              interval));
                }
                else if (ReadOptionalVar(multipleRepeat)) {
                    int interval = ValidateRepeatInterval(multipleRepeat.Value,
                                                          repeatParamLineNumber,
                                                          harvestTimestep);
                    prescriptions.Add(new RepeatHarvest(name,
                                                        rankingMethod,
                                                        siteSelector,
                                                        cohortSelector,
                                                        speciesToPlant,
                                                        interval));
                }
                else {
                    prescriptions.Add(new Prescription(name,
                                                       rankingMethod,
                                                       siteSelector,
                                                       cohortSelector,
                                                       speciesToPlant));
                }
            }
        }

        //---------------------------------------------------------------------

        protected IStandRankingMethod ReadRankingMethod()
        {
            InputVar<string> rankingName = new InputVar<string>("StandRanking");
            ReadVar(rankingName);

            IStandRankingMethod rankingMethod;

            if (rankingName.Value.Actual == "Economic")
                rankingMethod = new EconomicRank(ReadEconomicRankTable());

            else if (rankingName.Value.Actual == "MaxCohortAge")
                rankingMethod = new MaxCohortAge();

            else if ((rankingName.Value.Actual == "Random") ||
                     (rankingName.Value.Actual == "RegulateAges") ||
                     (rankingName.Value.Actual == "SpeciesBiomass") ||
                     (rankingName.Value.Actual == "TotalBiomass")) {
                throw new InputValueException(rankingName.Value.String,
                                              rankingName.Value.String + " is not implemented yet");
            }

            else {
                string[] methodList = new string[]{"Stand ranking methods:",
                                                   "  Economic",
                                                   "  MaxCohortAge",
                                                   "  Random",
                                                   "  RegulateAges",
                                                   "  SpeciesBiomass",
                                                   "  TotalBiomass"};
                throw new InputValueException(rankingName.Value.String,
                                              rankingName.Value.String + " is not a valid stand ranking",
                                              new MultiLineText(methodList));
            }

            //  Read optional ranking requirements

            ushort? minAge = null;
            InputVar<ushort> minimumAge = new InputVar<ushort>("MinimumAge");
            if (ReadOptionalVar(minimumAge)) {
                minAge = minimumAge.Value.Actual;
                rankingMethod.AddRequirement(new MinimumAge(minAge.Value));
            }

            InputVar<ushort> maximumAge = new InputVar<ushort>("MaximumAge");
            if (ReadOptionalVar(maximumAge)) {
                ushort maxAge = maximumAge.Value.Actual;
                if (minAge.HasValue && maxAge < minAge)
                    throw new InputValueException(maximumAge.Value.String,
                                                  "{0} is < minimum age ({1})",
                                                  maximumAge.Value.String,
                                                  minimumAge.Value.String);
                rankingMethod.AddRequirement(new MaximumAge(maxAge));
            }

            return rankingMethod;
        }

        //---------------------------------------------------------------------

        private static List<string> namesThatFollowRankingMethod = new List<string>(
            new string[]{
                Names.SiteSelection,

                //  Optional ranking requirements
                Names.MaximumAge,
                Names.MinimumAge
            }
        );

        //---------------------------------------------------------------------

        protected EconomicRankTable ReadEconomicRankTable()
        {
            speciesLineNumbers.Clear();  // in case parser re-used

            InputVar<byte> rank = new InputVar<byte>("Economic Rank");
            InputVar<ushort> minAge = new InputVar<ushort>("Minimum Age");
            string lastColumn = "the " + minAge.Name + " column";

            EconomicRankTable table = new EconomicRankTable(speciesDataset);
            while (! AtEndOfInput && ! namesThatFollowRankingMethod.Contains(CurrentName)) {
                StringReader currentLine = new StringReader(CurrentLine);

                //  Species name
                Species.ISpecies species = ReadSpecies(currentLine);

                //  Economic rank
                ReadValue(rank, currentLine);
                const byte maxRank = 100;
                if (rank.Value.Actual > maxRank)
                    throw new InputValueException(rank.Value.String,
                                                  "Economic rank must be between 0 and {0}",
                                                  maxRank);

                //  Minimum age
                ReadValue(minAge, currentLine);
                CheckNoDataAfter(lastColumn, currentLine);

                table[species] = new EconomicRankParameters(rank.Value.Actual,
                                                            minAge.Value.Actual);
                GetNextLine();
            }

            if (speciesLineNumbers.Count == 0)
                throw NewParseException("Expected a line starting with a species name");

            return table;
        }

        //---------------------------------------------------------------------

        protected ISpecies ReadSpecies(StringReader currentLine)
        {
            Species.ISpecies species = ReadAndValidateSpeciesName(currentLine);
            int lineNumber;
            if (speciesLineNumbers.TryGetValue(species.Name, out lineNumber))
                throw new InputValueException(speciesName.Value.String,
                                              "The species {0} was previously used on line {1}",
                                              speciesName.Value.String, lineNumber);
            else
                speciesLineNumbers[species.Name] = LineNumber;

            return species;
        }

        //---------------------------------------------------------------------

        protected ISpecies ReadAndValidateSpeciesName(StringReader currentLine)
        {
            ReadValue(speciesName, currentLine);
            Species.ISpecies species = speciesDataset[speciesName.Value.Actual];
            if (species == null)
                throw new InputValueException(speciesName.Value.String,
                                              "{0} is not a species name",
                                              speciesName.Value.String);
            return species;
        }

        //---------------------------------------------------------------------

        protected ISiteSelector ReadSiteSelector()
        {
            InputVar<ISiteSelector> siteSelector = new InputVar<ISiteSelector>(Names.SiteSelection, ReadSiteSelector);
            ReadVar(siteSelector);
            return siteSelector.Value.Actual;
        }

        //---------------------------------------------------------------------

        private static class SiteSelection
        {
            public const string Complete = "Complete";
            public const string TargetSize = "TargetSize";
            public const string CompleteAndSpreading = "Complete&Spread";
            public const string Patch = "Patch";
        }

        //---------------------------------------------------------------------

        protected InputValue<ISiteSelector> ReadSiteSelector(StringReader reader,
                                                             out int      index)
        {
            TextReader.SkipWhitespace(reader);
            index = reader.Index;
            string name = TextReader.ReadWord(reader);
            if (name == "")
                throw new InputValueException();  // Missing value

            ISiteSelector selector;
            StringBuilder valueAsStr = new StringBuilder(name);

            //  Site selection -- Complete stand
            if (name == SiteSelection.Complete) {
                selector = new CompleteStand();
            }

            //  Site selection -- Target size with partial or complete spread
            else if (name == SiteSelection.TargetSize || name == SiteSelection.CompleteAndSpreading) {
                InputVar<double> targetSize = new InputVar<double>("the target harvest size");
                ReadValue(targetSize, reader);
                StandSpreading.ValidateTargetSize(targetSize.Value);

                if (name == SiteSelection.TargetSize)
                    // TODO: selector = new PartialStandSpread(targetSize.Value.Actual);
                    throw new InputValueException(name,
                                                  name + " is not implemented yet");
                else
                    selector = new CompleteStandSpreading(targetSize.Value.Actual);
                valueAsStr.AppendFormat(" {0}", targetSize.Value.String);
            }

            //  Site selection -- Patch cutting
            else if (name == SiteSelection.Patch) {
                InputVar<Percentage> percentage = new InputVar<Percentage>("the site percentage for patch cutting");
                ReadValue(percentage, reader);
                PatchCutting.ValidatePercentage(percentage.Value);

                InputVar<double> size = new InputVar<double>("the target patch size");
                ReadValue(size, reader);
                PatchCutting.ValidateSize(size.Value);

                // TODO: selector = new PatchCutting(percentage.Value.Actual, size.Value.Actual);
                valueAsStr.AppendFormat(" {0} {1}", percentage.Value.String,
                                                    size.Value.String);
                throw new InputValueException(name,
                                              name + " is not implemented yet");
            }
            else {
                string[] methodList = new string[]{"Site selection methods:",
                                                   "  " + SiteSelection.Complete,
                                                   "  " + SiteSelection.CompleteAndSpreading,
                                                   "  " + SiteSelection.Patch,
                                                   "  " + SiteSelection.TargetSize};
                throw new InputValueException(name,
                                              name + " is not a valid site selection method",
                                              new MultiLineText(methodList));
            }

            return new InputValue<ISiteSelector>(selector, valueAsStr.ToString());
        }

        //---------------------------------------------------------------------

        protected ICohortSelector ReadCohortSelector(bool forSingleRepeat)
        {
            InputVar<string> cohortSelection = new InputVar<string>("CohortsRemoved");
            ReadVar(cohortSelection);

            if (cohortSelection.Value.Actual == "ClearCut")
                return new ClearCut();

            if (cohortSelection.Value.Actual == "SpeciesList") {
                if (forSingleRepeat)
                    return ReadSpeciesAndCohorts(Names.Plant,
                                                 Names.Prescription,
                                                 Names.HarvestImplementations);
                else
                    return ReadSpeciesAndCohorts(Names.Plant,
                                                 Names.SingleRepeat,
                                                 Names.MultipleRepeat,
                                                 Names.Prescription,
                                                 Names.HarvestImplementations);
            }

            throw new InputValueException(cohortSelection.Value.String,
                                          cohortSelection.Value.String + " is not a valid cohort selection",
                                          new MultiLineText("Valid values: ClearCut or SpeciesList"));
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Reads a list of species and their cohorts that should be removed.
        /// </summary>
        protected ICohortSelector ReadSpeciesAndCohorts(params string[] names)
        {
            List<string> namesThatFollow;
            if (names == null)
                namesThatFollow = new List<string>();
            else
                namesThatFollow = new List<string>(names);

            MultiSpeciesCohortSelector cohortSelector = new MultiSpeciesCohortSelector();
            speciesLineNumbers.Clear();

            while (! AtEndOfInput && ! namesThatFollow.Contains(CurrentName)) {
                StringReader currentLine = new StringReader(CurrentLine);

                // Species name
                Species.ISpecies species = ReadSpecies(currentLine);

                //  Cohort keyword, cohort age or cohort age range
                //  keyword = (All, Youngest, AllExceptYoungest, Oldest,
                //             AllExceptOldest, 1/{N})
                TextReader.SkipWhitespace(currentLine);
                int indexOfDataAfterSpecies = currentLine.Index;
                string word = TextReader.ReadWord(currentLine);
                if (word == "")
                    throw NewParseException("No cohort keyword, age or age range after the species name");

                bool isKeyword = false;
                if (word == "All") {
                    cohortSelector[species] = SelectCohorts.All;
                    isKeyword = true;
                }
                else if (word == "Youngest") {
                    cohortSelector[species] = SelectCohorts.Youngest;
                    isKeyword = true;
                }
                else if (word == "AllExceptYoungest") {
                    cohortSelector[species] = SelectCohorts.AllExceptYoungest;
                    isKeyword = true;
                }
                else if (word == "Oldest") {
                    cohortSelector[species] = SelectCohorts.Oldest;
                    isKeyword = true;
                }
                else if (word == "AllExceptOldest") {
                    cohortSelector[species] = SelectCohorts.AllExceptOldest;
                    isKeyword = true;
                }
                else if (word.StartsWith("1/")) {
                    InputVar<ushort> N = new InputVar<ushort>("1/N");
                    N.ReadValue(new StringReader(word.Substring(2)));
                    if (N.Value.Actual == 0)
                        throw NewParseException("For \"1/N\", N must be > 0");
                    cohortSelector[species] = new EveryNthCohort(N.Value.Actual).SelectCohorts;
                    isKeyword = true;
                }

                if (isKeyword)
                    CheckNoDataAfter("the keyword \"" + word + "\"", currentLine);
                else {
                    //  Read one or more ages or age ranges
                    List<ushort> ages = new List<ushort>();
                    List<AgeRange> ranges = new List<AgeRange>();
                    currentLine = new StringReader(CurrentLine.Substring(indexOfDataAfterSpecies));
                    InputVar<AgeRange> ageOrRange = new InputVar<AgeRange>("Age or Age Range");
                    while (currentLine.Peek() != -1) {
                        ReadValue(ageOrRange, currentLine);
                        ValidateAgeOrRange(ageOrRange.Value, ages, ranges);
                        TextReader.SkipWhitespace(currentLine);
                    }
                    cohortSelector[species] = new SpecificAgesCohortSelector(ages, ranges).SelectCohorts;
                }

                GetNextLine();
            }

            if (speciesLineNumbers.Count == 0)
                throw NewParseException("Expected a line starting with a species name");

            return cohortSelector;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Validates a cohort age or age range against previous ages and
        /// ranges.
        /// </summary>
        /// <param name="ageOrRange">
        /// The age or age range that's being validated.
        /// </param>
        /// <param name="ages">
        /// List of previous ages.
        /// </param>
        /// <param name="ranges">
        /// List of previous ranges.
        /// </param>
        /// <remarks>
        /// If the age or range is validated, it is added to the corresponding
        /// list.
        /// </remarks>
        protected void ValidateAgeOrRange(InputValue<AgeRange> ageOrRange,
                                          List<ushort>         ages,
                                          List<AgeRange>       ranges)
        {
            if (ageOrRange.String.Contains("-")) {
                AgeRange range = ageOrRange.Actual;

                //  Does the range contain any individual ages?
                foreach (ushort age in ages) {
                    if (range.Contains(age))
                        throw new InputValueException(ageOrRange.String,
                                                      "The range {0} contains the age {1}",
                                                      ageOrRange.String, age);
                }

                //  Does the range overlap any previous ranges?
                foreach (AgeRange previousRange in ranges) {
                    if (range.Overlaps(previousRange))
                        throw new InputValueException(ageOrRange.String,
                                                      "The range {0} overlaps the range {1}-{2}",
                                                      ageOrRange.String, previousRange.Start, previousRange.End);
                }

                ranges.Add(range);
            }
            else {
                ushort age = ageOrRange.Actual.Start;

                //  Does the age match any of the previous ages?
                foreach (ushort previousAge in ages) {
                    if (age == previousAge)
                        throw new InputValueException(ageOrRange.String,
                                                      "The age {0} appears more than once",
                                                      ageOrRange.String);
                }

                //  Is the age in any of the previous ranges?
                foreach (AgeRange previousRange in ranges) {
                    if (previousRange.Contains(age))
                        throw new InputValueException(ageOrRange.String,
                                                      "The age {0} lies within the range {1}-{2}",
                                                      ageOrRange.String, previousRange.Start, previousRange.End);
                }

                ages.Add(age);
            }
        }

        //---------------------------------------------------------------------

        protected Planting.SpeciesList ReadSpeciesToPlant()
        {
            InputVar<List<ISpecies>> plant = new InputVar<List<ISpecies>>("Plant", ReadSpeciesList);
            if (ReadOptionalVar(plant))
                return new Planting.SpeciesList(plant.Value.Actual, speciesDataset);
            else
                return null;
        }

        //---------------------------------------------------------------------

        public InputValue<List<ISpecies>> ReadSpeciesList(StringReader currentLine,
                                                          out int      index)
        {
			List<string> speciesNames = new List<string>();
			List<ISpecies> speciesList = new List<ISpecies>();

			TextReader.SkipWhitespace(currentLine);
			index = currentLine.Index;
			while (currentLine.Peek() != -1) {
				ISpecies species = ReadAndValidateSpeciesName(currentLine);
				if (speciesNames.Contains(species.Name))
					throw new InputValueException(speciesName.Value.String,
				                                  "The species {0} appears more than once.", species.Name);
				speciesNames.Add(species.Name);
				speciesList.Add(species);

				TextReader.SkipWhitespace(currentLine);
			}
			if (speciesNames.Count == 0)
				throw new InputValueException(); // Missing value

			return new InputValue<List<ISpecies>>(speciesList,
			                                      string.Join(" ", speciesNames.ToArray()));
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Validates the interval for a repeat harvest.
        /// </summary>
        /// <param name="interval">
        /// The interval to validate.
        /// </param>
        /// <param name="lineNumber">
        /// The line number where the interval was located in the text input.
        /// </param>
        /// <param name="harvestTimestep">
        /// The timestep of the harvest plug-in.
        /// </param>
        /// <returns>
        /// If the interval is not a multiple of the harvest timestep, then
        /// the method rounds the interval up to the next multiple and returns
        /// it.
        /// </returns>
        public int ValidateRepeatInterval(InputValue<int> interval,
                                          int             lineNumber,
                                          int             harvestTimestep)
        {
            if (interval.Actual <= 0)
                throw new InputValueException(interval.String,
                                              "Interval for repeat harvest must be > 0");

            if (interval.Actual % harvestTimestep == 0)
                return interval.Actual;
            else {
                int intervalRoundedUp = ((interval.Actual / harvestTimestep) + 1) * harvestTimestep;
                roundedIntervals.Add(new RoundedInterval(interval.Actual,
                                                         intervalRoundedUp,
                                                         lineNumber));
                return intervalRoundedUp;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Reads harvest implementations: which prescriptions are applied to
        /// which management areas.
        /// </summary>
        protected void ReadHarvestImplementations(ManagementAreaDataset mgmtAreas,
                                                  List<Prescription>    prescriptions)
        {
            ReadName(Names.HarvestImplementations);

            InputVar<ushort> mgmtAreaMapCode = new InputVar<ushort>("Mgmt Area");
            InputVar<string> prescriptionName = new InputVar<string>("Prescription");
            InputVar<Percentage> areaToHarvest = new InputVar<Percentage>("Area To Harvest");
            while (! AtEndOfInput && CurrentName != Names.PrescriptionMaps) {
                StringReader currentLine = new StringReader(CurrentLine);

                //  Mgmt Area column
                ReadValue(mgmtAreaMapCode, currentLine);
                ushort mapCode = mgmtAreaMapCode.Value.Actual;
                ManagementArea mgmtArea = mgmtAreas.Find(mapCode);
                if (mgmtArea == null) {
                    mgmtArea = new ManagementArea(mapCode);
                    mgmtAreas.Add(mgmtArea);
                }

                //  Prescription column
                ReadValue(prescriptionName, currentLine);
                string name = prescriptionName.Value.Actual;
                Prescription prescription = prescriptions.Find(new MatchName(name).Predicate);
                if (prescription == null)
                    throw new InputValueException(prescriptionName.Value.String,
                                                  prescriptionName.Value.String + " is an unknown prescription name");
                if (mgmtArea.IsApplied(prescription))
                    throw new InputValueException(prescriptionName.Value.String,
                                                  "Prescription {0} has already been applied to management area {1}",
                                                  prescriptionName.Value.String, mgmtArea.MapCode);

                //  Area to Harvest column
                ReadValue(areaToHarvest, currentLine);
                Percentage percentageToHarvest = areaToHarvest.Value.Actual;
                if (percentageToHarvest < 0 || percentageToHarvest > 1.0)
                    throw new InputValueException(areaToHarvest.Value.String,
                                                  "Percentage must be between 0% and 100%");

                mgmtArea.ApplyPrescription(prescription,
                                           percentageToHarvest,
                                           startTime,
                                           endTime);

                CheckNoDataAfter("the " + prescriptionName.Name + " column",
                                 currentLine);
                GetNextLine();
            }
        }

        //---------------------------------------------------------------------

        public class MatchName
        {
            private string name;

            //- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

            public MatchName(string name)
            {
                this.name = name;
            }

            //- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

            public bool Predicate(Prescription prescription)
            {
                return prescription.Name == name;
            }
        }
    }
}
