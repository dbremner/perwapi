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
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;


namespace QUT.PERWAPI
{
    /**************************************************************************/
    // Class to Write CIL File
    /**************************************************************************/
    public class CILWriter : StreamWriter
    {
        private readonly PEFile pefile;
        private readonly List<ReferenceScope> externRefs = new List<ReferenceScope>();
        private FieldDef[] fields;
        private MethodDef[] methods;
        private ClassDef[] classes;

        public CILWriter(string filename, bool debug, PEFile pefile)
            : base(new FileStream(filename, FileMode.Create))
        {
            Contract.Requires(filename != null);
            Contract.Requires(pefile != null);
            this.pefile = pefile;
            WriteLine("// ILASM output by PERWAPI");
            WriteLine("// for file <" + pefile.GetFileName() + ">");
        }

        internal void AddRef(ReferenceScope refScope)
        {
            Contract.Requires(refScope != null);
            if (!externRefs.Contains(refScope))
            {
                externRefs.Add(refScope);
            }
        }

        internal bool Debug { get; private set; }

        internal void BuildCILInfo()
        {
            fields = pefile.GetFields();
            methods = pefile.GetMethods();
            classes = pefile.GetClasses();
            if (fields != null)
            {
                foreach (FieldDef field in fields)
                {
                    field.BuildCILInfo(this);
                }
            }
            if (methods != null)
            {
                foreach (MethodDef method in methods)
                {
                    method.BuildCILInfo(this);
                }
            }
            if (classes != null)
            {
                foreach (ClassDef cls in classes)
                {
                    cls.BuildCILInfo(this);
                }
            }
        }

        public void WriteFile(bool debug)
        {
            this.Debug = debug;
            foreach (ReferenceScope externRef in externRefs)
            {
                externRef.Write(this);
            }
            Assembly assem = pefile.GetThisAssembly();
            if (assem != null)
            {
                assem.Write(this);
            }
            WriteLine(".module " + pefile.GetFileName());
            if (fields != null)
            {
                foreach (FieldDef field in fields)
                {
                    field.Write(this);
                }
            }
            if (methods != null)
            {
                foreach (MethodDef method in methods)
                {
                    method.Write(this);
                }
            }
            if (classes != null)
            {
                foreach (ClassDef cls in classes)
                {
                    cls.Write(this);
                }
            }
            this.Flush();
            this.Close();
        }

    }
}