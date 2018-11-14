/// These codes were used to read Excel file using ExcelDataReaer library. Pretty slow. about 15 seconds for each file
//using (FileStream stream = File.Open(record.fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
//                    {
//                        using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
//                        {
//                            // First handle the title line
//                            reader.Read();
//                            nColumn = reader.FieldCount;
//                            // Look at all cells to find the tags
//                            for (int index=0; index<nColumn; index++)
//                            {
//                                string currentTag = reader.GetString(index);
//                                for(i = 0; i<tagList.Length; i++)
//                                {
//                                    // for each cell, check if it matches a requested tag
//                                    if(tagList[i] == currentTag)
//                                    {
//                                        indexOfTags.Add(new IndexWithPosition(index, i));
//                                        break;
//                                    }
//                                }
//                                // found all tags. no need to continue reading excel
//                                if (indexOfTags.Count == tagList.Length)
//                                    break;
//                            }
//                            // No need to sort indexOfTags anymore, as all items are added in the ascending order of Index
//                            // in case some tags does not exist
//                            if (indexOfTags.Count<tagList.Length)
//                            {
//                                // Check which tag does not exist
//                                for(i=0; i<tagList.Length; i++)
//                                {
//                                    for(int j=0;j<indexOfTags.Count; j++)
//                                    {
//                                        if (indexOfTags[j].Position == i)
//                                        {
//                                            // The ith tag is found in indexOfTags
//                                            break;
//                                        }
//                                    }
//                                    MessageBox.Show("Cannot find tag \"" + tagList[i] + "\" in data file \"" + record.fileName + "\".");
//                                    indexOfTags.Add(new IndexWithPosition(Int32.MaxValue, i));
//                                }
//                            }

