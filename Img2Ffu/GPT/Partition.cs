﻿// Copyright (c) 2018, Rene Lergner - wpinternals.net - @Heathcliff74xda
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;

namespace Img2Ffu
{
    public partial class GPT
    {
        public class Partition
        {
            private UInt64 _SizeInSectors;
            private UInt64 _FirstSector;
            private UInt64 _LastSector;

            public string Name;            // 0x48
            public Guid PartitionTypeGuid; // 0x10
            public Guid PartitionGuid;     // 0x10
            internal UInt64 Attributes;      // 0x08
            
            internal UInt64 SizeInSectors
            {
                get
                {
                    if (_SizeInSectors != 0)
                    {
                        return _SizeInSectors;
                    }
                    else
                    {
                        return LastSector - FirstSector + 1;
                    }
                }
                set
                {
                    _SizeInSectors = value;
                    if (FirstSector != 0)
                    {
                        LastSector = FirstSector + _SizeInSectors - 1;
                    }
                }
            }
            
            internal UInt64 FirstSector // 0x08
            {
                get
                {
                    return _FirstSector;
                }
                set
                {
                    _FirstSector = value;
                    if (_SizeInSectors != 0)
                    {
                        _LastSector = FirstSector + _SizeInSectors - 1;
                    }
                }
            }
            
            internal UInt64 LastSector // 0x08
            {
                get
                {
                    return _LastSector;
                }
                set
                {
                    _LastSector = value;
                    _SizeInSectors = 0;
                }
            }
            
            public string Volume
            {
                get
                {
                    return @"\\?\Volume" + PartitionGuid.ToString("b") + @"\";
                }
            }
            
            public string FirstSectorAsString
            {
                get
                {
                    return "0x" + FirstSector.ToString("X16");
                }
                set
                {
                    FirstSector = Convert.ToUInt64(value, 16);
                }
            }
            
            public string LastSectorAsString
            {
                get
                {
                    return "0x" + LastSector.ToString("X16");
                }
                set
                {
                    LastSector = Convert.ToUInt64(value, 16);
                }
            }
            
            public string AttributesAsString
            {
                get
                {
                    return "0x" + Attributes.ToString("X16");
                }
                set
                {
                    Attributes = Convert.ToUInt64(value, 16);
                }
            }
        }
    }
}