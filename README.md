XmlSchemaClassGenerator
=======================

A console program and library to generate [XmlSerializer](http://msdn.microsoft.com/en-us/library/system.xml.serialization.xmlserializer.aspx) compatible C# classes
from <a href="http://en.wikipedia.org/wiki/XML_Schema_(W3C)">XML Schema</a> files.

Features
--------

* Map XML namespaces to C# namespaces, either explicitly or through a (configurable) function
* Generate C# XML comments from schema annotations
* Generate [DataAnnotations](http://msdn.microsoft.com/en-us/library/system.componentmodel.dataannotations.aspx) attributes from schema restrictions
* Use [`Collection<T>`](http://msdn.microsoft.com/en-us/library/ms132397.aspx) properties (initialized in constructor and with private setter)
* Use either int, long, decimal, or string for xs:integer and derived types
* Automatic properties
* Pascal case for classes and properties

Unsupported:

* Global elements and types that are restrictions of simple types
* Some restriction types

Usage
-----

From the command line:

```
Usage: XmlSchemaClassGenerator.Console [OPTIONS]+ xsdFile...
Generate C# classes from XML Schema files.
xsdFiles may contain wildcards in the file part.

Options:
  -h, --help                 show this message and exit
  -n, --namespace=VALUE      map an XML namespace to a C# namespace
                               Separate XML namespace and C# namespace by '='.
                               One option must be given for each namespace to
                               be mapped.
                               If no mapping is found for an XML namespace, a
                               name is generated automatically (may fail).
  -o, --output=FOLDER        the FOLDER to write the resulting .cs files to
  -i, --integer=TYPE         map xs:integer and derived types to TYPE instead
                               of string
                               TYPE can be i[nt], l[ong], or d[ecimal].
  -p, --prefix=PREFIX        the PREFIX to prepend to auto-generated namespace
                               names
  -v, --verbose              print generated file names on stdout
```

From code:

```C#
var generator = new Generator
{
    GenerateNamespaceName = xn => ..., // namespace generator func
    NamespaceMapping = new Dictionary<string, string>(...),
    OutputFolder = outputFolder,
    Log = s => Console.Out.WriteLine(s)
};

generator.Generate(files);
```

Contributing
------------

Pull requests are welcome :)
