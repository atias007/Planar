using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace BankOfIsraelCurrency
{
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public partial class CURRENCIES
    {
        [XmlElement(DataType = "date")]
        public DateTime LAST_UPDATE { get; set; }

        [XmlElement("CURRENCY")]
        public CURRENCIESCURRENCY[] CURRENCY { get; set; }
    }

    /// <remarks/>
    [SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true)]
    public partial class CURRENCIESCURRENCY
    {
        public string NAME { get; set; }

        public byte UNIT { get; set; }

        public string CURRENCYCODE { get; set; }

        public string COUNTRY { get; set; }

        public decimal RATE { get; set; }

        public decimal CHANGE { get; set; }
    }
}