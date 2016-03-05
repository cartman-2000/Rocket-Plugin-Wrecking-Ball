using System;
using System.Xml.Serialization;

namespace ApokPT.RocketPlugins
{
    public class Element
    {
        public Element() { }
        internal Element(ushort ID, char Categoryid)
        {
            Id = ID;
            CategoryId = Categoryid;
        }

        [XmlAttribute]
        public ushort Id { get; private set; }
        [XmlIgnore]
        public char CategoryId { get; private set; }
        [XmlAttribute("CategoryId")]
        public string XmlCategoryId
        {
            get
            {
                return CategoryId.ToString();
            }
            private set
            {
                char id;
                if (!char.TryParse(value, out id))
                    CategoryId = '*';
                else
                    CategoryId = id;
            }
        }
    }
}