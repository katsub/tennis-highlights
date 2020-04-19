using System.Globalization;
using System.Xml.Linq;

namespace TennisHighlights
{
    /// <summary>
    /// The XElement extensions
    /// </summary>
    public static class XElementExtensions
    {
        /// <summary>
        /// Gets the string attribute.
        /// </summary>
        /// <param name="xElement">The x element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="defaultValue">The default value.</param>
        public static string GetStringAttribute(this XElement xElement, string attributeName, string defaultValue = null)
        {
            return xElement.Attribute(attributeName)?.Value ?? defaultValue;
        }

        /// <summary>
        /// Gets the int attribute.
        /// </summary>
        /// <param name="xElement">The x element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="defaultValue">The default value.</param>
        public static int GetIntAttribute(this XElement xElement, string attributeName, int defaultValue = 0)
        {
            return int.TryParse(xElement.Attribute(attributeName)?.Value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Gets the double attribute.
        /// </summary>
        /// <param name="xElement">The x element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="defaultValue">The default value.</param>
        public static double GetDoubleAttribute(this XElement xElement, string attributeName, double defaultValue = 0)
        {
            return double.TryParse(xElement.Attribute(attributeName)?.Value, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Gets the bool attribute.
        /// </summary>
        /// <param name="xElement">The x element.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="defaultValue">if set to <c>true</c> [default value].</param>
        public static bool GetBoolAttribute(this XElement xElement, string attributeName, bool defaultValue = false)
        {
            return bool.TryParse(xElement.Attribute(attributeName)?.Value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Gets the string element value.
        /// </summary>
        /// <param name="xElement">The x element.</param>
        /// <param name="elementName">Name of the element.</param>
        /// <param name="defaultValue">The default value.</param>
        public static string GetStringElementValue(this XElement xElement, string elementName, string defaultValue = null)
        {
            return xElement.Element(elementName)?.Attribute("Value").Value ?? defaultValue;
        }

        /// <summary>
        /// Gets the int element value.
        /// </summary>
        /// <param name="xElement">The x element.</param>
        /// <param name="elementName">Name of the element.</param>
        /// <param name="defaultValue">The default value.</param>
        public static int GetIntElementValue(this XElement xElement, string elementName, int defaultValue = 0)
        {
            return int.TryParse(xElement.Element(elementName)?.Attribute("Value").Value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Gets the double element value.
        /// </summary>
        /// <param name="xElement">The x element.</param>
        /// <param name="elementName">Name of the element.</param>
        /// <param name="defaultValue">The default value.</param>
        public static double GetDoubleElementValue(this XElement xElement, string elementName, double defaultValue = 0d)
        {
            return double.TryParse(xElement.Element(elementName)?.Attribute("Value").Value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Gets the bool element value.
        /// </summary>
        /// <param name="xElement">The x element.</param>
        /// <param name="elementName">Name of the element.</param>
        /// <param name="defaultValue">The default value.</param>
        public static bool GetBoolElementValue(this XElement xElement, string elementName, bool defaultValue = false)
        {
            return bool.TryParse(xElement.Element(elementName)?.Attribute("Value").Value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Adds the element with value.
        /// </summary>
        /// <param name="xElement">The x element.</param>
        /// <param name="elementName">Name of the element.</param>
        /// <param name="value">The value.</param>
        public static void AddElementWithValue(this XElement xElement, string elementName, object value) => xElement.Add(new XElement(elementName, new XAttribute("Value", value ?? string.Empty)));
    }
}
