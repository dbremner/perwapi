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
using System.Diagnostics.Contracts;
using System.IO;

namespace QUT.PERWAPI
{

    /**************************************************************************/
    // Class to describe procedure locals
    /**************************************************************************/
    /// <summary>
    /// Descriptor for a local of a method
    /// </summary>
    public class Local
    {
        private static readonly byte PINNED = 0x45;
        public Type type;
        private int index = 0;

        /*-------------------- Constructors ---------------------------------*/

        /// <summary>
        /// Create a new local variable 
        /// </summary>
        /// <param name="lName">name of the local variable</param>
        /// <param name="lType">type of the local variable</param>
        public Local(string lName, Type lType)
        {
            Contract.Requires(lName != null);
            Contract.Requires(lType != null);
            Name = lName;
            type = lType;
        }

        /// <summary>
        /// Create a new local variable that is byref and/or pinned
        /// </summary>
        /// <param name="lName">local name</param>
        /// <param name="lType">local type</param>
        /// <param name="isPinned">has pinned attribute</param>
        public Local(string lName, Type lType, bool isPinned)
        {
            Name = lName;
            type = lType;
            Pinned = isPinned;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Name != null);
            Contract.Invariant(type != null);
        }

        public int GetIndex() { return index; }

        /// <summary>
        /// The name of the local variable.
        /// </summary>
        public string Name { get; }

        public bool Pinned { get; set; } = false;

        /// <summary>
        /// Gets the signature for this local variable.
        /// </summary>
        /// <returns>A byte array of the signature.</returns>
        public byte[] GetSig()
        {
            MemoryStream str = new MemoryStream();
            type.TypeSig(str);
            return str.ToArray();
        }

        internal void SetIndex(int ix)
        {
            index = ix;
        }

        internal void TypeSig(MemoryStream str)
        {
            Contract.Requires(str != null);
            if (Pinned) str.WriteByte(PINNED);
            type.TypeSig(str);
        }

        internal void BuildTables(MetaDataOut md)
        {
            Contract.Requires(md != null);
            if (!(type is ClassDef))
                type.BuildMDTables(md);
        }

        internal void BuildCILInfo(CILWriter output)
        {
            Contract.Requires(output != null);
            if (!(type is ClassDef))
                type.BuildCILInfo(output);
        }

        internal void Write(CILWriter output)
        {
            Contract.Requires(output != null);
            type.WriteType(output);
            output.Write("\t" + Name);
        }

    }
}
