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
using System.Linq;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using JetBrains.Annotations;


namespace QUT.PERWAPI
{
    /**************************************************************************/
    /// <summary>
    /// The base descriptor for a class 
    /// </summary>
    public abstract class Class : Type
    {
        //protected int row = 0;
        protected uint nameIx, nameSpaceIx;
        protected ArrayList nestedClasses = new ArrayList();
        protected bool special = false;
        protected ArrayList fields = new ArrayList();
        protected ArrayList methods = new ArrayList();
        internal uint fieldIx = 0, methodIx = 0, fieldEndIx = 0, methodEndIx = 0;
        protected string[] fieldNames, methodNames;
        protected ArrayList genericParams = new ArrayList();

        /*-------------------- Constructors ---------------------------------*/

        internal Class() : base((byte)ElementType.Class) { }

        /*------------------------- public set and get methods --------------------------*/

        public virtual void MakeValueClass()
        {
            typeIndex = (byte)ElementType.ValueType;
        }

        /// <summary>
        /// Get the name of this class
        /// </summary>
        /// <value>class name</value>
        public string Name { get; protected set; }

        /// <summary>
        /// Get the namespace that includes this class
        /// </summary>
        /// <value>namespace name</value>
        public string NameSpace { get; protected set; }

        /// <summary>
        /// Get the string representation of the qualified name
        /// of this class
        /// </summary>
        /// <returns>class qualified name</returns>
        public override string TypeName()
        {
            if (String.IsNullOrEmpty(NameSpace)) return Name;
            return NameSpace + "." + Name;
        }

        /// <summary>
        /// Get the descriptor for the method "name" of this class
        /// </summary>
        /// <param name="name">The name of the method to be retrieved</param>
        /// <returns>The method descriptor for "name"</returns>
        public Method GetMethodDesc(string name)
        {
            Contract.Requires(name != null);
            foreach (object method in methods)
            {
                if (((Method)method).HasName(name))
                    return (Method)method;
            }
            return null;
        }

        /// <summary>
        /// Get the descriptor for the method called "name" with the signature "parTypes"
        /// </summary>
        /// <param name="name">The name of the method</param>
        /// <param name="parTypes">The signature of the method</param>
        /// <returns>The method descriptor for name(parTypes)</returns>
        [CanBeNull]
        public Method GetMethodDesc(string name, Type[] parTypes)
        {
            Contract.Requires(name != null);
            Contract.Requires(parTypes != null);
            foreach (object method in methods)
            {
                if (((Method)method).HasNameAndSig(name, parTypes))
                    return (Method)method;
            }
            return null;
        }

        /// <summary>
        /// Get the vararg method "name(parTypes,optTypes)" for this class
        /// </summary>
        /// <param name="name">Method name</param>
        /// <param name="parTypes">Method parameter types</param>
        /// <param name="optParTypes">Optional parameter types</param>
        /// <returns>Descriptor for "name(parTypes,optTypes)"</returns>
        public Method GetMethodDesc(string name, Type[] parTypes, Type[] optParTypes)
        {
            Contract.Requires(name != null);
            Contract.Requires(parTypes != null);
            Contract.Requires(optParTypes != null);
            foreach (object method in methods)
            {
                if (((Method)method).HasNameAndSig(name, parTypes, optParTypes))
                    return (Method)method;
            }
            return null;
        }

        /// <summary>
        /// Get all the methods of this class called "name"
        /// </summary>
        /// <param name="name">The method name</param>
        /// <returns>List of methods called "name"</returns>
        public Method[] GetMethodDescs(string name)
        {
            Contract.Requires(name != null);
            ArrayList meths = GetMeths(name);
            return (Method[])meths.ToArray(typeof(Method));
        }

        /// <summary>
        /// Get all the methods for this class
        /// </summary>
        /// <returns>List of methods for this class</returns>
        public Method[] GetMethodDescs()
        {
            return (Method[])methods.ToArray(typeof(Method));
        }

        /// <summary>
        /// Remove the specified method from this class
        /// </summary>
        /// <param name="name">method name</param>
        public void RemoveMethod(string name)
        {
            Contract.Requires(name != null);
            Method meth = GetMethodDesc(name);
            if (meth != null) methods.Remove(meth);
        }

        /// <summary>
        /// Remove the specified method from this class
        /// </summary>
        /// <param name="name">method name</param>
        /// <param name="parTypes">method parameter types</param>
        public void RemoveMethod(string name, Type[] parTypes)
        {
            Contract.Requires(name != null);
            Contract.Requires(parTypes != null);
            Method meth = GetMethodDesc(name, parTypes);
            if (meth != null) methods.Remove(meth);
        }