//                            // Move reader to the data rows.
//                            reader.Read();
//                            // If the List data was not initialized yet. Opening the first file, figure out the time interval between the first two lines
//                            // Try to estimate the number of points to be extracted. Initialize array accordingly
//                            // Then create the array for the data
//                            if (RawData.Count == 0)
//                            {
//                                dateTime1 = reader.GetDateTime(0);
//                                if (dateTime1 > endDateTime) // if the time stamp is later than endDateTime, no need to continue.
//                                    break;
//                                // Get the values of requested tags into dataOfOnePoint
//                                for (i = 0; i<indexOfTags.Count; i++)
//                                {
//                                    if (indexOfTags[i].Index != Int32.MaxValue)
//                                        dataOfOnePoint[i] = (float) reader.GetDouble(indexOfTags[i].Index);
//                                    else
//                                        dataOfOnePoint[i] = Single.NaN;
//                                }
//                                // then go to the second line
//                                reader.Read();
//                                DateTime dateTime2 = reader.GetDateTime(0);
//                                // In some data files, the first two lines have the same time stamp.
//                                // Keep the second one
//                                while (dateTime1 == dateTime2)
//                                {
//                                    // take tag values of line 2
//                                    for (i = 0; i<indexOfTags.Count; i++)
//                                    {
//                                        if (indexOfTags[i].Index != Int32.MaxValue)
//                                            dataOfOnePoint[i] = (float) reader.GetDouble(indexOfTags[i].Index);
//                                        else
//                                            dataOfOnePoint[i] = Single.NaN;
//                                    }
//                                    // go to line 3
//                                    reader.Read();
//                                    dateTime2 = reader.GetDateTime(0);
//                                }
//                                nPoints = (int) ((endDateTime - dateTime1).Ticks / (dateTime2 - dateTime1).Ticks / interval + 1);
//                                // if for any reason nPoints is not positive, there's something wrong and the program will abort here
//                                if (nPoints <= 0)
//                                    throw new ArgumentException("Number of points is not positive.");
//                                for (i = 0; i<tagList.Length; i++)
//                                {
//                                    RawData.Add(new float[nPoints]);
//                                }
//                                DateTimes = new DateTime[nPoints];
//                                //DateTimeStrs = new string[nPoints];
//                                if (dateTime1 >= startDateTime) // Time stamp is after startDateTime. Take the point
//                                {
//                                    DateTimes[pointCount] = dateTime1;
//                                    for (i = 0; i<indexOfTags.Count; i++)
//                                        RawData[indexOfTags[i].Position][pointCount] = dataOfOnePoint[i];
//                                    skipCounter = 1;
//                                    pointCount++;
//                                }
//                            }// If it's the first file, now the reader will be at the 2nd data row
//                            // if it's not the first file, now the reader will be at the 1st data row
//                            do
//                            {
//                                dateTime1 = reader.GetDateTime(0);
//                                if (dateTime1 > endDateTime) // if the time stamp is later than endDateTime, no need to continue.
//                                    break;
//                                else if (dateTime1 >= startDateTime)
//                                {
//                                    if (pointCount > 0 && dateTime1 == DateTimes[pointCount - 1])
//                                    {
//                                        // New time stamp is same as previous
//                                        // override the previous data by this one
//                                        pointCount--;
//                                        for (i = 0; i<indexOfTags.Count; i++)
//                                        {
//                                            if (indexOfTags[i].Index != Int32.MaxValue)
//                                                RawData[indexOfTags[i].Position][pointCount] = (float) reader.GetDouble(indexOfTags[i].Index);
//                                            else
//                                                RawData[indexOfTags[i].Position][pointCount] = Single.NaN;
//                                        }
//                                        pointCount++;
//                                        skipCounter = 1;
//                                    }
//                                    else // new time stamp is different from previous
//                                    {
//                                        if (skipCounter == interval) // will take the point. Otherwise, will skip
//                                        {
//                                            if (pointCount == nPoints) // if for some reason the array is not large enough
//                                            {
//                                                // double the size of the array
//                                                nPoints *= 2;
//                                                Console.WriteLine("Expanding array from {0} to {1} elements", pointCount, nPoints);
//                                                for (i = 0; i<indexOfTags.Count; i++)
//                                                {
//                                                    float[] temp = new float[nPoints];
//Array.Copy(RawData[i], temp, pointCount);
//                                                    RawData[i] = temp;
//                                                }
//                                                DateTime[] tempDateTime = new DateTime[nPoints];
//Array.Copy(DateTimes, tempDateTime, pointCount);
//                                                DateTimes = tempDateTime;
//                                            }
//                                            DateTimes[pointCount] = dateTime1;
//                                            for (i = 0; i<indexOfTags.Count; i++)
//                                            {
//                                                if (indexOfTags[i].Index != Int32.MaxValue)
//                                                    RawData[indexOfTags[i].Position][pointCount] = (float) reader.GetDouble(indexOfTags[i].Index);
//                                                else
//                                                    RawData[indexOfTags[i].Position][pointCount] = Single.NaN;
//                                            }
//                                            pointCount++;
//                                            skipCounter = 1;
//                                        }
//                                        else
//                                            skipCounter++;
//                                    }
//                                }
//                            } while (reader.Read());
//                        }
//                    }

