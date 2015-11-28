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
using System.Diagnostics.Contracts;
using System.Security.Cryptography;


namespace QUT.PERWAPI
{
    /*****************************************************************************/
    /// <summary>
    /// Marshalling information for a field or param
    /// </summary>
    public class FieldMarshal : MetaDataElement
    {
        private MetaDataElement field;
        private NativeType nt;
        private uint ntIx;
        private readonly uint parentIx;

        /*-------------------- Added by Carlo Kok ---------------------------------*/

        public SafeArrayType SafeArraySubType { get; set; }

        public string SafeArrayUserDefinedSubType { get; set; }

        public NativeTypeIx ArraySubType { get; set; } = (NativeTypeIx)0x50; // default, important

        public int SizeConst { get; set; } = -1;

        public int SizeParamIndex { get; set; } = -1;

        public string CustomMarshallingType { get; set; }

        public string CustomMarshallingCookie { get; set; }

        /*-------------------- Constructors ---------------------------------*/

        internal FieldMarshal(MetaDataElement field, NativeType nType)
        {
            Contract.Requires(field != null);
            this.field = field;
            this.nt = nType;
            tabIx = MDTable.FieldMarshal;
        }

        internal FieldMarshal(PEReader buff)
        {
            Contract.Requires(buff != null);
            parentIx = buff.GetCodedIndex(CIx.HasFieldMarshal);
            ntIx = buff.GetBlobIx();
            tabIx = MDTable.FieldMarshal;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(field != null);
        }

        internal static void Read(PEReader buff, TableRow[] fMarshal)
        {
            Contract.Requires(buff != null);
            for (int i = 0; i < fMarshal.Length; i++)
                fMarshal[i] = new FieldMarshal(buff);
        }

        internal override void Resolve(PEReader buff)
        {
            field = buff.GetCodedElement(CIx.HasFieldMarshal, parentIx);
            nt = buff.GetBlobNativeType(ntIx);
            if (field is FieldDef)
            {
                ((FieldDef)field).SetMarshalType(nt);
            }
            else
            {
                ((Param)field).SetMarshalType(nt);
            }
        }

        internal override uint SortKey()
        {
            return (field.Row << MetaData.CIxShiftMap[(uint)CIx.HasFieldMarshal])
                | field.GetCodedIx(CIx.HasFieldMarshal);
        }

        internal sealed override void BuildTables(MetaDataOut md)
        {
            md.AddToTable(MDTable.FieldMarshal, this);
            ntIx = md.AddToBlobHeap(nt.ToBlob());
        }

        internal static uint Size(MetaData md)
        {
            return md.CodedIndexSize(CIx.HasFieldMarshal) + md.BlobIndexSize();
        }

        internal sealed override void Write(PEWriter output)
        {
            output.WriteCodedIndex(CIx.HasFieldMarshal, field);
            output.BlobIndex(ntIx);
        }

    }


}