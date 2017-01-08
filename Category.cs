using Rocket.Core.Logging;
using System;
using System.Xml.Serialization;

namespace ApokPT.RocketPlugins
{
    public class Category
    {
        public Category() { }
        internal Category(char id, string name, ConsoleColor color)
        {
            Id = id;
            Name = name;
            Color = color;
        }

        [XmlIgnore]
        public char Id { get; set; }
        [XmlAttribute("Id")]
        public string XmlId
        {
            get
            {
                return Id.ToString();
            }
            set
            {
                char id;
                if (!char.TryParse(value, out id))
                    Id = '*';
                else
                    Id = id;
            }
        }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlIgnore]
        public ConsoleColor Color { get; set; }
        [XmlAttribute("Color")]
        public string XmlColor
        {
            get
            {
                return Enum.GetName(typeof(ConsoleColor), Color);
            }
            set
            {
                try
                {
                    Color = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), value, true);
                }
                catch
                {
                    Logger.LogWarning("Warning: Invalid Color Name, falling back to white.");
                    Color = ConsoleColor.White;
                }
            }
        }
    }
}
