using System;
using System.Xml;

namespace XmlSchemaClassGenerator;

public class NormalizingXmlResolver(string forceUriScheme) : XmlUrlResolver()
{
    // the Uri scheme to force on the resolved Uris
    // "none" - do not change Uri scheme
    // "same" - force the same Uri scheme as base Uri
    // any other string becomes the new Uri scheme of the baseUri
    private readonly string forceUriScheme = forceUriScheme;

    public override Uri ResolveUri(Uri baseUri, string relativeUri)
    {
        var resolvedUri = base.ResolveUri(baseUri, relativeUri);
        var r = NormalizeUri(baseUri, resolvedUri);
        return r;
    }

    private Uri NormalizeUri(Uri baseUri, Uri resolvedUri)
    {
        var newScheme = forceUriScheme;

        switch (forceUriScheme)
        {
            case "none": return resolvedUri;
            case "same":
                {
                    newScheme = baseUri.Scheme;
                    break;
                }
        }

        var builder = new UriBuilder(resolvedUri) { Scheme = newScheme, Port = -1 };

        return builder.Uri;
    }
}