        /// <summary>
        /// Remove the specified method from this class
        /// </summary>
        /// <param name="name">method name</param>
        /// <param name="parTypes">method parameter types</param>
        /// <param name="optTypes">optional method parameter types</param>
        public void RemoveMethod(string name, Type[] parTypes, Type[] optTypes)
        {
            Contract.Requires(name != null);
            Contract.Requires(parTypes != null);
            Contract.Requires(optTypes != null);
            Method meth = GetMethodDesc(name, parTypes, optTypes);
            if (meth != null) methods.Remove(meth);
        }

        /// <summary>
        /// Remove the specified method from this class
        /// </summary>
        /// <param name="meth">method descriptor</param>
        public void RemoveMethod(Method meth)
        {
            Contract.Requires(meth != null);
            methods.Remove(meth);
        }

        /// <summary>
        /// Remove the specified method from this class
        /// </summary>
        /// <param name="ix">index into list of methods for specified method</param>
        public void RemoveMethod(int ix)
        {
            methods.RemoveAt(ix);
        }

        /// <summary>
        /// Get the descriptor for the field "name" for this class
        /// </summary>
        /// <param name="name">Field name</param>
        /// <returns>Descriptor for field "name"</returns>
        public Field GetFieldDesc(string name)
        {
            Contract.Requires(name != null);
            return FindField(name);
        }

        /// <summary>
        /// Get all the fields for this class
        /// </summary>
        /// <returns>List of fields for this class</returns>
        public Field[] GetFieldDescs()
        {
            return (Field[])fields.ToArray(typeof(Field));
        }

        /// <summary>
        /// Remove the specified field from this class
        /// </summary>
        /// <param name="name">field name</param>
        public void RemoveField(string name)
        {
            Contract.Requires(name != null);
            Field f = FindField(name);
            if (f != null) fields.Remove(f);
        }

        /// <summary>
        /// Instantiate this generic type with the supplied types
        /// </summary>
        /// <param name="genTypes">types to instantiate with</param>
        /// <returns>descriptor for instantiated generic type</returns>
        public virtual ClassSpec Instantiate(Type[] genTypes)
        {
            Contract.Requires(genTypes != null);
            return new ClassSpec(this, genTypes);
        }

        /// <summary>
        /// Denote this class as "special" such as a default module class
        /// </summary>
        public virtual void MakeSpecial()
        {
            special = true;
        }

        /// <summary>
        /// Get the owing scope of this class
        /// </summary>
        /// <returns>owner of this class</returns>
        public abstract MetaDataElement GetParent();

        /// <summary>
        /// Get any nested classes of this class
        /// </summary>
        /// <returns>list of nested classes</returns>
        public Class[] GetNestedClasses()
        {
            return (Class[])nestedClasses.ToArray(typeof(Class));
        }

        /// <summary>
        /// How many nested classes does this class have?
        /// </summary>
        /// <returns>number of nested classes</returns>
        public int GetNestedClassCount()
        {
            return nestedClasses.Count;
        }

        /*------------------------- internal functions --------------------------*/

        internal virtual Type GetGenPar(uint ix) { return null; }

        protected ArrayList GetMeths(string name)
        {
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<ArrayList>() != null);
            ArrayList meths = new ArrayList();
            foreach (object method in methods)
            {
                if (((Method)method).HasName(name))
                    meths.Add(method);
            }
            return meths;
        }

        internal ArrayList GetFieldList() { return fields; }

        internal ArrayList GetMethodList() { return methods; }

        internal bool isValueType()
        {
            return typeIndex == (byte)ElementType.ValueType;
        }

        internal bool isSpecial() { return special; }

        internal void AddToFieldList(Field f)
        {
            Contract.Requires(f != null);
            f.SetParent(this);
            fields.Add(f);
        }

        internal void AddToList(ArrayList list, MDTable tabIx)
        {
            Contract.Requires(list != null);
            switch (tabIx)
            {
                case (MDTable.Field): fields.AddRange(list); break;
                case (MDTable.Method): methods.AddRange(list); break;
                case (MDTable.TypeDef): nestedClasses.AddRange(list); break;
                default: throw new Exception("Unknown list type");
            }
        }

        internal void AddToMethodList(Method m)
        {
            Contract.Requires(m != null);
            m.SetParent(this);
            methods.Add(m);
        }

