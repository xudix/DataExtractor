using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataExtractor
{
    /// <summary>
    /// Tools to process Excel OpenXML (XLSX) files
    /// </summary>
    class XlsxTool
    {
        /// <summary>
        /// Get the shared string table of the Excel OpenXML (XLSX) file and return it in an string array
        /// </summary>
        /// <param name="fileName">The file name of the Excel OpenXML (XLSX) file containing the header</param>
        /// <returns>The header (first row) of the Excel OpenXML (XLSX) file</returns>
        public static string[] GetSharedStrings(string fileName)
            => GetSharedStrings(new ZipArchive(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)));

        /// <summary>
        /// Get the shared string table of the Excel OpenXML (XLSX) file and return it in an string array
        /// </summary>
        /// <param name="zipArchive">The file stream of the Excel OpenXML (XLSX) file containing the header</param>
        /// <returns>The header (first row) of the Excel OpenXML (XLSX) file</returns>
        public static string[] GetSharedStrings(ZipArchive zipArchive)
        {
            SharedStringTable SharedStrings;
            string[] result;
            SharedStrings = DeserializedZipEntry<SharedStringTable>(zipArchive.GetEntry(@"xl/sharedStrings.xml"));
            result = new string[Int32.Parse(SharedStrings.count)];
            for (int i = 0; i < SharedStrings.si.Length; i++)
                result[i] = SharedStrings.si[i].t;
            return result;
        }

        /// <summary>
        /// Get the header (first row) of the first sheet of an Excel OpenXML (XLSX) file
        /// </summary>
        /// <param name="zipArchive"></param>
        /// <returns></returns>
        public static string[] GetHeader(ZipArchive zipArchive)
        {
            // Normally the sharedStrings should be all the headers we need. 
            // However, in principal, if the file contains other strings, the shared string table is larger than the first row
            // Thus, we open the sheet to actually process the first row.
            string[] sharedStrings = GetSharedStrings(zipArchive);
            
            XmlEntry firstRow;
            int sharedStringIndex;
            // open the first sheet of the file
            using (StreamReader worksheetReader = new StreamReader(zipArchive.GetEntry(@"xl/worksheets/sheet1.xml").Open()))
            {
                firstRow = XlsxReadOne(worksheetReader, "row");
            }
            // method one: RegEx
            MatchCollection matches = Regex.Matches(firstRow.text, @"(?<=<v>)(.*?)(?=<)");
            string[] result = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                sharedStringIndex = Int32.Parse(matches[i].Value);
                result[i] = sharedStrings[sharedStringIndex];
            }
            // method two: XlsxReadOne(stringreader, "v")
            // XmlEntry cell, value;
            //using(StringReader sr = new StringReader(firstRow.text))
            //{
            //    while (sr.Peek() >= 0)
            //    {
            //        cell = XlsxReadOne(sr, "c");

            //    }
            //}

            return result;
        }

        /// <summary>
        /// Get the header (first row) and the corresponding column references of the first sheet of an Excel OpenXML (XLSX) file
        /// </summary>
        /// <param name="zipArchive"></param>
        /// <returns></returns>
        public static HeaderWithColRef GetHeaderWithColReference(ZipArchive zipArchive)
        {
            // Normally the sharedStrings should be all the headers we need. 
            // However, in principal, if the file contains other strings, the shared string table is larger than the first row
            // Thus, we open the sheet to actually process the first row.
            string[] sharedStrings = GetSharedStrings(zipArchive);
            HeaderWithColRef result;
            XmlEntry firstRow;
            int sharedStringIndex;
            // open the first sheet of the file
            using (StreamReader worksheetReader = new StreamReader(zipArchive.GetEntry(@"xl/worksheets/sheet1.xml").Open()))
            {
                firstRow = XlsxReadOne(worksheetReader, "row");
            }
            // method one: RegEx
            string pattern = @"<c\s*(?:r=""(?<colRef>[A-Z]+)[0-9]+"")?(?:\s(?:cm|ph|s|vm)="".*?"")*(?:\st=""(?<type>[a-z]*)"")?.*?>\s*<v>(?<value>.*?)(?=<)";
            MatchCollection matches = Regex.Matches(firstRow.text, pattern);
            result.header = new string[matches.Count];
            result.colRef = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].Groups["type"].Value == "s")// The cell contains string data, which can come from shared strin table
                {
                    sharedStringIndex = Int32.Parse(matches[i].Groups["value"].Value);
                    result.header[i] = sharedStrings[sharedStringIndex];
                }
                else
                    result.header[i] = matches[i].Groups["value"].Value;
                result.colRef[i] = matches[i].Groups["colRef"].Value;
            }
            // method two: XlsxReadOne(stringreader, "v")
            // XmlEntry cell, value;
            //using(StringReader sr = new StringReader(firstRow.text))
            //{
            //    while (sr.Peek() >= 0)
            //    {
            //        cell = XlsxReadOne(sr, "c");

            //    }
            //}

            return result;
        }

        public static XmlEntry XlsxReadOne(TextReader reader, string elementWanted)
        {
            string elementName;
            char c;
            char[] textBuffer = new char[4096];
            char[] attributeBuffer = new char[1024];
            char[] elementNameBuffer = new char[1024];
            int textWriteCount = 0, attrWriteCount = 0, elementNameWriteCount = 0;
            bool isInWantedElement = false;
            ReaderLocationType locationType = ReaderLocationType.StartOfSearch;
            XmlEntry result;
            result.text = String.Empty;
            result.xmlAttributes = new List<XmlAttributeItem>();
            while(reader.Peek() >= 0)
            {
                c = (char)reader.Read();
                if (isInWantedElement) // Currently in the wanted element. Will write down the current char and watch if we get to the end of the element
                {
                    textBuffer[textWriteCount] = c;
                    textWriteCount++;
                    if (textWriteCount == textBuffer.Length)
                        Array.Resize(ref textBuffer, textBuffer.Length + 4096);
                    switch (locationType)
                    {
                        case ReaderLocationType.Text:
                            if (c == '<')
                            {
                                locationType = ReaderLocationType.StartElement;
                            }
                            break;
                        case ReaderLocationType.StartElement:
                            if (c == '/') // it's an EndElement
                                locationType = ReaderLocationType.EndElement;
                            else
                                locationType = ReaderLocationType.Text;
                            break;
                        case ReaderLocationType.EndElement: // need to determine if this is the end of "elementWanted"
                            switch (c)
                            {
                                case ' ': // end of element name. should not see this in endElement
                                    elementName = new string(elementNameBuffer, 0, elementNameWriteCount);
                                    if (elementName == elementWanted)// finished the element we want. Will return
                                    {
                                        if (textWriteCount > 0)
                                            // The number of chars written to the string is reduced to takeout:
                                            // the element name and "</ " in the EndElement
                                            result.text = new string(textBuffer, 0, textWriteCount - elementNameWriteCount - 3);
                                        return result;
                                    }
                                    else // other element and not inside the wanted element.
                                    {
                                        // search for the next element
                                        locationType = ReaderLocationType.Text;
                                    }
                                    elementNameWriteCount = 0;
                                    break;
                                case '>':
                                    elementName = new string(elementNameBuffer, 0, elementNameWriteCount);
                                    if (elementName == elementWanted)// finished the element we want. Will return
                                    {
                                        if (textWriteCount > 0)
                                            // The number of chars written to the string is reduced to takeout:
                                            // the element name and "</>" in the EndElement
                                            result.text = new string(textBuffer, 0, textWriteCount - elementNameWriteCount - 3);
                                        return result;
                                    }
                                    else // other element and not inside the wanted element.
                                    {
                                        // search for the next element
                                        locationType = ReaderLocationType.Text;
                                    }
                                    elementNameWriteCount = 0;
                                    break;
                                default:
                                    elementNameBuffer[elementNameWriteCount] = c;
                                    elementNameWriteCount++;
                                    break;
                            }
                            break;
                    }
                }
                else // Not in the wanted element. Will not write down the current char. Watch for start element of wanted char
                {
                    switch (locationType) // The following code does not write anything to textBuffer. It should only write to attributes and control the locationType
                    {
                        case ReaderLocationType.StartOfSearch:
                            if (c == '<')
                            {
                                locationType = ReaderLocationType.StartElement;
                            }
                            break;
                        case ReaderLocationType.StartElement:
                            switch (c)
                            {
                                case ' ': // end of element name. Upcoming things will be attributes
                                    elementName = new string(elementNameBuffer, 0, elementNameWriteCount);
                                    elementNameWriteCount = 0;
                                    if (elementName == elementWanted)// got into the element we want. Will read the attribute 
                                    {
                                        locationType = ReaderLocationType.Attribute;
                                    }
                                    else // other element and not inside the wanted element.
                                    {
                                        // search for the next element
                                        locationType = ReaderLocationType.StartOfSearch;
                                    }
                                    break;
                                case '>': // end of element name and no attributel go to text.
                                    elementName = new string(elementNameBuffer, 0, elementNameWriteCount);
                                    elementNameWriteCount = 0;
                                    if (elementName == elementWanted)// got into the element we want.
                                    {
                                        locationType = ReaderLocationType.Text;
                                        isInWantedElement = true;
                                    }
                                    else // other element and not inside the wanted element.
                                    {
                                        // search for the next element
                                        locationType = ReaderLocationType.StartOfSearch;
                                    }
                                    break;
                                case '/': // end of element. 
                                    if (elementNameWriteCount != 0) // It's not following the < sign. It's an empty element.
                                    {
                                        elementName = new string(elementNameBuffer, 0, elementNameWriteCount);
                                        elementNameWriteCount = 0;
                                        if (elementName == elementWanted)// got into the element we want. Will return
                                        {
                                            if (textWriteCount > 0)
                                                result.text = new string(textBuffer, 0, textWriteCount);
                                            return result;
                                        }
                                        else // other element and not inside the wanted element.
                                        {
                                            // search for the next element
                                            locationType = ReaderLocationType.StartOfSearch;
                                        }
                                    }
                                    else // it's following a < sign. End element. Since we are not in the wanted element, don't care about this element.
                                        locationType = ReaderLocationType.StartOfSearch;
                                    break;
                                default:
                                    elementNameBuffer[elementNameWriteCount] = c;
                                    elementNameWriteCount++;
                                    break;
                            }
                            break;
                        case ReaderLocationType.Attribute: // Attribute only exis in the StartElement. 
                            switch (c)
                            {
                                case ' ': // end of an attribute. Another attribute coming.
                                    result.xmlAttributes.Add(new XmlAttributeItem(new string(attributeBuffer, 0, attrWriteCount)));
                                    attrWriteCount = 0;
                                    break;
                                case '/': // end of attribute and the whole element. should return.
                                    result.xmlAttributes.Add(new XmlAttributeItem(new string(attributeBuffer, 0, attrWriteCount)));
                                    if (textWriteCount > 0)
                                        result.text = new string(textBuffer, 0, textWriteCount);
                                    attrWriteCount = 0;
                                    return result;
                                case '>': // end of attribute and start element. start text part.
                                    result.xmlAttributes.Add(new XmlAttributeItem(new string(attributeBuffer, 0, attrWriteCount)));
                                    attrWriteCount = 0;
                                    locationType = ReaderLocationType.Text;
                                    isInWantedElement = true;
                                    break;
                                default:
                                    attributeBuffer[attrWriteCount] = c;
                                    attrWriteCount++;
                                    break;
                            }
                            break;
                        case ReaderLocationType.Text: // seems to be the same as "StartOfSearch"
                            if (c == '<')
                            {
                                locationType = ReaderLocationType.StartElement;
                            }
                            break;
                    }
                }
            }
            // get to the end of the file
            if (textWriteCount > 0)
                result.text = new string(textBuffer, 0, textWriteCount);
            return result;
        }


        private enum ReaderLocationType {StartOfSearch, StartElement, Attribute, Text, EndElement, EndOfFile }


        private static T DeserializedZipEntry<T>(ZipArchiveEntry ZipArchiveEntry)
        {
            using (Stream stream = ZipArchiveEntry.Open())
                return (T)new XmlSerializer(typeof(T)).Deserialize(XmlReader.Create(stream));
        }

        public struct XmlEntry
        {
            public string text;
            public List<XmlAttributeItem> xmlAttributes;

            //public string this[string index]
            //{
            //    get
            //    {
                    
            //    }
            //}

        }


        public struct XmlAttributeItem
        {
            string Name;
            string Value;
            
            /// <summary>
            /// Convert the Xml attribute string in to a XmlAttributeItem.
            /// </summary>
            /// <param name="xmlAttributeText">The string containing the attribute in Xml file. It should be in format name="value"</param>
            public XmlAttributeItem(string xmlAttributeText)
            {
                Name = "";
                Value = "";
                char c;
                int readCount = 0, writeCount = 0;
                char[] buffer = new char[1024];
                for(readCount = 0; readCount<xmlAttributeText.Length; readCount++)
                {
                    c = xmlAttributeText[readCount];
                    if (c == '=') // get to the end of the attribute name.
                    {
                        Name = new string(buffer, 0, writeCount);
                        writeCount = 0;
                        break;
                    }
                    else
                    {
                        buffer[writeCount] = c;
                        writeCount++;
                    }
                }
                // finished reading the attribute name. move to the attribute value
                do
                {
                    readCount++;
                } while (xmlAttributeText[readCount] != '\"');
                readCount++;
                for (; readCount < xmlAttributeText.Length; readCount++)
                {
                    c = xmlAttributeText[readCount];
                    if (c == '\"') // get to the end of the attribute value
                    {
                        Value = new string(buffer, 0, writeCount);
                        break;
                    }
                    else
                    {
                        buffer[writeCount] = c;
                        writeCount++;
                    }
                }
            }
        }


        public struct HeaderWithColRef
        {
            public string[] header;
            public string[] colRef;
        }
    }

    /// <summary>
    /// (c) 2014 Vienna, Dietmar Schoder
    /// 
    /// Code Project Open License (CPOL) 1.02
    /// 
    /// Handles a "shared strings XML-file" in an Excel xlsx-file
    /// </summary>
    [Serializable()]
    [XmlType(Namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main")]
    [XmlRoot("sst", Namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main")]
    public class SharedStringTable
    {
        [XmlAttribute]
        public string uniqueCount;
        [XmlAttribute]
        public string count;
        [XmlElement("si")]
        public SharedString[] si;

        public SharedStringTable()
        {
        }
    }
    public class SharedString
    {
        public string t;
        public override string ToString()
         => t;
    }
}
