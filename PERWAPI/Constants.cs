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
using System.Linq;
using JetBrains.Annotations;

namespace QUT.PERWAPI
{

    /**************************************************************************/
    // Classes used to describe constant values
    /**************************************************************************/
    /// <summary>
    /// Descriptor for a constant value, to be written in the blob heap
    /// </summary>
    public abstract class Constant
    {
        protected uint size = 0;
        internal ElementType type;
        protected uint blobIndex;
        internal MetaDataOut addedToBlobHeap;

        /*-------------------- Constructors ---------------------------------*/

        internal Constant() { }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(addedToBlobHeap != null);
        }

        internal virtual uint GetBlobIndex(MetaDataOut md)
        {
            Contract.Requires(md != null);
            return 0;
        }

        internal uint GetSize() { return size; }

        internal byte GetTypeIndex() { return (byte)type; }

        internal virtual void Write(BinaryWriter bw)
        {
            Contract.Requires(bw != null);
        }

        internal virtual void Write(CILWriter output)
        {
            Contract.Requires(output != null);
            throw new NotYetImplementedException("Constant values for CIL");
        }

    }

    /**************************************************************************/
    public abstract class BlobConstant : Constant { }
    /**************************************************************************/
    /// <summary>
    /// Boolean constant
    /// </summary>
    public class BoolConst : BlobConstant
    {
        private readonly bool val;

        /*-------------------- Constructors ---------------------------------*/

        /// <summary>
        /// Create a new boolean constant with the value "val"
        /// </summary>
        /// <param name="val">value of this boolean constant</param>
        public BoolConst(bool val)
        {
            this.val = val;
            size = 1;
            type = ElementType.Boolean;
        }

        public bool GetBool()
        {
            return val;
        }

        internal sealed override uint GetBlobIndex(MetaDataOut md)
        {
            if (addedToBlobHeap != md)
            {
                if (val) blobIndex = md.AddToBlobHeap(1, 1);
                else blobIndex = md.AddToBlobHeap(0, 1);
                addedToBlobHeap = md;
            }
            return blobIndex;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            if (val) bw.Write((sbyte)1);
            else bw.Write((sbyte)0);
        }

    }
    /**************************************************************************/
    public class CharConst : BlobConstant
    {
        private readonly char val;

        /*-------------------- Constructors ---------------------------------*/

        public CharConst(char val)
        {
            this.val = val;
            size = 2;
            type = ElementType.Char;
        }

        internal CharConst(PEReader buff)
        {
            Contract.Requires(buff != null);
            val = buff.ReadChar();
            size = 2;
            type = ElementType.Char;
        }

        public char GetChar()
        { // KJG addition 2005-Mar-01
            return val;
        }

        internal sealed override uint GetBlobIndex(MetaDataOut md)
        {
            if (addedToBlobHeap != md)
            {
                blobIndex = md.AddToBlobHeap(val);
                addedToBlobHeap = md;
            }
            return blobIndex;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            bw.Write(val);
        }

    }

    /**************************************************************************/
    public class NullRefConst : BlobConstant
    {

        /*-------------------- Constructors ---------------------------------*/

        public NullRefConst()
        {
            size = 4;
            type = ElementType.Class;
        }

        internal NullRefConst(PEReader buff)
        {
            Contract.Requires(buff != null);
            uint junk = buff.ReadUInt32();
            size = 4;
            type = ElementType.Class;
        }

        internal sealed override uint GetBlobIndex(MetaDataOut md)
        {
            if (addedToBlobHeap != md)
            {
                blobIndex = md.AddToBlobHeap(0, 4);
                addedToBlobHeap = md;
            }
            return blobIndex;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            bw.Write((int)0);
        }

    }

    /**************************************************************************/
    /// <summary>
    /// Constant array
    /// </summary>
    public class ArrayConst : BlobConstant
    {
        private readonly Constant[] elements;

        /*-------------------- Constructors ---------------------------------*/

        public ArrayConst(Constant[] elems)
        {
            Contract.Requires(elems != null);
            type = ElementType.SZArray;
            size = 5;  // one byte for SZARRAY, 4 bytes for length
            elements = elems;
            foreach (Constant elem in elements)
            {
                size += elem.GetSize();
            }
        }

        public Constant[] GetArray()
        {
            return elements;
        }

        internal sealed override uint GetBlobIndex(MetaDataOut md)
        {
            if (addedToBlobHeap != md)
            {
                MemoryStream str = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(str);
                Write(bw);
                blobIndex = md.AddToBlobHeap(str.ToArray());
                addedToBlobHeap = md;
            }
            return blobIndex;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            bw.Write((byte)type);
            bw.Write(elements.Length);
            foreach (Constant elem in elements)
            {
                elem.Write(bw);
            }
        }

    }

    /**************************************************************************/
    public class ClassTypeConst : BlobConstant
    {
        private string name;
        private readonly Class desc;

        /*-------------------- Constructors ---------------------------------*/

        public ClassTypeConst(string className)
        {
            Contract.Requires(className != null);
            name = className;
            type = ElementType.ClassType;
        }

        public ClassTypeConst(Class classDesc)
        {
            Contract.Requires(classDesc != null);
            desc = classDesc;
            type = ElementType.ClassType;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant((name != null) || (desc != null) );
        }

        public Class GetClass()
        {
            return desc;
        }

        public String GetClassName()
        {
            if (name == null) name = desc.TypeName();
            // CHECK - ClassName or TypeName
            // if (name == null) return desc.ClassName();
            return name;
        }

        internal override void Write(BinaryWriter bw)
        {
            if (name == null) name = desc.TypeName();
            // CHECK - ClassName or TypeName
            // if (name == null)  name = desc.ClassName();
            bw.Write(name);
        }

    }

    /**************************************************************************/
    public class BoxedSimpleConst : BlobConstant
    {
        private readonly SimpleConstant sConst;

        /*-------------------- Constructors ---------------------------------*/

        public BoxedSimpleConst(SimpleConstant con)
        {
            Contract.Requires(con != null);
            sConst = con;
            type = (ElementType)sConst.GetTypeIndex();
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(sConst != null);
        }

        public SimpleConstant GetConst()
        {
            Contract.Ensures(Contract.Result<SimpleConstant>() != null);
            return sConst;
        }

        internal override void Write(BinaryWriter bw)
        {
            bw.Write((byte)type);
            sConst.Write(bw);
        }
    }
    /**************************************************************************/
    /// <summary>
    /// Descriptor for a constant value
    /// </summary>
    public abstract class DataConstant : Constant
    {
        /*-------------------- Constructors ---------------------------------*/

        internal DataConstant() { }

        public uint DataOffset { get; set; } = 0;
    }

    /**************************************************************************/
    /// <summary>
    /// Constant for a memory address
    /// </summary>
    public class AddressConstant : DataConstant
    {
        private readonly DataConstant data;

        /*-------------------- Constructors ---------------------------------*/

        public AddressConstant(DataConstant dConst)
        {
            data = dConst;
            size = 4;
            type = ElementType.TypedByRef;
        }

        internal AddressConstant(PEReader buff)
        {
            Contract.Requires(buff != null);
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(data != null);
        }

        public DataConstant GetConst()
        {
            return data;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            ((PEWriter)bw).WriteDataRVA(data.DataOffset);
        }

    }

    /**************************************************************************/
    public class ByteArrConst : DataConstant
    {
        private readonly byte[] val;

        /*-------------------- Constructors ---------------------------------*/

        public ByteArrConst(byte[] val)
        {
            Contract.Requires(val != null);
            this.val = val;
            size = (uint)val.Length;
        }

        public byte[] GetArray()
        {
            return val;
        }

        internal sealed override uint GetBlobIndex(MetaDataOut md)
        {
            if (addedToBlobHeap != md)
            {
                blobIndex = md.AddToBlobHeap(val);
                addedToBlobHeap = md;
            }
            return blobIndex;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            bw.Write(val);
        }

    }

    /**************************************************************************/
    public class RepeatedConstant : DataConstant
    {
        private readonly DataConstant data;
        private readonly uint repCount;

        /*-------------------- Constructors ---------------------------------*/

        public RepeatedConstant(DataConstant dConst, int repeatCount)
        {
            Contract.Requires(dConst != null);
            data = dConst;
            repCount = (uint)repeatCount;
            type = ElementType.SZArray;
            size = data.GetSize() * repCount;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(data != null);
        }

        public DataConstant GetConst()
        {
            return data;
        }

        public uint GetCount()
        {
            return repCount;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            for (int i = 0; i < repCount; i++)
            {
                data.Write(bw);
            }
        }

    }

    /**************************************************************************/
    public class StringConst : DataConstant
    {
        private readonly string val;
        private readonly byte[] strBytes;

        /*-------------------- Constructors ---------------------------------*/

        public StringConst(string val)
        {
            Contract.Requires(val != null);
            this.val = val;
            size = (uint)val.Length;  // need to add null ??
            type = ElementType.String;
        }

        internal StringConst(byte[] sBytes)
        {
            Contract.Requires(sBytes != null);
            strBytes = sBytes;
            size = (uint)strBytes.Length;
            type = ElementType.String;
        }

        public string GetString()
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return val;
        }

        public byte[] GetStringBytes()
        {
            return strBytes;
        }

        internal sealed override uint GetBlobIndex(MetaDataOut md)
        {
            if (addedToBlobHeap != md)
            {
                if (val == null)
                    blobIndex = md.AddToBlobHeap(strBytes);
                else
                    blobIndex = md.AddToBlobHeap(val);
                addedToBlobHeap = md;
            }
            return blobIndex;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            if ((val == null) && (strBytes != null))
            {
                bw.Write(strBytes);
            }
            else
                bw.Write(val);
        }

    }

    /**************************************************************************/
    public abstract class SimpleConstant : DataConstant { }

    /**************************************************************************/
    public class IntConst : SimpleConstant
    {
        private readonly long val;

        /*-------------------- Constructors ---------------------------------*/

        public IntConst(sbyte val)
        {
            this.val = val;
            size = 1; //8;
            type = ElementType.I8;
        }

        public IntConst(short val)
        {
            this.val = val;
            size = 2; //16;
            type = ElementType.I2;
        }

        public IntConst(int val)
        {
            this.val = val;
            size = 4; //32;
            type = ElementType.I4;
        }

        public IntConst(long val)
        {
            this.val = val;
            size = 8; //64;
            type = ElementType.I8;
        }

        internal IntConst(PEReader buff, int numBytes)
        {
            Contract.Requires(buff != null);
            switch (numBytes)
            {
                case (1): val = buff.ReadSByte();
                    type = ElementType.I8;
                    break;
                case (2): val = buff.ReadInt16();
                    type = ElementType.I2;
                    break;
                case (4): val = buff.ReadInt32();
                    type = ElementType.I4;
                    break;
                case (8): val = buff.ReadInt64();
                    type = ElementType.I8;
                    break;
                default: val = 0;
                    break;
            }
            size = (uint)numBytes; // * 4;
        }

        public int GetIntSize()
        {
            return (int)size;
        }

        public ElementType GetIntType()
        {
            return type;
        }

        public int GetInt()
        {
            if (size < 8)
                return (int)val;
            else
                throw new Exception("Constant is long");
        }

        public long GetLong()
        {
            return val;
        }

        internal sealed override uint GetBlobIndex(MetaDataOut md)
        {
            if (addedToBlobHeap != md)
            {
                blobIndex = md.AddToBlobHeap(val, size);
                //switch (size) {
                //  case (1) : md.AddToBlobHeap((sbyte)val); break;
                //  case (2) : md.AddToBlobHeap((short)val); break;
                //  case (4) : md.AddToBlobHeap((int)val); break;
                //  default : md.AddToBlobHeap(val); break; 
                //}
                addedToBlobHeap = md;
            }
            return blobIndex;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            switch (size)
            {
                case (1): bw.Write((sbyte)val); break;
                case (2): bw.Write((short)val); break;
                case (4): bw.Write((int)val); break;
                default: bw.Write(val); break;
            }
        }

    }

    /**************************************************************************/
    public class UIntConst : SimpleConstant
    {
        private readonly ulong val;

        /*-------------------- Constructors ---------------------------------*/

        public UIntConst(byte val)
        {
            this.val = val;
            size = 1;
            type = ElementType.U8;
        }
        public UIntConst(ushort val)
        {
            this.val = val;
            size = 2;
            type = ElementType.U2;
        }
        public UIntConst(uint val)
        {
            this.val = val;
            size = 4;
            type = ElementType.U4;
        }
        public UIntConst(ulong val)
        {
            this.val = val;
            size = 8;
            type = ElementType.U8;
        }

        public int GetIntSize()
        {
            return (int)size;
        }

        public ElementType GetUIntType()
        {
            return type;
        }

        public uint GetUInt()
        {
            return (uint)val;
        }

        public ulong GetULong()
        {
            return val;
        }

        public long GetLong()
        {           // KJG addition
            if (val <= (ulong)(System.Int64.MaxValue))
                return (long)val;
            else
                throw new Exception("UInt Constant too large");
        }

        public long GetULongAsLong()
        {   // KJG addition
            return (long)val;
        }

        internal sealed override uint GetBlobIndex(MetaDataOut md)
        {
            if (addedToBlobHeap != md)
            {
                blobIndex = md.AddToBlobHeap(val, size);
                //switch (size) {
                //  case (1) : blobIndex = md.AddToBlobHeap((byte)val); break;
                //  case (2) : blobIndex = md.AddToBlobHeap((ushort)val); break;
                //  case (4) : blobIndex = md.AddToBlobHeap((uint)val); break;
                //  default : blobIndex = md.AddToBlobHeap(val); break;
                //}
                addedToBlobHeap = md;
            }
            return blobIndex;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            switch (size)
            {
                case (1): bw.Write((byte)val); break;
                case (2): bw.Write((ushort)val); break;
                case (4): bw.Write((uint)val); break;
                default: bw.Write(val); break;
            }
        }
    }

    /**************************************************************************/
    public class FloatConst : SimpleConstant
    {
        private readonly float val;

        /*-------------------- Constructors ---------------------------------*/

        public FloatConst(float val)
        {
            this.val = val;
            size = 4;
            type = ElementType.R4;
        }

        internal FloatConst(PEReader buff)
        {
            Contract.Requires(buff != null);
            val = buff.ReadSingle();
            size = 4;
            type = ElementType.R4;
        }

        public float GetFloat()
        {
            return val;
        }

        public double GetDouble()
        { // KJG addition 2005-Mar-01
            return (double)val;
        }

        internal sealed override uint GetBlobIndex(MetaDataOut md)
        {
            if (addedToBlobHeap != md)
            {
                blobIndex = md.AddToBlobHeap(val);
                addedToBlobHeap = md;
            }
            return blobIndex;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            bw.Write(val);
        }

    }

    /**************************************************************************/
    public class DoubleConst : SimpleConstant
    {
        private readonly double val;

        /*-------------------- Constructors ---------------------------------*/

        public DoubleConst(double val)
        {
            this.val = val;
            size = 8;
            type = ElementType.R8;
        }

        internal DoubleConst(PEReader buff)
        {
            Contract.Requires(buff != null);
            val = buff.ReadDouble();
            size = 8;
            type = ElementType.R8;
        }

        public double GetDouble()
        { // KJG addition 2005-Mar-01
            return val;
        }

        internal sealed override uint GetBlobIndex(MetaDataOut md)
        {
            if (addedToBlobHeap != md)
            {
                blobIndex = md.AddToBlobHeap(val);
                addedToBlobHeap = md;
            }
            return blobIndex;
        }

        internal sealed override void Write(BinaryWriter bw)
        {
            bw.Write(val);
        }

    }
}
