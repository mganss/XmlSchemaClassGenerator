using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XmlSchemaClassGenerator
{
    public class FileOutputWriter : OutputWriter
    {
        private readonly string _outputDirectory;
        private readonly bool _splitNamespaceAndFileName;
        public GeneratorConfiguration Configuration { get; set; }

        public FileOutputWriter(
            string directory, 
            bool createIfNotExists = true,
            bool splitNamespaceAndFileNameName = false)
        {
            _splitNamespaceAndFileName = splitNamespaceAndFileNameName;
            _outputDirectory = directory;

            if (createIfNotExists && !Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }


        /// <summary>
        /// A list of all the files written.
        /// </summary>
        public IList<string> WrittenFiles { get; } = new List<string>();

        public override void Write(CodeNamespace cn)
        {
            var cu = new CodeCompileUnit();
            cu.Namespaces.Add(cn);

            string fileName;
            if (_splitNamespaceAndFileName && cn.Name.Contains('.'))
            {
                var namespaceSegments = cn.Name.Split('.');
                // last segment will be used as file name:
                fileName = namespaceSegments.Last();
                
                // rebuild the namespace without the last:
                var allButLast = namespaceSegments.Take(namespaceSegments.Length - 1);
                cn.Name = string.Join(".", allButLast);
            }
            else
            {
                fileName = cn.Name;
            }
      

            var path = Path.Combine(_outputDirectory, fileName + ".cs");
            Configuration?.WriteLog(path);

            WriteFile(path, cu);
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
    }
}