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
    /**************************************************************************/
    /// <summary>
    /// Descriptor for the address of a field's value in the PE file
    /// </summary>
    public class FieldRVA : MetaDataElement
    {
        private FieldDef field;
        private readonly DataConstant data;
        private readonly uint rva = 0;
        private readonly uint fieldIx = 0;

        /*-------------------- Constructors ---------------------------------*/

        internal FieldRVA(FieldDef field, DataConstant data)
        {
            Contract.Requires(field != null);
            Contract.Requires(data != null);
            this.field = field;
            this.data = data;
            tabIx = MDTable.FieldRVA;
        }

        internal FieldRVA(PEReader buff)
        {
            Contract.Requires(buff != null);
            rva = buff.ReadUInt32();
            fieldIx = buff.GetIndex(MDTable.Field);
            tabIx = MDTable.FieldRVA;
        }

        internal static void Read(PEReader buff, TableRow[] fRVAs)
        {
            Contract.Requires(buff != null);
            Contract.Requires(fRVAs != null);
            for (int i = 0; i < fRVAs.Length; i++)
                fRVAs[i] = new FieldRVA(buff);
        }

        internal sealed override void Resolve(PEReader buff)
        {
            field = (FieldDef)buff.GetElement(MDTable.Field, fieldIx);
            field.AddDataValue(buff.GetDataConstant(rva, field.GetFieldType()));
        }

        internal sealed override void BuildTables(MetaDataOut md)
        {
            md.AddToTable(MDTable.FieldRVA, this);
            md.AddData(data);
        }

        internal static uint Size(MetaData md)
        {
            Contract.Requires(md != null);
            return 4 + md.TableIndexSize(MDTable.Field);
        }

        internal sealed override void Write(PEWriter output)
        {
            output.WriteDataRVA(data.DataOffset);
            output.WriteIndex(MDTable.Field, field.Row);
        }

    }


}