// These codes were used to read Excel file using OpenXML SDK
//// Create SpreadsheetDocument object to represent the excel file
//using(SpreadsheetDocument xlsxFile = SpreadsheetDocument.Open(new FileStream(record.fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), false))
//                    {
//                        WorkbookPart workbookPart = xlsxFile.WorkbookPart;
//WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
//// first handle the sharedstring table.
//// The sharedstring table contains all the tag names.
//SharedStringTablePart sharedStringTablePart = workbookPart.SharedStringTablePart;
//                        // Find the location of all 
//                        using (OpenXmlPartReader reader = new OpenXmlPartReader(sharedStringTablePart))
//                        {
//                            // read all Text element in the sharedstringtable
//                            // Find the index of all the requested  tags
//                            int index = 0;
//int tagCount = 0;
//string currentTag;
//                            while (reader.Read())
//                            {
//                                if (reader.ElementType == typeof(Text) && reader.IsStartElement)
//                                {
//                                    currentTag = reader.GetText();
//                                    for (i = 0; i<tagList.Length; i++)
//                                    {
//                                        // for each cell, check if it matches a requested tag
//                                        if (tagList[i] == currentTag)
//                                        {
//                                            strIndexOfTags[tagCount] = index.ToString();
//                                            positions[tagCount] = i;
//                                            tagCount++;
//                                            break;
//                                        }
//                                    }
//                                    // found all tags. no need to continue reading excel
//                                    if (tagCount == tagList.Length)
//                                        break;
//                                    index++;
//                                }
//                            }
//                            // No need to sort indexOfTags anymore, as all items are added in the ascending order of Index
//                            // in case some tags does not exist
//                            if (tagCount<tagList.Length)
//                            {
//                                // Check which tag does not exist. Go over all tags
//                                for (i = 0; i<tagList.Length; i++)
//                                {
//                                    // for each tag, see if the corresponding Position exist in positions
//                                    for (int j = 0; j<tagCount; j++)
//                                    {
//                                        if (positions[j] == i)
//                                        {
//                                            // The ith tag is found in indexOfTags
//                                            break;
//                                        }
//                                        else if (j == tagCount - 1) // hit the last thing in positions, and tag is not found.
//                                        {
//                                            // Add one entry to strIndexOfTags and positions. done with this tag
//                                            MessageBox.Show("Cannot find tag \"" + tagList[i] + "\" in data file \"" + record.fileName + "\".");
//                                            //strIndexOfTags[tagCount] = "";
//                                            positions[tagCount] = i;
//                                            tagCount++;
//                                            break;
//                                        }
//                                    }
//                                    if (tagCount == tagList.Length)
//                                        break;
//                                }
//                            }
//                        }
                        
//                        // Now go to the data sheet xml, which is the worksheet part
//                        using (OpenXmlPartReader reader = new OpenXmlPartReader(worksheetPart))
//                        {
//                            int tagCount = 0;
//// For OpenXml format file, if a cell is empty, no Cell exist in the xml
//// Thus, we have to rely on the CellReference (attribute "r") to determine what value to take
//// refOfTags contains the CellReference (only the column part, no row number) of the tags in TagList and indexOfTags
//Cell currentCell;
//XmlFindNext(reader, typeof(Cell));
                            
//                            // In first row, get the column references of the tags
//                            // The first row in xml looks like:
//                            // <Row>
//                            //  <c r="A1" t="s">
//                            //   <v>0</v>
//                            //  </c>
//                            do
//                            {
//                                currentCell = (Cell) reader.LoadCurrentElement();
//                                if (currentCell.CellValue.Text == strIndexOfTags[tagCount])
//                                {
//                                    refOfTags[tagCount] = GetColumnRef(currentCell.CellReference);
//tagCount++;
//                                    if (tagCount == tagList.Length || String.IsNullOrEmpty(strIndexOfTags[tagCount]))
//                                        break;
//                                }
                                
