using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class NamespaceProviderTests
{
    [Fact]
    public void ContainsKeyTest()
    {
        var ns = new NamespaceProvider { { new NamespaceKey("x"), "c" } };
        ns.GenerateNamespace = k => k.XmlSchemaNamespace != "z" ? k.XmlSchemaNamespace : null;
        AssertEx.CollectionEqual([.. ns.Values], ["c"]);
        Assert.True(ns.ContainsKey(new NamespaceKey("x")));
        Assert.True(ns.ContainsKey(new NamespaceKey("y")));
        Assert.True(ns.ContainsKey(new NamespaceKey("y")));
        Assert.False(ns.ContainsKey(new NamespaceKey("z")));
        ns.Clear();
        Assert.Empty(ns);
    }

    [Fact]
    public void KeysTest()
    {
        var ns = new NamespaceProvider { { new NamespaceKey("x"), "c" }, { new NamespaceKey("y"), "d" } };
        ns.Remove(new NamespaceKey("y"));
        AssertEx.CollectionEqual([.. ns.Keys], [new NamespaceKey("x")]);
    }

    [Fact]
    public void IndexTest()
    {
        var ns = new NamespaceProvider
        {
            [new NamespaceKey("x")] = "c",
            GenerateNamespace = k => k.XmlSchemaNamespace != "z" ? k.XmlSchemaNamespace : null
        };

        Assert.Equal("c", ns[new NamespaceKey("x")]);
        Assert.Equal("y", ns[new NamespaceKey("y")]);
        Assert.Equal("y", ns[new NamespaceKey("y")]);
        Assert.Throws<KeyNotFoundException>(() => ns[new NamespaceKey("z")]);
    }

    [Fact]
    public void NamespaceKeyComparableTest()
    {
        Assert.Equal(-1, new NamespaceKey((Uri)null).CompareTo(new NamespaceKey(new Uri("http://test"))));
        Assert.Equal(1, new NamespaceKey(new Uri("http://test")).CompareTo(new NamespaceKey((Uri)null)));
        Assert.NotEqual(0, new NamespaceKey(new Uri("http://test")).CompareTo(new NamespaceKey(new Uri("http://test2"))));
        Assert.True(new NamespaceKey("http://test").Equals((object)new NamespaceKey("http://test")));
        Assert.False(new NamespaceKey("http://test").Equals((object)null));
        Assert.NotEqual(0, ((IComparable)new NamespaceKey("http://test")).CompareTo(null));
        Assert.True(new NamespaceKey("http://test") == new NamespaceKey("http://test"));
        Assert.True(((NamespaceKey)null) == ((NamespaceKey)null));
        Assert.True(new NamespaceKey("http://test") > null);
        Assert.False(new NamespaceKey("http://test") < null);
        Assert.True(new NamespaceKey("http://test") >= null);
        Assert.False(new NamespaceKey("http://test") <= null);
        Assert.NotNull(new NamespaceKey("http://test"));
    }

    [Theory]
    [InlineData("http://www.w3.org/2001/XMLSchema", "test.xsd", "MyNamespace", "Test")]
    [InlineData("http://www.w3.org/2001/XMLSchema", "test.xsd", "MyNamespace", null)]
    [InlineData("http://www.w3.org/2001/XMLSchema", "test.xsd", "MyNamespace", "")]
    [InlineData("", "test.xsd", "MyNamespace", "Test")]
    [InlineData("", "test.xsd", "MyNamespace", null)]
    [InlineData("", "test.xsd", "MyNamespace", "")]
    [InlineData(null, "test.xsd", "MyNamespace", "Test")]
    [InlineData(null, "test.xsd", "MyNamespace", null)]
    [InlineData(null, "test.xsd", "MyNamespace", "")]
    public void TestParseNamespaceUtilityMethod1(string xmlNs, string xmlSchema, string netNs, string netPrefix)
    {
        string customNsPattern = "{0}|{1}={2}";

        var uri = new Uri(xmlSchema, UriKind.RelativeOrAbsolute);
        var fullNetNs = (string.IsNullOrEmpty(netPrefix)) ? netNs : string.Join(".", netPrefix, netNs);

        var expected = new KeyValuePair<NamespaceKey, string>(new NamespaceKey(uri, xmlNs), fullNetNs);
        var actual = CodeUtilities.ParseNamespace(string.Format(customNsPattern, xmlNs, xmlSchema, netNs), netPrefix);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("test.xsd", "MyNamespace", "Test")]
    [InlineData("test.xsd", "MyNamespace", null)]
    [InlineData("test.xsd", "MyNamespace", "")]
    public void TestParseNamespaceUtilityMethod2(string xmlSchema, string netNs, string netPrefix)
    {
        string customNsPattern = "{0}={1}";

        var fullNetNs = (string.IsNullOrEmpty(netPrefix)) ? netNs : string.Join(".", netPrefix, netNs);
        var expected = new KeyValuePair<NamespaceKey, string>(new NamespaceKey(null, xmlSchema), fullNetNs);
        var actual = CodeUtilities.ParseNamespace(string.Format(customNsPattern, xmlSchema, netNs), netPrefix);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestParseNamespaceUtilityMethodWithDefault()
    {
        var netPrefix = "Test";
        var netNs = "MyNamespace";
        var actual = CodeUtilities.ParseNamespace(netNs, null);

        Assert.Equal(new KeyValuePair<NamespaceKey, string>(new NamespaceKey(), netNs), actual);

        actual = CodeUtilities.ParseNamespace(netNs, netPrefix);

        Assert.Equal(new KeyValuePair<NamespaceKey, string>(new NamespaceKey(), string.Join(".", netPrefix, netNs)), actual);
    }

    [Theory]
    [InlineData("http://annox.dev.java.net", "Net.Java.Dev.Annox")]
    [InlineData("http://annox.dev.java.net/java.lang", "Net.Java.Dev.Annox.Java.Lang")]
    [InlineData("http://car/1.0", "Car._1._0")]
    [InlineData("http://graphml.graphdrawing.org/xmlns", "Org.Graphdrawing.Graphml.Xmlns")]
    [InlineData("http://hic.gov.au/hiconline/medicare/version-4", "Au.Gov.Hic.Hiconline.Medicare.Version._4")]
    [InlineData("http://java.sun.com/xml/ns/jaxb", "Com.Sun.Java.Xml.Ns.Jaxb")]
    [InlineData("http://me.me", "Me.Me")]
    [InlineData("http://microsoft.com/schemas/VisualStudio/TeamTest/2010", "Com.Microsoft.Schemas.VisualStudio.TeamTest._2010")]
    [InlineData("http://microsoft.com/wsdl/types/", "Com.Microsoft.Wsdl.Types")]
    [InlineData("http://none.local/a", "Local.None.A")]
    [InlineData("http://none.local/b", "Local.None.B")]
    [InlineData("http://rest.immobilienscout24.de/schema/attachmentsorder/1.0", "De.Immobilienscout24.Rest.Schema.Attachmentsorder._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/common/1.0", "De.Immobilienscout24.Rest.Schema.Common._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/customer/realestatestock/1.0", "De.Immobilienscout24.Rest.Schema.Customer.Realestatestock._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/messages/1.0", "De.Immobilienscout24.Rest.Schema.Messages._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/alterationdate/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Alterationdate._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/listelement/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Listelement._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/premiumplacement/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Premiumplacement._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/productrecommondation/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Productrecommondation._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/realestateproject/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Realestateproject._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/realestates/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Realestates._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/realestatestock/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Realestatestock._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/realtor/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Realtor._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/realtorbadges/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Realtorbadges._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/showcaseplacement/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Showcaseplacement._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/toplisting/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Toplisting._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/topplacement/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Topplacement._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/user/1.0", "De.Immobilienscout24.Rest.Schema.Offer.User._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/offer/zipandlocationtoregion/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Zipandlocationtoregion._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/platform/gis/1.0", "De.Immobilienscout24.Rest.Schema.Platform.Gis._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/realestate/counts/1.0", "De.Immobilienscout24.Rest.Schema.Realestate.Counts._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/search/common/1.0", "De.Immobilienscout24.Rest.Schema.Search.Common._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/search/expose/1.0", "De.Immobilienscout24.Rest.Schema.Search.Expose._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/search/region/1.0", "De.Immobilienscout24.Rest.Schema.Search.Region._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/search/resultlist/1.0", "De.Immobilienscout24.Rest.Schema.Search.Resultlist._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/search/savedSearch/1.0", "De.Immobilienscout24.Rest.Schema.Search.SavedSearch._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/search/searcher/1.0", "De.Immobilienscout24.Rest.Schema.Search.Searcher._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/search/shortlist/1.0", "De.Immobilienscout24.Rest.Schema.Search.Shortlist._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/user/1.0", "De.Immobilienscout24.Rest.Schema.User._1._0")]
    [InlineData("http://rest.immobilienscout24.de/schema/videoupload/1.0", "De.Immobilienscout24.Rest.Schema.Videoupload._1._0")]
    [InlineData("http://schemas.nsc.co.uk/exceptions", "Uk.Co.Nsc.Schemas.Exceptions")]
    [InlineData("http://tableau.com/api", "Com.Tableau.Api")]
    [InlineData("http://tempuri.org/default.xsd", "Org.Tempuri.Default.Xsd")]
    [InlineData("http://tempuri.org/mySchema.xsd", "Org.Tempuri.MySchema.Xsd")]
    [InlineData("http://tempuri.org/PurchaseOrderSchema.xsd", "Org.Tempuri.PurchaseOrderSchema.Xsd")]
    [InlineData("http://tempuri.org/v1", "Org.Tempuri.V1")]
    [InlineData("http://tempuri.org/XMLSchema.xsd", "Org.Tempuri.XmlSchema.Xsd")]
    [InlineData("http://wadl.dev.java.net/2009/02", "Net.Java.Dev.Wadl._2009._02")]
    [InlineData("http://wsdl.siri.org.uk", "Uk.Org.Siri.Wsdl")]
    [InlineData("http://www.aixm.aero/schema/5.1.1", "Aero.Aixm.Schema._5._1._1")]
    [InlineData("http://www.aixm.aero/schema/5.1.1/extensions/EUR/ADR", "Aero.Aixm.Schema._5._1._1.Extensions.Eur.Adr")]
    [InlineData("http://www.aixm.aero/schema/5.1.1/message", "Aero.Aixm.Schema._5._1._1.Message")]
    [InlineData("http://www.elster.de/2002/XMLSchema", "De.Elster._2002.XmlSchema")]
    [InlineData("http://www.elster.de/headerbasis02/XMLSchema", "De.Elster.Headerbasis02.XmlSchema")]
    [InlineData("http://www.eurocontrol.int/cfmu/b2b/ADRMessage", "Int.Eurocontrol.Cfmu.B2B.AdrMessage")]
    [InlineData("http://www.govtalk.gov.uk/CM/gms-xs", "Uk.Gov.Govtalk.Cm.Gms.Xs")]
    [InlineData("http://www.govtalk.gov.uk/core", "Uk.Gov.Govtalk.Core")]
    [InlineData("http://www.iata.org/IATA/EDIST/2017.2", "Org.Iata.Iata.Edist._2017._2")]
    [InlineData("http://www.ifopt.org.uk/acsb", "Uk.Org.Ifopt.Acsb")]
    [InlineData("http://www.ifopt.org.uk/ifopt", "Uk.Org.Ifopt.Ifopt")]
    [InlineData("http://www.immobilienscout24.de/immobilientransfer", "De.Immobilienscout24.Immobilientransfer")]
    [InlineData("http://www.isotc211.org/2005/gco", "Org.Isotc211._2005.Gco")]
    [InlineData("http://www.isotc211.org/2005/gmd", "Org.Isotc211._2005.Gmd")]
    [InlineData("http://www.isotc211.org/2005/gmx", "Org.Isotc211._2005.Gmx")]
    [InlineData("http://www.isotc211.org/2005/gsr", "Org.Isotc211._2005.Gsr")]
    [InlineData("http://www.isotc211.org/2005/gss", "Org.Isotc211._2005.Gss")]
    [InlineData("http://www.isotc211.org/2005/gts", "Org.Isotc211._2005.Gts")]
    [InlineData("http://www.isotc211.org/2005/srv", "Org.Isotc211._2005.Srv")]
    [InlineData("http://www.netex.org.uk/netex", "Uk.Org.Netex.Netex")]
    [InlineData("http://www.omg.org/spec/BPMN/20100524/DI", "Org.Omg.Spec.Bpmn._20100524.Di")]
    [InlineData("http://www.omg.org/spec/BPMN/20100524/MODEL", "Org.Omg.Spec.Bpmn._20100524.Model")]
    [InlineData("http://www.omg.org/spec/DD/20100524/DC", "Org.Omg.Spec.Dd._20100524.Dc")]
    [InlineData("http://www.omg.org/spec/DD/20100524/DI", "Org.Omg.Spec.Dd._20100524.Di")]
    [InlineData("http://www.opengis.net/fes/2.0", "Net.Opengis.Fes._2._0")]
    [InlineData("http://www.opengis.net/gml/3.2", "Net.Opengis.Gml._3._2")]
    [InlineData("http://www.opengis.net/ows/1.1", "Net.Opengis.Ows._1._1")]
    [InlineData("http://www.opengis.net/wfs/2.0", "Net.Opengis.Wfs._2._0")]
    [InlineData("http://www.siri.org.uk/siri", "Uk.Org.Siri.Siri")]
    [InlineData("http://www.w3.org/1999/xhtml", "Org.W3._1999.Xhtml")]
    [InlineData("http://www.w3.org/1999/xhtml/datatypes/", "Org.W3._1999.Xhtml.Datatypes")]
    [InlineData("http://www.w3.org/1999/xlink", "Org.W3._1999.Xlink")]
    [InlineData("http://www.w3.org/2001/SMIL20/", "Org.W3._2001.Smil20")]
    [InlineData("http://www.w3.org/2001/XMLSchema", "Org.W3._2001.XmlSchema")]
    [InlineData("http://www.w3.org/2001/XMLSchema-instance", "Org.W3._2001.XmlSchema.Instance")]
    [InlineData("http://www.w3.org/XML/1998/namespace", "Org.W3.Xml._1998.Namespace")]
    [InlineData("http://www.xbrl.org/2003/instance", "Org.Xbrl._2003.Instance")]
    [InlineData("http://www.xbrl.org/2003/linkbase", "Org.Xbrl._2003.Linkbase")]
    [InlineData("http://www.xbrl.org/2003/XLink", "Org.Xbrl._2003.XLink")]
    [InlineData("http://www.xbrl.org/2013/inlineXBRL", "Org.Xbrl._2013.InlineXbrl")]
    [InlineData("http://www.yworks.com/xml/graphml", "Com.Yworks.Xml.Graphml")]
    [InlineData("ttp://rest.immobilienscout24.de/schema/offer/productbookingoverview/1.0", "De.Immobilienscout24.Rest.Schema.Offer.Productbookingoverview._1._0")]
    [InlineData("urn:ietf:params:xml:ns:contact-1.0", "Ietf.Params.Xml.Ns.Contact._1._0")]
    [InlineData("urn:ietf:params:xml:ns:domain-1.0", "Ietf.Params.Xml.Ns.Domain._1._0")]
    [InlineData("urn:ietf:params:xml:ns:epp-1.0", "Ietf.Params.Xml.Ns.Epp._1._0")]
    [InlineData("urn:ietf:params:xml:ns:eppcom-1.0", "Ietf.Params.Xml.Ns.Eppcom._1._0")]
    [InlineData("urn:ietf:params:xml:ns:host-1.0", "Ietf.Params.Xml.Ns.Host._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rde-1.0", "Ietf.Params.Xml.Ns.Rde._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rdeContact-1.0", "Ietf.Params.Xml.Ns.RdeContact._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rdeDnrdCommon-1.0", "Ietf.Params.Xml.Ns.RdeDnrdCommon._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rdeDomain-1.0", "Ietf.Params.Xml.Ns.RdeDomain._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rdeEppParams-1.0", "Ietf.Params.Xml.Ns.RdeEppParams._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rdeHeader-1.0", "Ietf.Params.Xml.Ns.RdeHeader._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rdeHost-1.0", "Ietf.Params.Xml.Ns.RdeHost._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rdeIDN-1.0", "Ietf.Params.Xml.Ns.RdeIdn._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rdeNNDN-1.0", "Ietf.Params.Xml.Ns.RdeNndn._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rdePolicy-1.0", "Ietf.Params.Xml.Ns.RdePolicy._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rdeRegistrar-1.0", "Ietf.Params.Xml.Ns.RdeRegistrar._1._0")]
    [InlineData("urn:ietf:params:xml:ns:rgp-1.0", "Ietf.Params.Xml.Ns.Rgp._1._0")]
    [InlineData("urn:ietf:params:xml:ns:secDNS-1.1", "Ietf.Params.Xml.Ns.SecDns._1._1")]
    [InlineData("www.microsoft.com/SqlServer/Dts", "Com.Microsoft.SqlServer.Dts")]
    [InlineData("www.microsoft.com/sqlserver/dts/tasks/messagequeuetask", "Com.Microsoft.Sqlserver.Dts.Tasks.Messagequeuetask")]
    [InlineData("www.microsoft.com/sqlserver/dts/tasks/sendmailtask", "Com.Microsoft.Sqlserver.Dts.Tasks.Sendmailtask")]
    [InlineData("www.microsoft.com/sqlserver/dts/tasks/sqltask", "Com.Microsoft.Sqlserver.Dts.Tasks.Sqltask")]
    [InlineData("www.microsoft.com/sqlserver/dts/tasks/webservicetask", "Com.Microsoft.Sqlserver.Dts.Tasks.Webservicetask")]
    [InlineData("http://docs.oasis-open.org/codelist/ns/genericode/1.0/", "Org.Oasis.Open.Docs.Codelist.Ns.Genericode._1._0")]
    [InlineData("http://niem.gov/niem/ansi_d20/2.0", "Gov.Niem.Niem.Ansi.D20._2._0")]
    [InlineData("http://niem.gov/niem/ansi-nist/2.0", "Gov.Niem.Niem.Ansi.Nist._2._0")]
    [InlineData("http://niem.gov/niem/appinfo/2.0", "Gov.Niem.Niem.Appinfo._2._0")]
    [InlineData("http://niem.gov/niem/domains/jxdm/4.0", "Gov.Niem.Niem.Domains.Jxdm._4._0")]
    [InlineData("http://niem.gov/niem/domains/screening/2.0", "Gov.Niem.Niem.Domains.Screening._2._0")]
    [InlineData("http://niem.gov/niem/fbi/2.0", "Gov.Niem.Niem.Fbi._2._0")]
    [InlineData("http://niem.gov/niem/fips_10-4/2.0", "Gov.Niem.Niem.Fips._10._4._2._0")]
    [InlineData("http://niem.gov/niem/fips_6-4/2.0", "Gov.Niem.Niem.Fips._6._4._2._0")]
    [InlineData("http://niem.gov/niem/iso_4217/2.0", "Gov.Niem.Niem.Iso._4217._2._0")]
    [InlineData("http://niem.gov/niem/iso_639-3/2.0", "Gov.Niem.Niem.Iso._639._3._2._0")]
    [InlineData("http://niem.gov/niem/niem-core/2.0", "Gov.Niem.Niem.Niem.Core._2._0")]
    [InlineData("http://niem.gov/niem/nonauthoritative-code/2.0", "Gov.Niem.Niem.Nonauthoritative.Code._2._0")]
    [InlineData("http://niem.gov/niem/proxy/xsd/2.0", "Gov.Niem.Niem.Proxy.Xsd._2._0")]
    [InlineData("http://niem.gov/niem/structures/2.0", "Gov.Niem.Niem.Structures._2._0")]
    [InlineData("http://niem.gov/niem/unece_rec20-misc/2.0", "Gov.Niem.Niem.Unece.Rec20.Misc._2._0")]
    [InlineData("http://niem.gov/niem/usps_states/2.0", "Gov.Niem.Niem.Usps.States._2._0")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/GetCase/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.GetCase._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/GetCaseList/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.GetCaseList._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/GetDocument/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.GetDocument._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/GetFeesCalculation/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.GetFeesCalculation._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/GetFilingList/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.GetFilingList._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/GetFilingStatus/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.GetFilingStatus._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/GetPolicy/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.GetPolicy._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/GetServiceInformation/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.GetServiceInformation._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/NotifyDocketingComplete/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.NotifyDocketingComplete._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/NotifyFilingReviewComplete/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.NotifyFilingReviewComplete._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/RecordFiling/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.RecordFiling._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/exchange/ReviewFiling/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Exchange.ReviewFiling._2._3")]
    [InlineData("http://schema.azcourts.az.gov/aoc/efiling/ecf/extension/2.3", "Gov.Az.Azcourts.Schema.Aoc.Efiling.Ecf.Extension._2._3")]
    [InlineData("http://uri.etsi.org/01903/v1.3.2#", "Org.Etsi.Uri._01903.V1._3._2")]
    [InlineData("http://uri.etsi.org/01903/v1.4.1#", "Org.Etsi.Uri._01903.V1._4._1")]
    [InlineData("http://www.w3.org/2000/09/xmldsig#", "Org.W3._2000._09.Xmldsig")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:ApplicationResponse-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.ApplicationResponse._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:AttachedDocument-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.AttachedDocument._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:AwardedNotification-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.AwardedNotification._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:BillOfLading-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.BillOfLading._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CallForTenders-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.CallForTenders._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Catalogue-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.Catalogue._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CatalogueDeletion-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.CatalogueDeletion._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CatalogueItemSpecificationUpdate-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.CatalogueItemSpecificationUpdate._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CataloguePricingUpdate-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.CataloguePricingUpdate._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CatalogueRequest-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.CatalogueRequest._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CertificateOfOrigin-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.CertificateOfOrigin._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.CommonAggregateComponents._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.CommonBasicComponents._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.CommonExtensionComponents._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CommonSignatureComponents-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.CommonSignatureComponents._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:ContractAwardNotice-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.ContractAwardNotice._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:ContractNotice-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.ContractNotice._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.CreditNote._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:DebitNote-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.DebitNote._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.DespatchAdvice._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:DocumentStatus-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.DocumentStatus._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:DocumentStatusRequest-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.DocumentStatusRequest._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:ExceptionCriteria-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.ExceptionCriteria._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:ExceptionNotification-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.ExceptionNotification._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Forecast-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.Forecast._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:ForecastRevision-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.ForecastRevision._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:ForwardingInstructions-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.ForwardingInstructions._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:FreightInvoice-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.FreightInvoice._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:FulfilmentCancellation-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.FulfilmentCancellation._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:GoodsItemItinerary-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.GoodsItemItinerary._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:GuaranteeCertificate-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.GuaranteeCertificate._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:InstructionForReturns-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.InstructionForReturns._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:InventoryReport-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.InventoryReport._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.Invoice._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:ItemInformationRequest-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.ItemInformationRequest._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Order-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.Order._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:OrderCancellation-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.OrderCancellation._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:OrderChange-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.OrderChange._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:OrderResponse-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.OrderResponse._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:OrderResponseSimple-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.OrderResponseSimple._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:PackingList-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.PackingList._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:PriorInformationNotice-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.PriorInformationNotice._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:ProductActivity-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.ProductActivity._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:QualifiedDataTypes-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.QualifiedDataTypes._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Quotation-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.Quotation._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:ReceiptAdvice-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.ReceiptAdvice._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Reminder-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.Reminder._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:RemittanceAdvice-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.RemittanceAdvice._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:RequestForQuotation-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.RequestForQuotation._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:RetailEvent-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.RetailEvent._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:SelfBilledCreditNote-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.SelfBilledCreditNote._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:SelfBilledInvoice-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.SelfBilledInvoice._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:SignatureAggregateComponents-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.SignatureAggregateComponents._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:SignatureBasicComponents-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.SignatureBasicComponents._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Statement-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.Statement._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:StockAvailabilityReport-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.StockAvailabilityReport._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Tender-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.Tender._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TendererQualification-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TendererQualification._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TendererQualificationResponse-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TendererQualificationResponse._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TenderReceipt-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TenderReceipt._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TradeItemLocationProfile-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TradeItemLocationProfile._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TransportationStatus-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TransportationStatus._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TransportationStatusRequest-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TransportationStatusRequest._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TransportExecutionPlan-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TransportExecutionPlan._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TransportExecutionPlanRequest-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TransportExecutionPlanRequest._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TransportProgressStatus-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TransportProgressStatus._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TransportProgressStatusRequest-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TransportProgressStatusRequest._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TransportServiceDescription-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TransportServiceDescription._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:TransportServiceDescriptionRequest-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.TransportServiceDescriptionRequest._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:UnawardedNotification-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.UnawardedNotification._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:UnqualifiedDataTypes-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.UnqualifiedDataTypes._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:UtilityStatement-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.UtilityStatement._2")]
    [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Waybill-2", "Oasis.Names.Specification.Ubl.Schema.Xsd.Waybill._2")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:AppellateCase-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.AppellateCase._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:AppInfo-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.AppInfo._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:BankruptcyCase-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.BankruptcyCase._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CaseListQueryMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.CaseListQueryMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CaseListResponseMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.CaseListResponseMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CaseQueryMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.CaseQueryMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CaseResponseMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.CaseResponseMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CitationCase-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.CitationCase._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CivilCase-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.CivilCase._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CommonTypes-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.CommonTypes._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CoreFilingMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.CoreFilingMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CourtPolicyQueryMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.CourtPolicyQueryMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CourtPolicyResponseMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.CourtPolicyResponseMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CriminalCase-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.CriminalCase._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:DocumentQueryMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.DocumentQueryMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:DocumentResponseMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.DocumentResponseMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:DomesticCase-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.DomesticCase._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FeesCalculationQueryMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.FeesCalculationQueryMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FeesCalculationResponseMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.FeesCalculationResponseMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FilingListQueryMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.FilingListQueryMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FilingListResponseMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.FilingListResponseMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FilingStatusQueryMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.FilingStatusQueryMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FilingStatusResponseMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.FilingStatusResponseMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:JuvenileCase-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.JuvenileCase._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:MessageReceiptMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.MessageReceiptMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:NullSignature-1.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.NullSignature._1._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:PaymentMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.PaymentMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:PaymentReceiptMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.PaymentReceiptMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:RecordDocketingCallbackMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.RecordDocketingCallbackMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:RecordDocketingMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.RecordDocketingMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:ReviewFilingCallbackMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.ReviewFilingCallbackMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:ServiceInformationQueryMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.ServiceInformationQueryMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:ServiceInformationResponseMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.ServiceInformationResponseMessage._4._0")]
    [InlineData("urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:ServiceReceiptMessage-4.0", "Oasis.Names.Tc.Legalxml.Courtfiling.Schema.Xsd.ServiceReceiptMessage._4._0")]
    [InlineData("urn:un:unece:uncefact:data:specification:CoreComponentTypeSchemaModule:2", "Un.Unece.Uncefact.Data.Specification.CoreComponentTypeSchemaModule._2")]
    [InlineData("urn:un:unece:uncefact:documentation:2", "Un.Unece.Uncefact.Documentation._2")]
    [InlineData("urn:un:unece:uncefact:documentation:2", "Test.Un.Unece.Uncefact.Documentation._2", "Test")]
    [InlineData("http://example.com", "Com.Example")]
    [InlineData("http://example.com/", "Com.Example")]
    [InlineData("http://example.com/test", "Com.Example.Test")]
    [InlineData("http://example.com/test/1.0", "Com.Example.Test._1._0")]
    [InlineData("https://sub.domain.example.com/api/v2", "Com.Example.Domain.Sub.Api.V2")]
    [InlineData("ftp://ftp.example.org/resource", "Org.Example.Ftp.Resource")]
    [InlineData("urn:example:namespace", "Example.Namespace")]
    [InlineData("urn:example:namespace:1.2.3", "Example.Namespace._1._2._3")]
    [InlineData("urn:example:namespace:alpha-beta", "Example.Namespace.Alpha.Beta")]
    [InlineData("urn:example:namespace:alpha_beta", "Example.Namespace.Alpha.Beta")]
    [InlineData("urn:example:namespace:2024-06", "Example.Namespace._2024._06")]
    [InlineData("urn:example:namespace:2024_06", "Example.Namespace._2024._06")]
    [InlineData("urn:example:namespace:2024.06", "Example.Namespace._2024._06")]
    [InlineData("urn:example:namespace:1_0_0", "Example.Namespace._1._0._0")]
    [InlineData("urn:example:namespace:1-0-0", "Example.Namespace._1._0._0")]
    [InlineData("urn:example:namespace:1.0.0", "Example.Namespace._1._0._0")]
    [InlineData("urn:example:namespace:alpha.beta.gamma", "Example.Namespace.Alpha.Beta.Gamma")]
    [InlineData("urn:example:namespace:alpha-beta.gamma", "Example.Namespace.Alpha.Beta.Gamma")]
    [InlineData("urn:example:namespace:alpha_beta.gamma", "Example.Namespace.Alpha.Beta.Gamma")]
    [InlineData("urn:example:namespace:alpha.beta-gamma", "Example.Namespace.Alpha.Beta.Gamma")]
    [InlineData("urn:example:namespace:alpha.beta_gamma", "Example.Namespace.Alpha.Beta.Gamma")]
    [InlineData("urn:example:namespace:alpha.beta.gamma-delta", "Example.Namespace.Alpha.Beta.Gamma.Delta")]
    [InlineData("urn:example:namespace:alpha.beta.gamma_delta", "Example.Namespace.Alpha.Beta.Gamma.Delta")]
    [InlineData("urn:example:namespace:alpha.beta.gamma.delta", "Example.Namespace.Alpha.Beta.Gamma.Delta")]
    [InlineData("urn:example:namespace:alpha.beta.gamma.delta", "Test.Example.Namespace.Alpha.Beta.Gamma.Delta", "Test")]
    [InlineData("urn:example:namespace:1.0", "Test.Example.Namespace._1._0", "Test")]
    [InlineData(null, "")]
    [InlineData(null, "", "")]
    [InlineData(null, "Test", "Test")]
    [InlineData("", "", "")]
    [InlineData("", "Test", "Test")]
    [InlineData(" ", " ", " ")]
    [InlineData(" ", "Test", "Test")]
    [InlineData("http://", "")]
    [InlineData("urn:", "")]
    [InlineData(":", "")]
    [InlineData("/", "")]
    [InlineData(".", "")]
    [InlineData("-", "")]
    public void TestGenerateNamespace(string xmlns, string netNs, string namespacePrefix = null)
    {
        var ns = CodeUtilities.GenerateNamespace(xmlns, namespacePrefix);
        Assert.Equal(netNs, ns);
    }
}
