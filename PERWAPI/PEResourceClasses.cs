/*  
 * PERWAPI - An API for Reading and Writing PE Files
 * 
 * Copyright (c) Diane Corney, Queensland University of Technology, 2004.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the PERWAPI Copyright as included with this
 * distribution in the file PERWAPIcopyright.rtf.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY as is explained in the copyright notice.
 *
 * The author may be contacted at d.corney@qut.edu.au
 * 
 * Version Date:  26/01/07
 */

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;


namespace QUT.PERWAPI
{
  /// <summary>
  /// (Unmanaged) Resource Elements consist of PEResourceDirectories
  /// or PEResourceData elements.  Resource directories may be nested up
  /// to three deep sorted on Type, Name and Language in that order.
  /// </summary>
    public abstract class PEResourceElement
    {
      protected PEResourceElement() { }

      public int Id { get; set; }

      public string Name { get; set; }

      protected internal abstract uint Size();

      /// <summary>
      /// Write out the unmanaged resource data.
      /// </summary>
      /// <param name="dest">The Binary Writer</param>
      /// <param name="baseOffset">File position at start of .rsrc section</param>
      /// <param name="RVA">RVA of .rsrc section when loaded</param>
        protected internal abstract void Write(BinaryWriter dest, uint baseOffset, uint currentOffset, uint RVA);

        protected internal long offset;

        protected const uint HeaderSize = 16;
        protected const uint EntrySize = 8;
    }

    /// <summary>
    /// ResourceDirectory entries, as defined in Winnt.h
    /// as type struct _IMAGE_RESOURCE_DIRECTORY.
    /// </summary>
    public class PEResourceDirectory : PEResourceElement {
        public uint Date { get; set; } = 0;
        public ushort MajVer { get; set; } = 1;
        public ushort MinVer { get; set; } = 0;

        //private ArrayList subItems = new ArrayList();
      private readonly List<PEResourceElement> elements = new List<PEResourceElement>();

      public int Count() { return elements.Count; }

      /// <summary>
      /// Programmatically create unmanaged resource.
      /// </summary>
      public PEResourceDirectory() { }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(elements != null);
        }

      /// <summary>
      /// Read unmanged resource directory structure from PE-file.
      /// </summary>
      /// <param name="reader"></param>
      internal void PopulateResourceDirectory(PEReader reader, long baseOffset) {
          Contract.Requires(reader != null);
          PEResourceElement resElement;

          int junk = reader.ReadInt32(); // Must be zero.
        this.Date = reader.ReadUInt32();    // Time stamp.
        this.MajVer = reader.ReadUInt16();
        this.MinVer = reader.ReadUInt16();

        int numNameEntries = reader.ReadUInt16(); // Number of named entries.
        int numIdntEntries = reader.ReadUInt16(); // Number of ID entries.
        for (int i = 0; i < numNameEntries; i++) {
          uint nameOrId = reader.ReadUInt32();
          uint elemOfst = reader.ReadUInt32();
          if ((elemOfst & 0x80000000) != 0) // Element is subdirectory.
            resElement = new PEResourceDirectory();
          else
            resElement = new PEResourceData();
          resElement.Name = ReadName(reader, baseOffset + nameOrId & 0x7fffffff);
          resElement.offset = baseOffset + (long)(elemOfst & 0x7fffffff);
          this.AddElement(resElement);
        }
        // Read the Ident entries.
        for (int i = 0; i < numIdntEntries; i++) {
          uint nameOrId = reader.ReadUInt32();
          uint elemOfst = reader.ReadUInt32();
          if ((elemOfst & 0x80000000) != 0) // Element is subdirectory.
            resElement = new PEResourceDirectory();
          else
            resElement = new PEResourceData();
          resElement.Id = (ushort)nameOrId;
          resElement.offset = baseOffset + (long)(elemOfst & 0x7fffffff);
          this.AddElement(resElement);
        }
        // Now recurse to get subdirectories/the real data.
        foreach (PEResourceElement elem in this.elements)
        {
            PEResourceDirectory resDirectory;
            if ((resDirectory = elem as PEResourceDirectory) != null) {
            reader.BaseStream.Seek(resDirectory.offset, SeekOrigin.Begin);
            resDirectory.PopulateResourceDirectory(reader, baseOffset);
          }
          else
            {
                PEResourceData resData;
                if ((resData = elem as PEResourceData) != null) {
                    reader.BaseStream.Seek(resData.offset, SeekOrigin.Begin);
                    resData.PopulateResourceData(reader, baseOffset);
                }
            }
        }
      }

      private static string ReadName(BinaryReader rdr, long offset) {
          Contract.Requires(rdr != null);
          Contract.Ensures(Contract.Result<string>() != null);
          long savedPos = rdr.BaseStream.Position;
        rdr.BaseStream.Seek(offset, SeekOrigin.Begin);
        ushort nLength = rdr.ReadUInt16();
        char[] name = new char[nLength];
        for (int i = 0; i < nLength; i++)
          name[i] = (char)rdr.ReadUInt16();
        rdr.BaseStream.Seek(savedPos, SeekOrigin.Begin);
        return new string(name);
      }