//                            } while (reader.ReadNextSibling());
//                            XmlFindNext(reader, typeof(Row));
//XmlFindNext(reader, typeof(Cell));
//currentCell = (Cell) reader.LoadCurrentElement();
//                            // If the List data was not initialized yet. Opening the first file, figure out the time interval between the first two lines
//                            // Try to estimate the number of points to be extracted. Initialize array accordingly
//                            // Then create the array for the data
//                            if (RawData.Count == 0)
//                            {
//                                dateTime1 = DateTime.FromOADate(Double.Parse(currentCell.CellValue.Text));
//                                if (dateTime1 > endDateTime) // if the time stamp is later than endDateTime, no need to continue.
//                                    break;
//                                // Get the values of requested tags into dataOfOnePoint
//                                if (dateTime1 >= startDateTime)
//                                    GetCellValues2(reader, refOfTags, ref dataOfOnePoint);
//// then go to the second line
//XmlFindNext(reader, typeof(Row));
//XmlFindNext(reader, typeof(Cell));
//currentCell = (Cell) reader.LoadCurrentElement();
//DateTime dateTime2 = DateTime.FromOADate(Double.Parse(currentCell.CellValue.Text));
//                                // In some data files, the first two lines have the same time stamp.
//                                // Keep the second one
//                                while (dateTime1 == dateTime2)
//                                {
//                                    // take tag values of line 2
//                                    GetCellValues2(reader, refOfTags, ref dataOfOnePoint);
//// go to line 3
//XmlFindNext(reader, typeof(Row));
//XmlFindNext(reader, typeof(Cell));
//currentCell = (Cell) reader.LoadCurrentElement();
//dateTime2 = DateTime.FromOADate(Double.Parse(currentCell.CellValue.Text));
//                                }
//                                nPoints = (int) ((endDateTime - dateTime1).Ticks / (dateTime2 - dateTime1).Ticks / interval + 1);
//                                // if for any reason nPoints is not positive, there's something wrong and the program will abort here
//                                if (nPoints <= 0)
//                                    throw new ArgumentException("Number of points is not positive.");
//                                for (i = 0; i<tagList.Length; i++)
//                                {
//                                    RawData.Add(new float[nPoints]);
//                                }
//                                DateTimes = new DateTime[nPoints];
//                                //DateTimeStrs = new string[nPoints];
//                                if (dateTime1 >= startDateTime) // Time stamp is after startDateTime. Take the point
//                                {
//                                    DateTimes[pointCount] = dateTime1;
//                                    for (i = 0; i<positions.Length; i++)
//                                        RawData[positions[i]][pointCount] = dataOfOnePoint[i];
//                                    skipCounter = 1;
//                                    pointCount++;
//                                }
//                            }// If it's the first file, now the reader and currentCell will be at the 2nd data row first cell
//                            // if it's not the first file, now the reader and currentCell will be at the 1st data row first cell
//                            do
//                            {
//                                dateTime1 = DateTime.FromOADate(Double.Parse(currentCell.CellValue.Text));
//                                if (dateTime1 > endDateTime) // if the time stamp is later than endDateTime, no need to continue.
//                                    break;
//                                else if (dateTime1 >= startDateTime)
//                                {
//                                    if (pointCount > 0 && dateTime1 == DateTimes[pointCount - 1])
//                                    {
//                                        // New time stamp is same as previous
//                                        // override the previous data by this one
//                                        pointCount--;
//                                        GetCellValues2(reader, refOfTags, ref dataOfOnePoint);
//                                        for (i = 0; i<positions.Length; i++)
//                                            RawData[positions[i]][pointCount] = dataOfOnePoint[i];
//                                        pointCount++;
//                                        skipCounter = 1;
//                                    }
//                                    else
//                                    {
//                                        if (skipCounter == interval) // will take the point. Otherwise, will skip
//                                        {
//                                            if (pointCount == nPoints) // if for some reason the array is not large enough
//                                            {
//                                                // double the size of the array
//                                                nPoints *= 2;
//                                                Console.WriteLine("Expanding array from {0} to {1} elements", pointCount, nPoints);
//                                                for (i = 0; i<indexOfTags.Count; i++)
//                                                {
//                                                    float[] temp = new float[nPoints];
//Array.Copy(RawData[i], temp, pointCount);
//                                                    RawData[i] = temp;
//                                                }
//                                                DateTime[] tempDateTime = new DateTime[nPoints];
//Array.Copy(DateTimes, tempDateTime, pointCount);
//                                                DateTimes = tempDateTime;
//                                            }
//                                            DateTimes[pointCount] = dateTime1;
//                                            //DateTimeStrs[pointCount] = dateTime1.ToString("MM/dd h:mm");
//                                            GetCellValues2(reader, refOfTags, ref dataOfOnePoint);
//                                            for (i = 0; i<positions.Length; i++)
//                                                RawData[positions[i]][pointCount] = dataOfOnePoint[i];
//                                            pointCount++;
//                                            skipCounter = 1;
//                                        }
//                                        else
//                                            skipCounter++;
//                                    }
//                                }
//                                XmlFindNext(reader, typeof(Row));
//XmlFindNext(reader, typeof(Cell));
//                                if(!reader.EOF)
//                                    currentCell = (Cell) reader.LoadCurrentElement();
//                            } while (!reader.EOF);
//                            if (dateTime1 > endDateTime) // if the time stamp is later than endDateTime, no need to continue.
//                                break;
//                        }
//                    }
                