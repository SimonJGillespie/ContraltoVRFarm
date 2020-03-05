﻿/*  
    This file is part of IFS.

    IFS is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    IFS is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with IFS.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFS.FTP
{
    /// <summary>
    /// Defines the well-known set of FTP property names, both Mandatory and Optional.
    /// </summary>
    public static class KnownPropertyNames
    {
        // Mandatory
        public static readonly string ServerFilename = "Server-Filename";
        public static readonly string Type = "Type";
        public static readonly string EndOfLineConvention = "End-of-Line-Convention";
        public static readonly string ByteSize = "Byte-Size";
        public static readonly string Device = "Device";
        public static readonly string Directory = "Directory";
        public static readonly string NameBody = "Name-Body";
        public static readonly string Version = "Version";

        // Optional
        public static readonly string Size = "Size";
        public static readonly string UserName = "User-Name";
        public static readonly string UserPassword = "User-Password";
        public static readonly string UserAccount = "User-Account";
        public static readonly string ConnectName = "Connect-Name";
        public static readonly string ConnectPassword = "Connect-Password";
        public static readonly string CreationDate = "Creation-Date";
        public static readonly string WriteDate = "Write-Date";
        public static readonly string ReadDate = "Read-Date";
        public static readonly string Author = "Author";
        public static readonly string Checksum = "Checksum";
        public static readonly string DesiredProperty = "Desired-Property";

        // Mail
        public static readonly string Mailbox = "Mailbox";
        public static readonly string Length = "Length";
        public static readonly string DateReceived = "Date-Received";
        public static readonly string Opened = "Opened";
        public static readonly string Deleted = "Deleted";
    }         

    /// <summary>
    /// Defines an FTP PropertyList and methods to work with the contents of one.
    /// From the FTP spec:
    /// 
    /// "5.1 Syntax of a file property list
    /// 
    /// A file property list consists of a string of ASCII characters, beginning with a left parenthesis and
    /// ending with a matching right parenthesis.  Within that list, each property is represented similarly
    /// as a parenthesized list.  For example:
    ///    ((Server-Filename TESTFILE.7)(Byte-Size 36))
    /// 
    /// This scheme has the advantage of being human readable, although it will require some form of 
    /// scanner or interpreter.  Nevertheless, this is a rigid format, with minimum flexibility ni form; FTP is
    /// a machine-to-machine protocol, not a programming language.
    /// 
    /// The first item in each property (delimited by a left parenthesis and a space) is the property name,
    /// taken from a fixed but extensible set.  Upper- and lower-case letters are considered equivalent in the
    /// property name.  The text between the first space and the right parenthesis is the property value.  All
    /// characters in the property value are taken literally, except in accordance with the quoting convention
    /// described below.
    /// 
    /// All spaces are significant, and multiple spaces may not be arbitrarily included.  There should be no space
    /// between the two leading parentheses, for example, and a single space separates a property 
    /// name from the property value.  Other spaces in a property value will become part of that value, so 
    /// that the following example will work properly:
    ///   ((Server-Filename xxxxx)(Read-Date 23-Jan-76 11:30:22 PST))
    /// 
    /// A single apostrophe is used as the quote character in a property value, and should be used before a 
    /// parenthesis or a desired apostrophe:
    ///   Don't(!)Goof ==> (PropertyName Don''t'(!')Goof)"
    /// 
    /// 
    /// </summary>
    public class PropertyList
    {
        public PropertyList()
        {
            _propertyList = new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Parses a property list from the specified string.
        /// </summary>
        /// <param name="list"></param>
        public PropertyList(string list) : this()
        {
            ParseList(list, 0);
        }

        /// <summary>
        /// Parses a property list from the specified string at the given starting offset.
        /// endIndex returns the end of the parsed property list in the string.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        public PropertyList(string input, int startIndex, out int endIndex) : this()
        {
            endIndex = ParseList(input, startIndex);            
        }

        /// <summary>
        /// Indicates whether the Property List contains the specified property
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsPropertyValue(string name)
        {
            return (_propertyList.ContainsKey(name.ToLowerInvariant()));
        }

        /// <summary>
        /// Returns the first value for the specified property, if present.  Otherwise returns null.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetPropertyValue(string name)
        {
            name = name.ToLowerInvariant();

            if (_propertyList.ContainsKey(name))
            {
                return _propertyList[name][0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the list of property values associated with the given property name, if present.
        /// Otherwise returns null.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<string> GetPropertyValues(string name)
        {
            name = name.ToLowerInvariant();

            if (_propertyList.ContainsKey(name))
            {
                return _propertyList[name];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sets a single value for the specified property, if present.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetPropertyValue(string name, string value)
        {
            name = name.ToLowerInvariant();

            List<string> newpList = new List<string>();
            newpList.Add(value);

            if (_propertyList.ContainsKey(name))
            {                
                _propertyList[name] = newpList;
            }
            else
            {
                _propertyList.Add(name, newpList);
            }
        }

        /// <summary>
        /// Sets multiple values for the specified property, if present.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetPropertyValues(string name, List<string> values)
        {
            name = name.ToLowerInvariant();

            if (_propertyList.ContainsKey(name))
            {
                _propertyList[name] = values;
            }
            else
            {
                _propertyList.Add(name, values);
            }
        }

        /// <summary>
        /// Serialize the PropertyList back to its string representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            // Opening paren
            sb.Append("(");

            foreach(string key in _propertyList.Keys)
            {
                foreach (string value in _propertyList[key])
                {
                    sb.AppendFormat("({0} {1})", key, EscapeString(value));
                }
            }

            // Closing paren
            sb.Append(")");

            return sb.ToString();
        }

        private string EscapeString(string value)
        {
            StringBuilder sb = new StringBuilder(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\'' || value[i] == '(' || value[i] == ')')
                {
                    // Escape this thing
                    sb.Append('\'');
                    sb.Append(value[i]);
                }
                else
                {
                    sb.Append(value[i]);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parses a string representation of a property list into our hash table.
        /// </summary>
        /// <param name="list"></param>
        private int ParseList(string input, int startOffset)
        {
            string list = input.Substring(startOffset);

            //
            // First check the basics; the string must start and end with left and right parens, respectively.
            // We do not trim whitespace as there should not be any per the spec.
            //
            if (!list.StartsWith("(") || !list.EndsWith(")"))
            {
                throw new InvalidOperationException("Property list must begin and end with parentheses.");
            }            

            //
            // Looking good so far; parse individual properties now.  These also start and end with
            // left and right parens.
            //
            int index = 1;

            //
            // Loop until we hit the end of the string (minus the closing paren)
            //
            while (index < list.Length)
            { 
                // If this is a closing paren, this denotes the end of the property list.
                if (list[index] == ')')
                {
                    break;
                }

                // Start of next property, must begin with a left paren.
                if (list[index] != '(')
                {
                    throw new InvalidOperationException("Property must begin with a left parenthesis.");
                }

                index++;                

                //
                // Read in the full property name.  Property names can't have escaped characters in them
                // so we don't need to watch out for those, just find the first space.
                //
                int endIndex = list.IndexOf(' ', index);

                if (endIndex < 0)
                {
                    throw new InvalidOperationException("Badly formed property list, no space delimiter found.");
                }

                string propertyName = list.Substring(index, endIndex - index).ToLowerInvariant();
                index = endIndex + 1;       // Move past space

                //
                // Read in the property value.  This may contain spaces or escaped characters and it ends with an
                // unescaped right paren.
                //
                StringBuilder propertyValue = new StringBuilder();

                while(true)
                {
                    // End of value?
                    if (list[index] == ')')
                    {
                        // Move past closing paren
                        index++;

                        // And we're done with this property.
                        break;
                    }
                    // Quoted value?
                    else if (list[index] == '\'')
                    {
                        // Add quoted character
                        index++;

                        // Ensure we don't walk off the end of the string
                        if (index >= list.Length)
                        {
                            throw new InvalidOperationException("Invalid property list syntax.");
                        }

                        propertyValue.Append(list[index]);
                    }
                    // Just a normal character
                    else
                    {
                        propertyValue.Append(list[index]);
                    }

                    index++;

                    // Ensure we don't walk off the end of the string
                    if (index >= list.Length)
                    {
                        throw new InvalidOperationException("Invalid property list syntax.");
                    }
                }

                //
                // Add name/value pair to the hash table.
                //
                if (!_propertyList.ContainsKey(propertyName))
                {
                    // New property key
                    List<string> newpList = new List<string>();
                    newpList.Add(propertyValue.ToString());
                    _propertyList.Add(propertyName, newpList);
                }
                else
                {
                    // Property key with multiple values
                    _propertyList[propertyName].Add(propertyValue.ToString());
                }                
            }

            return index + startOffset + 1;
        }        

        private Dictionary<string, List<string>> _propertyList;
    }
}
