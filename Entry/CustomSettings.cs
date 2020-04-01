using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using XmlSchemaClassGenerator;

namespace Entry
{
    public static class CustomSettings
    {
        //Input directory settings
        public static string inputPath { get; set; }
        public static DirectoryInfo inputDirectory { get; set; }

        //Output directory settings
        public static string outputPath { get; set; }
        public static DirectoryInfo outputDirectory { get; set; }
        public static bool RemoveExistingOutputFiles = false;

        //In & out file paths
        public static List<FileInfo> inputFiles = new List<FileInfo>();
        public static List<string> files = new List<string>();

        //XmlSchemaClassGenerator settings
        public static Generator generator = new Generator();

        public static List<string> Get()
        {
            List<string> errors = new List<string>();
            
            try
            {
                //Input verification
                if (Directory.Exists(inputPath) == false)
                {
                    errors.Add("Input directory path invalid '" + inputPath);
                }
                else
                {
                    inputDirectory = Directory.GetParent(inputPath);
                    List<FileInfo> fs = inputDirectory.GetFiles().ToList();
                    
                    inputFiles = fs.Where(f => f.Extension == ".xsd").ToList();
                    if (inputFiles.Count == 0)
                    {
                        errors.Add("Zero input '.xsd' files were found in '" + inputPath + "'");
                    }
                    else
                    {
                        List<bool> passed = VerifyInputFiles();
                        if (passed.Count(b => b == false) > 0)
                        {
                            foreach(bool p in passed)
                            {
                                errors.Add("Input file error for '" + inputFiles[passed.IndexOf(p)].Name + "'");
                            }
                        }
                    }
                }

                //Output verification
                if (outputPath == "")
                {
                    errors.Add("Output directory path is empty");
                }
                else
                {
                    if (Directory.Exists(outputPath) == false)
                    {
                        outputDirectory = Directory.CreateDirectory(outputPath);
                    }
                    else
                    {
                        outputDirectory = Directory.GetParent(outputPath);
                        if (RemoveExistingOutputFiles == true)
                        {
                            outputDirectory.Delete(true);
                        }
                    }
                }

                if (errors.Count == 0)
                {
                    generator.Log = s => Console.Out.WriteLine(s);
                    generator.OutputFolder = outputPath;

                    //New separate file feature
                    generator.SeparateClasses = true;

                    //Generator options
                    generator.AssemblyVisible = true;
                    generator.DisableComments = false;
                    generator.EmitOrder = true;
                    generator.EnableDataBinding = false;
                    generator.EnableUpaCheck = true;
                    generator.EntityFramework = true;
                    generator.GenerateNullables = true;
                    generator.GenerateComplexTypesForCollections = true;
                    generator.GenerateDebuggerStepThroughAttribute = true;
                    generator.GenerateDesignerCategoryAttribute = true;
                    generator.GenerateInterfaces = true;
                    generator.GenerateNullables = true;
                    generator.GenerateSerializableAttribute = true;
                    generator.UseShouldSerializePattern = true;
                    generator.UseXElementForAny = true;

                    foreach (FileInfo f in inputFiles)
                    {
                        string n = f.Name.Substring(0, f.Name.LastIndexOf("."));
                        string sNS = FetchSchemaNamespace(f.FullName);

                        if (sNS == "")
                        { errors.Add("XmlSchema TargetNamespace is empty for '" + n + "'"); }
                        else
                        {
                            if (generator.NamespaceProvider.Keys.Count(ns => ns.XmlSchemaNamespace == sNS) == 0)
                            {
                                generator.NamespaceProvider.Add(new NamespaceKey(sNS), n);
                            }
                            files.Add(f.FullName);
                        }
                    }
                }
            }
            catch (Exception ae)
            {
                string strError = ae.ToString();
                if (ae.InnerException != null) strError = ae.InnerException.Message.ToString();
            }
            return errors;
        }


        private static List<bool> VerifyInputFiles()
        {
            List<bool> passed = new List<bool>();
            foreach (FileInfo fi in inputFiles)
            {
                try
                {
                    XmlReader xr = XmlReader.Create(fi.FullName);
                    passed.Add(true);
                }
                catch (Exception)
                {
                    passed.Add(false);
                }
            }
            return passed;
        }

        private static string FetchSchemaNamespace(string fileURL)
        {
            XmlTextReader reader = new XmlTextReader(fileURL);
            XmlSchema xs = XmlSchema.Read(reader, ValidationCallback);
            return xs.TargetNamespace;
        }

        private static void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
                Console.Write("WARNING: ");
            else if (args.Severity == XmlSeverityType.Error)
                Console.Write("ERROR: ");

            Console.WriteLine(args.Message);
        }
    }
}
