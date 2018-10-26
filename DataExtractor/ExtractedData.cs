using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DataExtractor
{
    // The class containing the extracted data, as well as the methods associated with data extraction
    public class ExtractedData
    {
        // Constructor
        // The constructor will first construct a List of file recores, sort the records, figure out what files are needed,
        // then call ExtractData to get data
        public ExtractedData(DateTime startDateTime, DateTime endDateTime, string[] tags, string[] dataFiles, int interval = 1)
        {
            Tags = new string[tags.Length];
            tags.CopyTo(Tags, 0);
            // Construct an array of the file records
            List<FileRecord> fileRecords = new List<FileRecord>();
            int i;
            foreach (string fileName in dataFiles)
            {
                FileRecord record = new FileRecord(fileName);
                if (record.fileName != String.Empty)
                    fileRecords.Add(record);
            }
            fileRecords.Sort();
            if (fileRecords.Count == 0)
                throw new ArgumentException("Invalid data file list");
            // If the start time is later than the end time, swap them
            if (startDateTime > endDateTime)
            {
                DateTime temp = startDateTime;
                startDateTime = endDateTime;
                endDateTime = startDateTime;
            }
            // Determine what files are needed
            // If request time ends before first file, no data is available
            if (endDateTime < fileRecords[0].startTime)
                throw new ArgumentException("Requested Date and Time Not Available in Selected Data Files");
            else if (fileRecords.Count > 1)
            {
                // when there's more than one file, check their start time
                // only files needed will be left in the fileRecords
                for (i = 0; i < fileRecords.Count - 1;)
                {
                    // if next file start before start datetime
                    if (fileRecords[i + 1].startTime <= startDateTime)
                        fileRecords.RemoveAt(i);
                    else
                    {
                        // if next file start after start DateTime. This file is needed.
                        if (fileRecords[i + 1].startTime > endDateTime)
                        {
                            // next file start after endDateTime. Later files will not be needed
                            fileRecords.RemoveRange(i + 1, fileRecords.Count - i - 1);
                            break;
                        }
                        i++;
                    }
                }
            }
            // Get data from the listed files
            Extract(startDateTime, endDateTime, Tags, fileRecords, interval);
        }

        // data in the class
        public List<float[]> RawData { get; set; }
        public DateTime[] DateTimes { get; set; }
        public string[] Tags { get; set; }
        public int pointCount;

        // Try to parse the date input from user into a DateTime struct
        public static DateTime ParseDate(string dateStr = "")
        {
            //If the input contains things like 1st, 2nd, 3rd, 4th, remove the st / nd / rd / th and replace by space
            // (?<=) is a "Zero-width positive lookbehind assertion".
            // The regex assert that a digit (and maybe a whitespace) preceeds the st/nd/rd/th
            dateStr = Regex.Replace(dateStr, @"(?<=\d\s*)(st|nd|rd|th)", " ", RegexOptions.IgnoreCase);
            // If the input contains any letters, assume they are literal months. Add space before and after
            dateStr = Regex.Replace(dateStr, @"([a-zA-Z]+)", @" $1 ", RegexOptions.IgnoreCase);
            //Split the dateStr by white spaces and underline _. Split returns an array. Remove all empty string and get an IList object
            // Except is a method of IEnumerable
            IList<string> dateList = Regex.Split(dateStr, @"[\W_]+").Where(s => s != "").ToList();

            int year, month, day;
            switch (dateList.Count())
            {
                case 1: // There's only one element in dateIE. dateStr should be a pure digit string
                    dateStr = dateList[0];
                    int dateInt;
                    if (Int32.TryParse(dateStr, out dateInt))
                    {
                        switch (dateStr.Length)
                        {
                            case 2: // only two digits. Treat it as MD
                                year = DateTime.Now.Year;
                                month = dateInt / 10;
                                day = dateInt % 10;
                                break;
                            case 3:
                            case 4:
                                // The date format is MMDD. Will use current year
                                year = DateTime.Now.Year;
                                month = dateInt / 100;
                                day = dateInt % 100;
                                break;

                            case 5: //MDDYY format
                                year = dateInt % 100;
                                month = dateInt / 10000;
                                day = (dateInt / 100) % 100;
                                break;
                            case 6: //YYMMDD or MMDDYY format
                                //default is YYMMDD, unless last two digits are greater than31 or middle two greater than 12
                                year = dateInt / 10000 + 2000;
                                month = (dateInt / 100) % 100;
                                day = dateInt % 100;
                                if (month > 12 || day > 31 || day == 0) //MMDDYY format
                                {
                                    year = dateInt % 100 + 2000;
                                    month = dateInt / 10000;
                                    day = (dateInt / 100) % 100;
                                    if (month > 12 || day > 31 || day == 0) //DDMMYY format
                                    {
                                        month = (dateInt / 100) % 100;
                                        day = dateInt / 10000;
                                    }
                                }
                                break;
                            case 7: //MDDYYYY or DMMYYYY format
                                year = dateInt % 10000;
                                month = dateInt / 1000000;
                                day = (dateInt / 10000) % 100;
                                if (month > 12 || day > 31 || day == 0) //DDMMYY format
                                {
                                    month = (dateInt / 10000) % 100;
                                    day = dateInt / 1000000;
                                }
                                break;
                            case 8: // YYYYMMDD or MMDDYYYY or DDMMYYYY format
                                // default is YYYYMMDD
                                year = dateInt / 10000;
                                month = (dateInt / 100) % 100;
                                day = dateInt % 100;
                                if (month > 12 || day > 31 || day == 0) //MMDDYY format
                                {
                                    year = dateInt % 10000;
                                    month = dateInt / 1000000;
                                    day = (dateInt / 10000) % 100;
                                    if (month > 12 || day > 31 || day == 0) //DDMMYY format
                                    {
                                        month = (dateInt / 10000) % 100;
                                        day = dateInt / 1000000;
                                    }
                                }
                                break;
                            default: // anyother cases should generate an exception
                                throw new FormatException("Invalid Date");
                        }
                        break;
                    }
                    else
                    {
                        throw new FormatException("Invalid Date");
                    }

                case 2: //Only month and day is given. Assume current year
                    year = DateTime.Now.Year;
                    if (dateList[0].All(Char.IsLetter)) // MMMDD format
                    {
                        Dictionary<string, int> monthDict = new Dictionary<string, int>
                        {
                            {"JAN", 1 }, {"JANUARY", 1 }, {"FEB", 2 }, {"FEBRUARY", 2 }, {"MAR", 3 }, {"MARCH", 3 },
                            {"APR", 4 }, {"APRIL", 4 }, {"MAY", 5 }, {"JUN", 6 }, {"JUNE", 6 }, {"JUL", 7 },
                            {"JULY", 7 }, {"AUG", 8 }, {"AUGUST", 8 }, {"SEP", 9}, {"SEPT", 9 }, {"SEPTEMBER", 9 },
                            {"OCT", 10 }, {"OCTOBER", 10 }, {"NOV", 11 }, {"NOVEMBER", 11 }, {"DEC", 12 }, {"DECEMBER", 12 }
                        };
                        try
                        {
                            month = monthDict[dateList[0].ToUpper()];
                            day = Int32.Parse(dateList[1]);
                        }
                        catch
                        {
                            throw new FormatException("Invalid Date");
                        }
                    }
                    else if (dateList[1].All(Char.IsLetter)) // DDMMM format
                    {
                        Dictionary<string, int> monthDict = new Dictionary<string, int>
                        {
                            {"JAN", 1 }, {"JANUARY", 1 }, {"FEB", 2 }, {"FEBRUARY", 2 }, {"MAR", 3 }, {"MARCH", 3 },
                            {"APR", 4 }, {"APRIL", 4 }, {"MAY", 5 }, {"JUN", 6 }, {"JUNE", 6 }, {"JUL", 7 },
                            {"JULY", 7 }, {"AUG", 8 }, {"AUGUST", 8 }, {"SEP", 9}, {"SEPT", 9 }, {"SEPTEMBER", 9 },
                            {"OCT", 10 }, {"OCTOBER", 10 }, {"NOV", 11 }, {"NOVEMBER", 11 }, {"DEC", 12 }, {"DECEMBER", 12 }
                        };
                        try
                        {
                            month = monthDict[dateList[1].ToUpper()];
                            day = Int32.Parse(dateList[0]);
                        }
                        catch
                        {
                            throw new FormatException("Invalid Date");
                        }
                    }
                    else if (Int32.TryParse(dateList[0], out month) & Int32.TryParse(dateList[1], out day)) // month and day are all digits
                    {
                        if (month > 12)//swap month and date
                        {
                            int temp = day;
                            day = month;
                            month = temp;
                        }
                    }
                    else
                    {
                        throw new FormatException("Invalid Date");
                    } //case 2 complete
                    break;
                case 3: //Regular expression found separators. Year, Month and Day are all given
                    if (dateList[0].All(Char.IsLetter)) // MMMDDYYYY format
                    {
                        Dictionary<string, int> monthDict = new Dictionary<string, int>
                        {
                            {"JAN", 1 }, {"JANUARY", 1 }, {"FEB", 2 }, {"FEBRUARY", 2 }, {"MAR", 3 }, {"MARCH", 3 },
                            {"APR", 4 }, {"APRIL", 4 }, {"MAY", 5 }, {"JUN", 6 }, {"JUNE", 6 }, {"JUL", 7 },
                            {"JULY", 7 }, {"AUG", 8 }, {"AUGUST", 8 }, {"SEP", 9}, {"SEPT", 9 }, {"SEPTEMBER", 9 },
                            {"OCT", 10 }, {"OCTOBER", 10 }, {"NOV", 11 }, {"NOVEMBER", 11 }, {"DEC", 12 }, {"DECEMBER", 12 }
                        };
                        try
                        {
                            month = monthDict[dateList[0].ToUpper()];
                            year = Int32.Parse(dateList[2]);
                            if (year < 100) // MMDDYY format
                                year += 2000;
                            day = Int32.Parse(dateList[1]);
                        }
                        catch
                        {
                            throw new FormatException("Invalid Date");
                        }
                    }
                    else if (dateList[1].All(Char.IsLetter)) // DDMMM format
                    {
                        Dictionary<string, int> monthDict = new Dictionary<string, int>
                        {
                            {"JAN", 1 }, {"JANUARY", 1 }, {"FEB", 2 }, {"FEBRUARY", 2 }, {"MAR", 3 }, {"MARCH", 3 },
                            {"APR", 4 }, {"APRIL", 4 }, {"MAY", 5 }, {"JUN", 6 }, {"JUNE", 6 }, {"JUL", 7 },
                            {"JULY", 7 }, {"AUG", 8 }, {"AUGUST", 8 }, {"SEP", 9}, {"SEPT", 9 }, {"SEPTEMBER", 9 },
                            {"OCT", 10 }, {"OCTOBER", 10 }, {"NOV", 11 }, {"NOVEMBER", 11 }, {"DEC", 12 }, {"DECEMBER", 12 }
                        };
                        try
                        {
                            month = monthDict[dateList[1].ToUpper()];
                            // default is YYYYMMDD format, unless the last part of dateList is greater than 31
                            day = Int32.Parse(dateList[2]);
                            if (dateList[0].Length >= 2 && day <= 31)
                            {
                                year = Int32.Parse(dateList[0]);
                            }
                            else // DDMMYYYY
                            {
                                year = day;
                                day = Int32.Parse(dateList[0]);
                            }
                            if (year < 100) // MMDDYY format
                                year += 2000;
                        }
                        catch
                        {
                            throw new FormatException("Invalid Date");
                        }
                    }
                    else if (Int32.TryParse(dateList[0], out year) & Int32.TryParse(dateList[1], out month)
                        & Int32.TryParse(dateList[2], out day)) // year, month and day are all digits
                    {
                        // by default parsed it in YYYY MM DD format
                        if (day > 31) // MM DD YYYY format or DD MM YYYY format
                        {
                            int temp = year;
                            year = day;
                            if (dateList[2].Length == 2) // MMDDYY format
                                year += 2000;
                            // second default is MM DD YYYY
                            // at this point, the original day is actually year; original year is assigned to temp
                            if (temp <= 12) // treat it as MM DD YYYY
                            {
                                day = month;
                                month = temp;
                            }
                            else // DD MM YYYY
                                day = temp;
                        }
                        else if (month > 12) // MM DD YYYY format
                        {
                            int temp = year;
                            year = day;
                            day = month;
                            month = temp;
                            if (dateList[2].Length == 2) // MMDDYY format
                                year += 2000;
                        }
                        else // check to see if it's YYMMDD
                        {
                            if (dateList[0].Length == 2) // MMDDYY format
                                year += 2000;
                        }

                    }
                    else
                    {
                        throw new FormatException("Invalid Date");
                    } //case 3 complete
                    break;
                default: throw new FormatException("Invalid Date");
            }
            try
            {
                return new DateTime(year, month, day);
            }
            catch
            {
                throw new FormatException("Invalid Date");
            }

        }

        // Parse the time input from user into a TimeSpan struct
        public static TimeSpan ParseTime(string timeStr = "")
        {
            int hour, minute, second;
            // if there is 'p' or 'pm' in the timeStr, we may need to add 12 to Hour
            bool isPM, isAM;
            if (isPM = Regex.IsMatch(timeStr, @"pm*", RegexOptions.IgnoreCase))
            {
                timeStr = Regex.Replace(timeStr, @"pm*", "", RegexOptions.IgnoreCase);
            }
            if (isAM = Regex.IsMatch(timeStr, @"am*", RegexOptions.IgnoreCase))
            {   //if there is 'a' or 'am' in the timeStr, set a flag to deal with 12am
                timeStr = Regex.Replace(timeStr, @"am*", "", RegexOptions.IgnoreCase);
            }
            // remove all letters
            timeStr = Regex.Replace(timeStr, @"[a-zA-Z]+", "", RegexOptions.IgnoreCase);
            // split the str by symbols
            IList<string> timeList = Regex.Split(timeStr, @"[\W_]+").Where(s => s != "").ToList();

            switch (timeList.Count)
            {
                case 1: // No separator, all digits
                    int timeInt;
                    timeStr = timeList[0];
                    if (Int32.TryParse(timeStr, out timeInt)) // If the string is all digit
                    {
                        switch (timeStr.Length)
                        {
                            case 5:
                            case 6:
                                // 5 or 6 digits, contains hour, minute, and second
                                second = timeInt % 100;
                                minute = (timeInt / 100) % 100;
                                hour = timeInt / 10000;
                                break;
                            case 4:
                            case 3:
                                // 3 or 4 digits, only hour and minute
                                second = 0;
                                minute = timeInt % 100;
                                hour = timeInt / 100;
                                break;
                            case 2:
                            case 1:
                                // 1 or 2 digits. Only hour is given
                                second = minute = 0;
                                hour = timeInt;
                                break;
                            default:
                                throw new FormatException("Invalid Time");
                        }
                        break;
                    }
                    else
                        throw new FormatException("Invalid Time");
                case 2: // two items are provided, which are hour and minute
                    if (Int32.TryParse(timeList[0], out hour) & Int32.TryParse(timeList[1], out minute))
                    {

                        second = 0;
                        break;
                    }
                    else
                        throw new FormatException("Invalid Time");
                case 3: // Hour, minute, and second given
                    if (Int32.TryParse(timeList[0], out hour) & Int32.TryParse(timeList[1], out minute)
                        & Int32.TryParse(timeList[2], out second))
                        break;
                    else
                        throw new FormatException("Invalid Time");
                default:
                    throw new FormatException("Invalid Time");
            }
            //Check if the time is valid
            if (hour > 24 || minute > 59 || second > 59 || hour < 0 || minute < 0 || second < 0)
            {
                throw new FormatException("Invalid Time");
            }
            // if it's 0pm, 1pm, ... 11pm, add 12 to the hour number
            if (isPM && hour >= 0 && hour <= 11)
                hour += 12;
            else if (isAM && hour == 12)// if it's 12am, set hour to 0
                hour = 0;
            else if (hour == 24) // if hour is 24, set the time to last second of the day 23:59:59
            {
                hour = 23;
                minute = second = 59;
            }
            return new TimeSpan(hour, minute, second);


        }


        public static bool IsDigit(string value) //determine whether all 
        {
            foreach (char character in value)
            {
                if (character > '9' || character < '0')
                    return false;
            }
            return true;
        }

        // Extract data from all files in fileRecords according to the tagList between startDateTime and endDateTime
        // All files in the fileRecords will be opened
        private void Extract(DateTime startDateTime, DateTime endDateTime, string[] tagList, List<FileRecord> fileRecords, int interval = 1)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            //PointsToPlot = new SeriesCollection();
            // This is a List that contains the data to be returend
            RawData = new List<float[]>(tagList.Length);
            // use a temporary array to store the data obtained from each line
            float[] dataOfOnePoint = new float[tagList.Length];
            
            // A string that represents a line from the csv file. line1 and line2 are the first two lines.
            // Reading the first two lines allows the method to determine the time interval between the two lines, which is used to size the arrays
            string line, titleLine, line1, line2;
            string[] splitTitleLine;
            // delimiter used by the file
            char delimiter;
            // Array of integers corresponding to the position of the tags in a line
            List<IndexWithPosition> indexOfTags = new List<IndexWithPosition>(tagList.Length);

            // A delegate that specifies how to retrive datetime from a line.
            // if the file contain a "date" or ";date" column, the method for translating the datetime is different
            Func<string, char, DateTime> dateTimeFromStr;

            // nColumn is the number of columns in a csv file
            int nColumn;
            // nPoints is the estimated numbe of data points to be extracted, based on the time interval between points
            int nPoints = 0;
            // pointCount is the points extracted. If pointCount get to the length of the array, make the array larger
            pointCount = 0;
            // skipCounter is used to skip data points that is not wanted. skipping behavior is controlled by parameter interval
            int skipCounter = interval;
            // i is the counter used in for loops
            int i;

            // In this method, we will use long integers (Int64) to represent the date and time. It encodes the time as yyyyMMddHHmmss
            long startTimeInt = Int64.Parse(startDateTime.ToString("yyyyMMddHHmmss"));
            long endTimeInt = Int64.Parse(endDateTime.ToString("yyyyMMddHHmmss"));

            foreach (FileRecord record in fileRecords)
            {
                using (StreamReader sr = new StreamReader(new FileStream(record.fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    switch (record.fileType.ToLower())
                    {
                        case "csv":
                            delimiter = ',';
                            break;
                        case "txt":
                            delimiter = '\t';
                            break;
                        default:
                            throw new ArgumentException("Unsupported File Type: " + record.fileType);

                    }
                    // read the first line that contains the tag names
                    titleLine = sr.ReadLine();
                    splitTitleLine = titleLine.Split(new char[] { delimiter });
                    // number of columns in the csv file
                    nColumn = splitTitleLine.Length;

                    // Find where the tags are located
                    for (i = 0; i < tagList.Length; i++)
                    {
                        int index = Array.FindIndex(splitTitleLine, (string s) => s == tagList[i]);
                        if (index == -1) // The tag is not found
                        {
                            MessageBox.Show("Cannot find tag \"" + tagList[i] + "\" in data file \"" + record.fileName + "\".");
                            indexOfTags.Add(new IndexWithPosition(Int32.MaxValue, i));
                        }
                        else
                            indexOfTags.Add(new IndexWithPosition(index, i));
                    }
                    // Sort the List indexOfTags based on the index. Thus, we can get the value of the tags one by one as we go through one line of the data file
                    indexOfTags.Sort();

                    // Read the first two lines, and figure out the time interval between two lines in the data file
                    line1 = sr.ReadLine();
                    line2 = sr.ReadLine();
                    // if the file contain a "date" or ";date" column, the method for translating the datetime is different
                    if (splitTitleLine[0].ToLower() == "date" || splitTitleLine[0].ToLower() == ";date")
                        dateTimeFromStr = Str2DateTime_1;
                    else // There's no date column. The date is included in the Time column
                        dateTimeFromStr = Str2DateTime_2;
                    DateTime dateTime1 = dateTimeFromStr(line1, delimiter);
                    if (dateTime1 > endDateTime) // if the time stamp is later than endDateTime, no need to continue.
                        break;
                    DateTime dateTime2 = dateTimeFromStr(line2, delimiter);
                    // In some data files, the first line has the same time stamp with the second. 
                    /* Example:
                     * <data file>
                     * Date, Time, tags...
                     * 09/01/2018, 0:00:00,, data...   <- Discard this line
                     * 09/01/2018, 0:00:00,, data...   <- Make this line1
                     * 09/01/2018, 0:00:01,, data...   <- Make this line2
                     * ...
                     * </data file>
                     */
                    if (dateTime1 == dateTime2)
                    {
                        line1 = line2;
                        line2 = sr.ReadLine();
                        dateTime1 = dateTimeFromStr(line1, delimiter);
                        dateTime2 = dateTimeFromStr(line2, delimiter);
                    }
                    // If the List data was not initialized yet. Opening the first file, figure out the time interval between the first two lines
                    // Try to estimate the number of points to be extracted. Initialize array accordingly
                    // Then create the array for the data
                    if (RawData.Count == 0)
                    {
                        nPoints = (int)((endDateTime - startDateTime).Ticks / (dateTime2 - dateTime1).Ticks / interval + 1);
                        // if for any reason nPoints is not positive, there's something wrong and the program will abort here
                        if (nPoints <= 0)
                            throw new ArgumentException("Number of points is not positive.");
                        for (i = 0; i < tagList.Length; i++)
                        {
                            RawData.Add(new float[nPoints]);
                            // //Considering that it should only take a few MBs to store all the data, there's no need to save on the array
                            // //Go over the whole list indexOfTags, find the corresponding record based on the Position, then determine if the tag is found in the file.
                            //foreach(IndexWithPosition tagRecord in indexOfTags)
                            //{
                            //    // located tag record
                            //    if (tagRecord.Position == i)
                            //    {
                            //        if(tagRecord.Index == Int32.MaxValue) // If the Index is Int32.MaxValue, then this tag is not found. Add an empty array to the List data
                            //            data.Add(new float[0]); // 
                            //        else
                            //            data.Add(new float[nPoints]);
                            //    }
                            //}
                        }
                        DateTimes = new DateTime[nPoints];
                        //DateTimeStrs = new string[nPoints];
                        if (dateTime1 >= startDateTime) // Time stamp is after startDateTime. Take the point
                        {
                            DateTimes[pointCount] = dateTime1;
                            //DateTimeStrs[pointCount] = dateTime1.ToString("MM/dd h:mm");
                            ReadStrUntil(line1, delimiter, indexOfTags, ref dataOfOnePoint);
                            for (i = 0; i < indexOfTags.Count; i++)
                                RawData[indexOfTags[i].Position][pointCount] = dataOfOnePoint[i];
                            skipCounter = 1;
                            pointCount++;
                        }
                    }
                    // If it's not the first file. 
                    // Need to check if the time stamp of line 1 of this file is the same as that of the previous file. 
                    // In a lot of data files, the last time stamp of the previouis file is the same as the first two 
                    // time stamps of the second file.
                    /* Example:
                     * <First file>
                     * ...
                     * 09/01/2018, 0:00:00, data....   <- Written in output data previously. Need to be overriden
                     * </First file>
                     * 
                     * <Second file>
                     * Date, Time, tags...
                     * 09/01/2018, 0:00:00,, data...   <- This line would be discarded in previous steps
                     * 09/01/2018, 0:00:00,, data...   <- Only keep this line
                     * 09/01/2018, 0:00:01,, data...
                     * ...
                     * </Second file>
                     */
                    // In this case, will only keep the last value with same time stamp
                    else if (dateTime1 >= startDateTime)
                    {
                        // Time stamp is after startDateTime. Take the point
                        if (pointCount > 0 && dateTime1 == DateTimes[pointCount - 1])
                        {
                            // New time stamp is same as previous
                            // override the previous data by this one
                            pointCount--;
                            ReadStrUntil(line1, delimiter, indexOfTags, ref dataOfOnePoint);
                            for (i = 0; i < indexOfTags.Count; i++)
                                RawData[indexOfTags[i].Position][pointCount] = dataOfOnePoint[i];
                            pointCount++;
                            skipCounter = 1;
                        }
                        else// New time stamp is different from previous
                        {
                            // Perform normal checks
                            if (skipCounter == interval) // will take the point. Otherwise, will skip
                            {
                                if (pointCount == nPoints) // if for some reason the array is not large enough
                                {
                                    // double the size of the array
                                    nPoints *= 2;
                                    for (i = 0; i < indexOfTags.Count; i++)
                                    {
                                        float[] temp = new float[nPoints];
                                        RawData[i].CopyTo(temp, pointCount);
                                        RawData[i] = temp;
                                    }
                                }
                                DateTimes[pointCount] = dateTime1;
                                //DateTimeStrs[pointCount] = dateTime1.ToString("MM/dd h:mm");
                                ReadStrUntil(line1, delimiter, indexOfTags, ref dataOfOnePoint);
                                for (i = 0; i < indexOfTags.Count; i++)
                                    RawData[indexOfTags[i].Position][pointCount] = dataOfOnePoint[i];
                                pointCount++;
                                skipCounter = 1;
                            }
                            else
                                skipCounter++;
                        }
                    }
                    line = line2;
                    // start processing the data file. 
                    do
                    {
                        dateTime1 = dateTimeFromStr(line, delimiter);
                        if (dateTime1 > endDateTime) // if the time stamp is later than endDateTime, no need to continue.
                            break;
                        else if (dateTime1 >= startDateTime)
                        {
                            if (skipCounter == interval) // will take the point. Otherwise, will skip
                            {
                                if (pointCount == nPoints) // if for some reason the array is not large enough
                                {
                                    // double the size of the array
                                    nPoints *= 2;
                                    for (i = 0; i < indexOfTags.Count; i++)
                                    {
                                        float[] temp = new float[nPoints];
                                        RawData[i].CopyTo(temp, pointCount);
                                        RawData[i] = temp;
                                    }
                                }
                                DateTimes[pointCount] = dateTime1;
                                //DateTimeStrs[pointCount] = dateTime1.ToString("MM/dd h:mm");
                                ReadStrUntil(line, delimiter, indexOfTags, ref dataOfOnePoint);
                                for (i = 0; i < indexOfTags.Count; i++)
                                    RawData[indexOfTags[i].Position][pointCount] = dataOfOnePoint[i];
                                pointCount++;
                                skipCounter = 1;
                            }
                            else
                                skipCounter++;
                        }

                    } while ((line = sr.ReadLine()) != null);
                    if (dateTime1 > endDateTime) // if the time stamp is later than endDateTime, no need to continue.
                        break;
                    // clear the indexOfTags so that it does not affect the next file
                    indexOfTags.Clear();
                }
            }
            // if for some reason the size of the array is larger than actual number of points
            if (pointCount < nPoints) 
            {
                // double the size of the array
                nPoints = pointCount;
                for (i = 0; i < tagList.Length  ; i++)
                {
                    float[] temp = new float[nPoints];
                    Array.Copy(RawData[i], temp, pointCount);
                    RawData[i] = temp;
                }
                DateTime[] tempDateTime = new DateTime[nPoints];
                Array.Copy(DateTimes, tempDateTime, pointCount);
                DateTimes = tempDateTime;
                //string[] tempDateTimeStr = new string[nPoints];
                //Array.Copy(DateTimeStrs, tempDateTimeStr, pointCount);
                //DateTimeStrs = tempDateTimeStr;
            }
            // Convert the arrays into LineSeries
            //for (i = 0; i<tagList.Length; i++)
            //{
            //    var series = new LineSeries()
            //    {
            //        Title = tagList[i],
            //        Values = new ChartValues<float>(RawData[i]),
            //        LineSmoothness = 0,
            //        PointGeometry = null,
            //        Fill = Brushes.Transparent,
            //    };
            //    PointsToPlot.Add(series);
            //}
            watch.Stop();
            Console.WriteLine("Data Extraction Completed in " + watch.ElapsedMilliseconds+"ms");
        }

        // Take a line from the data file and read the find the datetime of the the string
        // This function assumes that the file contains a "date" or ";date" column, plus a "time" column. The content before the second delimiter will be used.
        private static DateTime Str2DateTime_1(string str, char delimiter)
        {
            char[] cResult = new char[16];
            char c;
            int charCount = 0;
            int i = 0;
            string dateStr, timeStr;
            // Read until the first delimiter. Use that to get the Date
            do
            {
                c = str[i];
                i++;
                if (c == delimiter) // see a delimiter char
                {
                    break;
                }
                else
                {
                    cResult[charCount] = c;
                    charCount++;
                    if (charCount == cResult.Length)
                        Array.Resize(ref cResult, cResult.Length * 2);
                }
            } while (i < str.Length);
            dateStr = new string(cResult, 0, charCount);
            charCount = 0;
            // Read until the second delimiter. This will be the time
            do
            {
                c = str[i];
                i++;
                if (c == delimiter) // see a delimiter char
                {
                    break;
                }
                else
                {
                    cResult[charCount] = c;
                    charCount++;
                    if (charCount == cResult.Length)
                        Array.Resize(ref cResult, cResult.Length * 2);
                }
            } while (i < str.Length);
            timeStr = new string(cResult, 0, charCount);
            return ParseDate(dateStr) + ParseTime(timeStr);
        }

        // Take a line from the data file and read the find the datetime of the the string
        // This function assumes that the file DOES NOT contains a "date" or ";date" column. ONLY a "time" column exist. 
        // The Time column, which is the first column, should be written in "date time" format, i.e. date and time is separated by a space ' '
        private static DateTime Str2DateTime_2(string str, char delimiter)
        {
            char[] cResult = new char[32];
            char c;
            int charCount = 0;
            int i = 0;
            string dateStr, timeStr;
            // Read until the first space ' '. Use that to get the Date
            do
            {
                c = str[i];
                i++;
                if (c == ' ') // see a space
                {
                    break;
                }
                else
                {
                    cResult[charCount] = c;
                    charCount++;
                    if (charCount == cResult.Length)
                        Array.Resize(ref cResult, cResult.Length * 2);
                }
            } while (i < str.Length);
            dateStr = new string(cResult, 0, charCount);
            charCount = 0;
            // Read until the second delimiter. This will be the time
            do
            {
                c = str[i];
                i++;
                if (c == delimiter) // see a delimiter char
                {
                    break;
                }
                else
                {
                    cResult[charCount] = c;
                    charCount++;
                    if (charCount == cResult.Length)
                        Array.Resize(ref cResult, cResult.Length * 2);
                }
            } while (i < str.Length);
            timeStr = new string(cResult, 0, charCount);
            return ParseDate(dateStr) + ParseTime(timeStr);
        }

        // read the character in "str" one by one until the "nth" occurance of char "end"
        // write anything between the (n-1)th and nth of "end" to out result
        // nth is zero indexing, i.e. count from 0. 
        private static string ReadStrUntil(string str, char end, int nth = 0)
        {

            int endCount = 0; // the number of "end" seen
            int writeCount = 0;
            char[] cResult = new char[16];
            char c;
            int readCount = 0;
            do
            {
                c = str[readCount];
                readCount++;
                if (c == end) // see a end char
                {
                    endCount++;
                    if (endCount == nth + 1)
                        break;
                }
                else if (endCount == nth)
                {
                    cResult[writeCount] = c;
                    writeCount++;
                    if (writeCount == cResult.Length)
                        Array.Resize(ref cResult, cResult.Length * 2);
                }
            } while (readCount < str.Length);
            return new string(cResult, 0, writeCount);
        }

        private static List<string> ReadStrUntil(string str, char end, IList<int> nth)
        {
            // read the character in "str" one by one until the "nth" occurance of char "end"
            // write anything between the (n-1)th and nth of "end" to out result
            // Note: nth is zero indexing, i.e. count from 0
            // This overload method do the same for every number in List nth
            // The caller should guarentee that: 
            // 1. The List nth is sorted in ascending order
            // 2. The List result is at least as large as the List nth
            // 3. The array in List result should be large enough for the field in the csv
            int endCount = 0; // the number of "end" seen
            int itemCount = 0; // number of items completed in the List nth
            int writeCount = 0;
            char[] cResult = new char[16];
            List<string> result = new List<string>(nth.Count);

            foreach (char c in str)
            {
                if (c == end) // see a end char
                    endCount++;
                else if (endCount == nth[itemCount]) // not a end char, and it's between the (n-1)th and nth end char
                {
                    cResult[writeCount] = c;
                    writeCount++;
                    if (writeCount == cResult.Length)
                        Array.Resize(ref cResult, cResult.Length * 2);
                }
                if (endCount == nth[itemCount]+1) // found the nth end char. add the result to the List of string
                {
                    result.Add(new string(cResult, 0, writeCount));
                    // then start over for the next item in nth
                    writeCount = 0;
                    itemCount++;
                }
                if (itemCount == nth.Count)
                    break;
            }
            return result;
        }

        private static void ReadStrUntil(string str, char end, IList<IndexWithPosition> nth, ref float[] result)
        {
            // read the character in "str" one by one until the "nth" occurance of char "end"
            // Convert anything between the (n-1)th and nth of "end" to float and write to "result"
            // This overload method do the same for every number in List nth
            // The caller should guarentee that: 
            // 1. The List nth is sorted in ascending order
            // 2. The List result is at least as large as the List nth
            // 3. The array in result should be long enough to take all fileds specified by "nth"
            int endCount = 0; // the number of "end" seen
            int itemCount = 0; // number of items completed in the List nth
            int writeCount = 0; // number of chars written to cResult
            int readCount = 0; // number of chars read in str
            char[] cResult = new char[16];
            char c;
            // If the current index is Int32.MaxValue, then the upcoming tags are not found in this file.
            // Stop reading, and write NaN to return array
            if (nth[itemCount].Index == Int32.MaxValue)
            {
                do
                {
                    result[itemCount] = Single.NaN;
                    itemCount++;
                } while (itemCount < nth.Count);
            }
            do
            {
                c = str[readCount];
                readCount++;
                if (c == end) // see a end char
                {
                    endCount++;
                    if (endCount == nth[itemCount].Index + 1) // found the nth end char. convert the result to string then float, and add to the array
                    {
                        result[itemCount] = Single.Parse(new string(cResult, 0, writeCount));
                        // then start over for the next item in nth
                        writeCount = 0;
                        itemCount++;
                        if (itemCount == nth.Count)
                            break;
                        // If the current index is Int32.MaxValue, then the upcoming tags are not found in this file.
                        // Stop reading, and write NaN to return array
                        if (nth[itemCount].Index == Int32.MaxValue)
                        {
                            do
                            {
                                result[itemCount] = Single.NaN;
                                itemCount++;
                            } while (itemCount < nth.Count);
                            break;
                        }
                    }

                }
                else if (endCount == nth[itemCount].Index) // not a end char, and it's between the (n-1)th and nth end char
                {
                    cResult[writeCount] = c;
                    writeCount++;
                    if (writeCount == cResult.Length)
                        Array.Resize(ref cResult, cResult.Length * 2);
                }
            } while (readCount < str.Length);
        }

        // Parse the datetime string into DateTime struct. 
        // Assume that in the string date and time is separated by a space
        public static DateTime ParseDateTime(string datetime) =>
            ParseDate(ReadStrUntil(datetime, ' ')) + ParseTime(ReadStrUntil(datetime, ' ', 1));

        // A FileRecord include the file pathname, file type, and start time.
        // The constructor will determine the start time based on the file name.
        private struct FileRecord : IComparable<FileRecord>
        {
            public string fileName, fileType;
            public DateTime startTime;

            public FileRecord(string inputFileNames)
            {
                fileName = inputFileNames;
                fileType = String.Empty;
                startTime = DateTime.MaxValue;

                // Determind the file type and start date
                // The file name shoule end in the format like {Date}-{Time}.xxx
                // Date has to be 8 digits, while time should be 6 digits. xxx is the extension. delimiter between date and time is optional
                Match m = Regex.Match(fileName, @"([0-9]{8})[\W_]*([0-9]{6})\.(\w+)$");
                if (m.Success)
                {
                    try
                    {
                        startTime = DateTime.ParseExact(m.Groups[1].Value + m.Groups[2], @"yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        MessageBox.Show("File name \"" + fileName + "\" is not valid.\r\nFile name must end with start time information in yyyyMMddHHmmss format and extension");
                        fileName = String.Empty;
                    }
                    fileType = m.Groups[3].Value;
                }
                else
                {
                    MessageBox.Show("File name \"" + fileName + "\" is not valid.\r\nFile name must end with start time information in yyyyMMddHHmmss format and extension");
                    fileName = String.Empty;
                }
            }

            public int CompareTo(FileRecord other) =>
                startTime.CompareTo(other.startTime);
        }

        // This struct is used to preserve the original position information in an array after it is sorted
        // Use this struct to form an array, and label the Position from 0 to n
        // If the array is sorted by Index, the Position indicates the original position of the Index in the array before sorting.
        private struct IndexWithPosition : IComparable<IndexWithPosition>
        {
            public int Index;
            public int Position;

            public IndexWithPosition(int index, int position)
            {
                Index = index;
                Position = position;
            }

            public int CompareTo(IndexWithPosition other) =>
                Index.CompareTo(other.Index);
        }

        // Write the extracted data to a file that the user specified
        public void WriteToFile(DateTime startDateTime, DateTime endDateTime, Window owner, string fileType, string defaultPath = "")
        {
            char delimiter;
            int i,j;
            // filterText is used 
            string filterText;
            switch (fileType.ToLower())
            {
                case "csv":
                    delimiter = ',';
                    filterText = "CSV File (.csv) | *.csv";
                    break;
                case "txt":
                    delimiter = '\t';
                    filterText = "Text File (.txt)|*.txt";
                    break;
                default:
                    throw new ArgumentException("Unsupported File Type: " + fileType);

            }
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save As New "+fileType+" File",
                DefaultExt = fileType,
                Filter = filterText,
                FileName = "ExtractedData_"+DateTimes[0].ToString("yyyyMMdd-HHmmss") 
            };
            if (!String.IsNullOrEmpty(defaultPath)) dialog.InitialDirectory = defaultPath;

            if (dialog.ShowDialog(owner) == true && dialog.FileNames != null)
            {
                try
                {
                    // Open the file and start writing to it
                    // IS this the right way to write? Calling sr.Write() multiple times may be slow.
                    // Alternative is to construct a buffer string
                    using (StreamWriter sr = new StreamWriter(dialog.OpenFile(), Encoding.ASCII, 65535))
                    {
                        // write the first line (header line)
                        sr.Write("Time"+delimiter);
                        for (i = 0; i < Tags.Length; i++)
                            sr.Write(Tags[i] + delimiter);
                        for(j=0;j<DateTimes.Length; j++)
                        {
                            if (DateTimes[i] > endDateTime)
                                break;
                            else if (DateTimes[i] >= startDateTime)
                            {
                                sr.Write("\n"+DateTimes[i].ToString("M/d/yyyy HH:mm:ss")+delimiter);
                                for (i = 0; i < RawData.Count; i++)
                                {
                                    sr.Write(RawData[i][j]);
                                    sr.Write(delimiter);
                                }
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show("Fail to write " + fileType + " file: " + dialog.FileName + "\n" + e.Message);
                }
            }
        }


    }
}