      public bool HasData() {
        return elements.Count > 0;
      }

      public void AddElement(PEResourceElement el)
      {
          Contract.Requires(el != null);
          //subItems.Add(el);
        elements.Add(el);
      }

      /// <summary>
      /// Total file-space size of all child elements
      /// </summary>
      private uint subSize;

      /// <summary>
      /// File-space needed for all names of this directory
      /// </summary>
      private uint nameSize;

      /// <summary>
      /// File-space taken up by this directory
      /// </summary>
      private uint dirSize;

      /// <summary>
      /// Number of named elements.  These come first in list.
      /// </summary>
      private uint numNamed;

      private uint numIds { get { return (uint)elements.Count - numNamed; } }

      protected internal override uint Size() {
        nameSize = 0;
        numNamed = 0;
        subSize = 0;
        //for (int i = 0; i < subItems.Count; i++) 
        foreach (PEResourceElement elem in this.elements) {
          subSize += elem.Size();
          if (elem.Name != null) {
            nameSize += 2 + (uint)elem.Name.Length * 2;
            numNamed++;
          }
        }
        dirSize = (uint)elements.Count * EntrySize + HeaderSize;
        return dirSize + nameSize + subSize;
      }

      /// <summary>
      /// Write out the unmanaged resource rooted at this directory.
      /// </summary>
      /// <param name="dest">The Binary Writer</param>
      /// <param name="RVA">RVA of this .rsrc section</param>
      internal void Write(BinaryWriter dest, uint RVA) {
          Contract.Requires(dest != null);
          Size();
        dest.Flush();
        uint baseOffset = (uint)dest.BaseStream.Position;
        this.Write(dest, baseOffset, 0, RVA);
      }

      protected internal override void Write(BinaryWriter dest, uint baseOffset, uint currentOffset, uint RVA) { 
        uint nameOffset = currentOffset + this.dirSize;
        uint targetOffset = currentOffset + this.dirSize;
        dest.Write((uint)0); // characteristics
        dest.Write(Date);    // datetime
        dest.Write(MajVer);
        dest.Write(MinVer);
        dest.Write((ushort)numNamed);
        dest.Write((ushort)numIds);
        currentOffset += HeaderSize;

        // Write out the named items.
        foreach (PEResourceElement elem in elements) { 
          if (elem.Name != null) {
            dest.Write((uint)(nameOffset | 0x80000000));
            if (elem is PEResourceDirectory)
              dest.Write((uint)(targetOffset | 0x80000000));
            else
              dest.Write((uint)targetOffset);
            nameOffset += 2 + (uint)elem.Name.Length * 2;
            targetOffset += (uint)elem.Size();
            currentOffset += EntrySize;
          }
        }

        // Write out the items with ID.
        foreach (PEResourceElement elem in elements) { 
          if (elem.Name == null) {
            dest.Write(elem.Id);
            if (elem is PEResourceDirectory)
              dest.Write((uint)(targetOffset | 0x80000000));
            else
              dest.Write((uint)targetOffset);
            currentOffset += EntrySize;
            targetOffset += elem.Size();
          }
        }

        // Write out the name strings.
        foreach (PEResourceElement elem in elements) {
          string s = elem.Name;
          if (s != null) {
            dest.Write((ushort)s.Length);
            byte[] b = System.Text.Encoding.Unicode.GetBytes(s);
            dest.Write(b);
          }
        }
        currentOffset += this.nameSize;

        // Now recurse to the children.
        foreach (PEResourceElement elem in elements) {
          elem.Write(dest, baseOffset, currentOffset, RVA);
          currentOffset += elem.Size();
        }
      }
    }

    public class PEResourceData : PEResourceElement
    {
        public PEResourceData() { }

        public int CodePage { get; set; } = 0;

        public byte[] Data { get; set; }

        protected internal override uint Size()
        {
            return 16 + (uint)Data.Length;
        }

      /// <summary>
      /// Read the binary data from the PE file.
      /// </summary>
      /// <param name="reader"></param>
        internal void PopulateResourceData(PEReader reader, long baseOffset) {
          Contract.Requires(reader != null);
          uint dataRVA = reader.ReadUInt32();
          int dataLength = reader.ReadInt32();
          this.CodePage = reader.ReadInt32();
          uint junk = reader.ReadUInt32(); // Must be zero.
          reader.BaseStream.Seek(reader.GetOffset(dataRVA), SeekOrigin.Begin);
          Data = new byte[dataLength];
          int numberRead = reader.BaseStream.Read(Data, 0, dataLength);
        }

        protected internal override void Write(BinaryWriter dest, uint baseOffset, uint currentOffset, uint RVA)
        {
            dest.Write((uint)(currentOffset + HeaderSize) + RVA);
            dest.Write((uint)Data.Length);
            dest.Write((uint)CodePage);
            dest.Write((uint)0);
            dest.Write(Data);
        }
    }
}