        internal void AddToClassList(Class nClass)
        {
            Contract.Requires(nClass != null);
            nestedClasses.Add(nClass);
        }

        [CanBeNull]
        internal Class GetNested(string name)
        {
            Contract.Requires(name != null);
            for (int i = 0; i < nestedClasses.Count; i++)
            {
                if (((Class)nestedClasses[i]).Name == name)
                    return (Class)nestedClasses[i];
            }
            return null;
        }

        internal Method GetMethod(MethSig mSig)
        {
            Contract.Requires(mSig != null);
            return GetMethodDesc(mSig.name, mSig.parTypes, mSig.optParTypes);
        }

        protected Field FindField(string name)
        {
            Contract.Requires(fields != null);
            for (int i = 0; i < fields.Count; i++)
            {
                if (((Field)fields[i]).Name() == name)
                    return (Field)fields[i];
            }
            return null;
        }

        internal void SetBuffer(PEReader buff)
        {
            Contract.Requires(buff != null);
            buffer = buff;
        }

        internal override void TypeSig(MemoryStream sig)
        {
            sig.WriteByte(typeIndex);
            MetaDataOut.CompressNum(BlobUtil.CompressUInt(TypeDefOrRefToken()), sig);
        }

        internal abstract string ClassName();

        internal virtual uint TypeDefOrRefToken() { return 0; }

    }

    /**************************************************************************/
    /// <summary>
    /// 
    /// </summary> 
    public class ClassSpec : Class
    {
        private readonly Class genClass;
        private uint sigIx;
        private static readonly byte GENERICINST = 0x15;

        /*-------------------- Constructors ---------------------------------*/

        internal ClassSpec(Class clType, Type[] gPars)
        {
            Contract.Requires(clType != null);
            Contract.Requires(gPars != null);
            this.typeIndex = GENERICINST;
            genClass = clType;
            genericParams = new ArrayList(gPars);
            tabIx = MDTable.TypeSpec;
            typeIndex = GENERICINST;
            ArrayList classMethods = clType.GetMethodList();
            ArrayList classFields = clType.GetFieldList();
            for (int i = 0; i < classMethods.Count; i++)
            {
                MethSig mSig = ((Method)classMethods[i]).GetSig(); //.InstantiateGenTypes(this,gPars);
                if (mSig != null)
                {
                    MethodRef newMeth = new MethodRef(mSig);
                    newMeth.SetParent(this);
                    newMeth.GenericParams = ((Method)classMethods[i]).GenericParams;
                    methods.Add(newMeth);
                }
            }
            for (int i = 0; i < classFields.Count; i++)
            {
                Type fType = ((Field)classFields[i]).GetFieldType();
                fields.Add(new FieldRef(this, ((Field)classFields[i]).Name(), fType));
            }
        }

        /*------------------------- public set and get methods --------------------------*/

        /// <summary>
        /// Get the generic class that this is an instantiation of
        /// </summary>
        /// <returns>generic class</returns>
        public override MetaDataElement GetParent()
        {
            return null;
        }

        /// <summary>
        /// Get the specified generic parameter number 
        /// </summary>
        /// <param name="ix">generic parameter number</param>
        /// <returns>generic parameter number ix</returns>
        public Type GetGenericParamType(int ix)
        {
            if (ix >= genericParams.Count) return null;
            return (Type)genericParams[ix];
        }

        /// <summary>
        /// Get the generic parameters of this class
        /// </summary>
        /// <returns>list of generic parameters</returns>
        public Type[] GetGenericParamTypes()
        {
            return (Type[])genericParams.ToArray(typeof(Type));
        }

        /// <summary>
        /// Get the generic class that this class instantiates
        /// </summary>
        /// <returns>generic class</returns>
        public Class GetGenericClass()
        {
            return genClass;
        }

        /// <summary>
        /// Count how many generic parameters this class has
        /// </summary>
        /// <returns>number of generic parameters</returns>
        public int GetGenericParCount()
        {
            return genericParams.Count;
        }

        /*----------------------------- internal functions ------------------------------*/

        internal void AddMethod(Method meth)
        {
            Contract.Requires(meth != null);
            methods.Add(meth);
            meth.SetParent(this);
        }

        internal override string ClassName()
        {
            // need to return something here??
            return null;
        }

        internal override sealed uint TypeDefOrRefToken()
        {
            uint cIx = Row;
            cIx = (cIx << 2) | 0x2;
            return cIx;
        }

        internal override Type GetGenPar(uint ix)
        {
            if (genClass == null) return new GenericParam(null, this, (int)ix);
            return genClass.GetGenPar(ix);
            //if (ix >= genericParams.Count) return null;
            //return (Type)genericParams[(int)ix];
        }

        internal override sealed Type AddTypeSpec(MetaDataOut md) {
          md.ConditionalAddTypeSpec(this);
          BuildMDTables(md);
          return this;
        }

        internal override void BuildTables(MetaDataOut md)
        {
            if (!genClass.isDef())
                genClass.BuildMDTables(md);
            for (int i = 0; i < genericParams.Count; i++)
            {
                if (!((Type)genericParams[i]).isDef() &&
                    (!(genericParams[i] is GenericParam)))
                    ((Type)genericParams[i]).BuildMDTables(md);
            }
        }

        internal override void BuildSignatures(MetaDataOut md)
        {
            MemoryStream outSig = new MemoryStream();
            TypeSig(outSig);
            sigIx = md.AddToBlobHeap(outSig.ToArray());
        }

        internal sealed override void TypeSig(MemoryStream sig)
        {
            sig.WriteByte(typeIndex);
            genClass.TypeSig(sig);
            //MetaDataOut.CompressNum((uint)genericParams.Count, sig);
            MetaDataOut.CompressNum(BlobUtil.CompressUInt((uint)genericParams.Count), sig);
            for (int i = 0; i < genericParams.Count; i++)
            {
                ((Type)genericParams[i]).TypeSig(sig);
            }
        }

        internal sealed override void Write(PEWriter output)
        {
            //Console.WriteLine("Writing the blob index for a TypeSpec");
            output.BlobIndex(sigIx);
        }

        internal sealed override uint GetCodedIx(CIx code)
        {
            switch (code)
            {
                case (CIx.TypeDefOrRef): return 2;
                case (CIx.HasCustomAttr): return 13;
                case (CIx.MemberRefParent): return 4;
            }
            return 0;
        }
    }

    /**************************************************************************/
    /// <summary>
    /// wrapper for TypeSpec parent of MethodRef or FieldRef
    /// </summary>
    public class ConstructedTypeSpec : Class
    {
        public ConstructedTypeSpec(TypeSpec tySpec)
            : base()
        {
            Contract.Requires(tySpec != null);
            Spec = tySpec;
            this.typeIndex = Spec.GetTypeIndex();
        }

        public TypeSpec Spec { get; }

        public override MetaDataElement GetParent()
        {
            return null;
        }

        internal override string ClassName()
        {
            return Spec.NameString();
        }



    }

    /**************************************************************************/
    public abstract class ClassDesc : Class
    {

        /*-------------------- Constructors ---------------------------------*/

        internal ClassDesc(string nameSpaceName, string className)
        {
            Contract.Requires(nameSpaceName != null);
            Contract.Requires(className != null);
            NameSpace = nameSpaceName;
            Name = className;
        }

        internal ClassDesc()
        {
        }

        /*------------------------- public set and get methods --------------------------*/

        public GenericParam GetGenericParam(int ix)
        {
            if (ix >= genericParams.Count) return null;
            return (GenericParam)genericParams[ix];
        }

        public GenericParam[] GetGenericParams()
        {
            return (GenericParam[])genericParams.ToArray(typeof(GenericParam));
        }

        public virtual void SetGenericParams(GenericParam[] genPars)
        {
            Contract.Requires(genPars != null);
            for (int i = 0; i < genPars.Length; i++)
            {
                genPars[i].SetClassParam(this, i);
            }
            genericParams = new ArrayList(genPars);
        }

        /*----------------------------- internal functions ------------------------------*/

        protected void DeleteGenericParam(int pos)
        {
            Contract.Requires(pos >= 0);
            genericParams.RemoveAt(pos);
            for (int i = pos; i < genericParams.Count; i++)
            {
                GenericParam gp = (GenericParam)genericParams[i];
                gp.Index = (uint)i;
            }
        }

        internal void AddGenericParam(GenericParam par)
        {
            Contract.Requires(par != null);
            genericParams.Add(par);
            //par.SetClassParam(this,genericParams.Count-1);
        }

        internal override Type GetGenPar(uint ix)
        {
            // create generic param descriptor if one does not exist
            // - used when reading exported interface
            // The next two lines are *required* for v2.0 beta release! (kjg)
            for (int i = genericParams.Count; i <= ix; i++)
                genericParams.Add(new GenericParam("gp" + i, this, i));
            return (GenericParam)genericParams[(int)ix];
        }

    }
}