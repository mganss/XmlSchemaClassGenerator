using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace XmlSchemaClassGenerator
{
    public class FileOutputWriter : OutputWriter
    {
        public GeneratorConfiguration Configuration { get; set; }

        public FileOutputWriter(string directory, bool createIfNotExists = true)
        {
            OutputDirectory = directory;

            if (createIfNotExists && !Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }
        }

        public string OutputDirectory { get; }

        /// <summary>
        /// A list of all the files written.
        /// </summary>
        public IList<string> WrittenFiles { get; } = new List<string>();

        public override void Write(CodeNamespace cn)
        {
            var cu = new CodeCompileUnit();
            cu.Namespaces.Add(cn);

            if (Configuration?.SeparateClasses == true || Configuration?.SeparateNamespaceHierarchy == true)
            {
                WriteSeparateFiles(cn);
            }
            else
            {
                var path = Path.Combine(OutputDirectory, cn.Name + ".cs");
                Configuration?.WriteLog(path);
                WriteFile(path, cu);
            }
        }

        protected virtual void WriteFile(string path, CodeCompileUnit cu)
        {
            FileStream fs = null;

            try
            {
                fs = new FileStream(path, FileMode.Create);
                using (var writer = new StreamWriter(fs))
                {
                    fs = null;
                    Write(writer, cu);
                }
                WrittenFiles.Add(path);
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }
        }

        private void WriteSeparateFiles(CodeNamespace cn)
        {
            var validatedNamespaceName = ValidateName(cn.Name);

            var namespacePath = validatedNamespaceName;

            if (Configuration?.SeparateNamespaceHierarchy == true)
            {
                namespacePath = Path.Combine(validatedNamespaceName.Split('.'));
            }

            var dirPath = Path.Combine(OutputDirectory, namespacePath);
            var ccu = new CodeCompileUnit();
            var cns = new CodeNamespace(validatedNamespaceName);

            Directory.CreateDirectory(dirPath);

            cns.Imports.AddRange(cn.Imports.Cast<CodeNamespaceImport>().ToArray());
            cns.Comments.AddRange(cn.Comments);
            ccu.Namespaces.Add(cns);

            foreach (CodeTypeDeclaration ctd in cn.Types)
            {
                var path = Path.Combine(dirPath, ctd.Name + ".cs");
                cns.Types.Clear();
                cns.Types.Add(ctd);
                Configuration?.WriteLog(path);
                WriteFile(path, ccu);
            }
        }

        static readonly Regex InvalidCharacters = new($"[{string.Join("", Path.GetInvalidFileNameChars())}]", RegexOptions.Compiled);

        private string ValidateName(string name) => InvalidCharacters.Replace(name, "_");
    }
}