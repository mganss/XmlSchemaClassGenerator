﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlSchemaClassGenerator;

public sealed class NamespaceKey(Uri source, string xmlSchemaNamespace) : IComparable<NamespaceKey>, IEquatable<NamespaceKey>, IComparable
{
    private const UriComponents CompareComponentsAbs = UriComponents.Host | UriComponents.Scheme | UriComponents.Path;
    private const UriFormat CompareFormat = UriFormat.Unescaped;

    public string XmlSchemaNamespace { get; private set; } = xmlSchemaNamespace ?? string.Empty;
    public Uri Source { get; private set; } = source;

    public NamespaceKey()
        : this(null, null)
    {

    }

    public NamespaceKey(Uri source)
        : this(source, null)
    {

    }

    public NamespaceKey(string xmlSchemaNamespace)
        : this(null, xmlSchemaNamespace)
    {

    }

    public bool Equals(NamespaceKey other)
    {
        return CompareTo(other) == 0;
    }

    public override bool Equals(object obj)
    {
        return Equals((NamespaceKey)obj);
    }

    public int CompareTo(NamespaceKey other)
    {
        if (other == null) return 1;

        if (Source == null && other.Source != null)
        {
            return -1;
        }
        if (Source != null && other.Source == null)
        {
            return 1;
        }
        if (Source != null)
        {
            var result = Uri.Compare(Source, other.Source, CompareComponentsAbs, CompareFormat,
                StringComparison.OrdinalIgnoreCase);
            if (result != 0)
            {
                return result;
            }
        }
        return string.Compare(XmlSchemaNamespace, other.XmlSchemaNamespace, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        var hashCode = 0;
        if (Source != null)
        {
            if (Source.IsAbsoluteUri)
            {
                hashCode = Source.GetComponents(CompareComponentsAbs, CompareFormat).ToLower().GetHashCode();
            }
            else
            {
                hashCode = Source.OriginalString.ToLower().GetHashCode();
            }
        }
        if (XmlSchemaNamespace != null)
        {
            hashCode ^= XmlSchemaNamespace.GetHashCode();
        }
        return hashCode;
    }

    int IComparable.CompareTo(object obj)
    {
        return CompareTo((NamespaceKey)obj);
    }

    public static bool operator ==(NamespaceKey left, NamespaceKey right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator >(NamespaceKey left, NamespaceKey right)
    {
        return left.CompareTo(right) > 0;
    }
    public static bool operator >=(NamespaceKey left, NamespaceKey right)
    {
        return left.CompareTo(right) >= 0;
    }
    public static bool operator <(NamespaceKey left, NamespaceKey right)
    {
        return left.CompareTo(right) < 0;
    }
    public static bool operator <=(NamespaceKey left, NamespaceKey right)
    {
        return left.CompareTo(right) <= 0;
    }
    public static bool operator !=(NamespaceKey left, NamespaceKey right)
    {
        return !(left == right);
    }
}
