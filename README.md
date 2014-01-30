XmlSchemaClassGenerator
=======================

A console program and library to generate 
[XmlSerializer](http://msdn.microsoft.com/en-us/library/system.xml.serialization.xmlserializer.aspx) compatible C# classes
from <a href="http://en.wikipedia.org/wiki/XML_Schema_(W3C)">XML Schema</a> files.

Features
--------

* Map XML namespaces to C# namespaces, either explicitly or through a (configurable) function
* Generate C# XML comments from schema annotations
* Generate [DataAnnotations](http://msdn.microsoft.com/en-us/library/system.componentmodel.dataannotations.aspx) attributes 
from schema restrictions
* Use [`Collection<T>`](http://msdn.microsoft.com/en-us/library/ms132397.aspx) properties 
(initialized in constructor and with private setter)
* Use either int, long, decimal, or string for xs:integer and derived types
* Automatic properties
* Pascal case for classes and properties
* Generate nullable adapter properties for optional elements and attributes without default values (see [below](#nullables))

Unsupported:

* Global elements and types that are restrictions of simple types
* Some restriction types

Usage
-----

From the command line:

```
Usage: XmlSchemaClassGenerator.Console [OPTIONS]+ xsdFile...
Generate C# classes from XML Schema files.
xsdFiles may contain globs, e.g. "content\{schema,xsd}\**\*.xsd".

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
  -0, --nullable             generate nullable adapter properties for optional
                               elements/attributes w/o default values
```

From code:

```C#
var generator = new Generator
{
    GenerateNamespaceName = xn => ..., // namespace generator func
    NamespaceMapping = new Dictionary<string, string>(...),
    OutputFolder = outputFolder,
    Log = s => Console.Out.WriteLine(s),
    GenerateNullables = true,
};

generator.Generate(files);
```

Nullables<a name="nullables"></a>
---------------------------------

XmlSerializer has been present in the .NET Framework since version 1.1 
and has never been updated to provide support for nullables
which are a natural fit for the problem of signaling the absence or presence of a value type
but have only been present since .NET Framework 2.0.

Instead XmlSerializer has support for a pattern where you provide an additional bool property
with "Specified" appended to the name to signal if the original property should be serialized. 
For example:

```xml
<xs:attribute name="id" type="xs:int" use="optional">...</xs:attribute>
```

```C#
[System.Xml.Serialization.XmlAttributeAttribute("id", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="int")]
public int Id { get; set; }

[System.Xml.Serialization.XmlIgnoreAttribute()]
public bool IdSpecified { get; set; }
```

XmlSchemaClassGenerator can optionally generate an additional nullable property that works as an adapter to both properties:

```C#
[System.Xml.Serialization.XmlAttributeAttribute("id", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="int")]
public int IdValue { get; set; }
        
[System.Xml.Serialization.XmlIgnoreAttribute()]
public bool IdValueSpecified { get; set; }

[System.Xml.Serialization.XmlIgnoreAttribute()]
public System.Nullable<int> Id
{
    get
    {
        if (this.IdValueSpecified)
        {
            return this.IdValue;
        }
        else
        {
            return null;
        }
    }
    set
    {
        this.IdValue = value.GetValueOrDefault();
        this.IdValueSpecified = value.HasValue;
    }
}
```

Contributing
------------

Pull requests are welcome :)
