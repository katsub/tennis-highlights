namespace TennisHighlights
{
    /// <summary>
    /// The color correction settings.
    /// </summary>
    public class ColorCorrectionSettings
    {
        /// <summary>
        /// Gets the saturation.
        /// </summary>
        public int Saturation { get; set; }
        /// <summary>
        /// Gets the brightness.
        /// </summary>
        public int Brightness { get; set; }
        /// <summary>
        /// Gets the contrast.
        /// </summary>
        public int Contrast { get; set; }
        /// <summary>
        /// Gets the warm color.
        /// </summary>
        public int WarmColor { get; set; }
        /// <summary>
        /// Gets the tone color.
        /// </summary>
        public int ToneColor { get; set; }
    
        /// <summary>
        /// Initializes a new instance of the ColorCorrectionSettings class
        /// </summary>
        public ColorCorrectionSettings() { }

        /// <summary>
        /// Initializes a new instance of the ColorCorrectionSettings class
        /// </summary>
        /// <param name="original">The original settings.</param>
        public ColorCorrectionSettings(ColorCorrectionSettings original)
        {
            Brightness = original.Brightness;
            Contrast = original.Contrast;
            Saturation = original.Saturation;
            WarmColor = original.WarmColor;
            ToneColor = original.ToneColor;
        }

        /// <summary>
        /// Compares this instance to a given instance
        /// </summary>
        /// <param name="obj">The instance to compare</param>
        public override bool Equals(object obj)
        {
            return obj is ColorCorrectionSettings other && Brightness == other.Brightness
                                                        && Saturation == other.Saturation
                                                        && Contrast == other.Contrast
                                                        && WarmColor == other.WarmColor
                                                        && ToneColor == other.ToneColor;
        }

        /// <summary>
        /// Gets this instace hash code
        /// </summary>
        public override int GetHashCode()
        {
            return Brightness.GetHashCode() 
                   ^ Saturation.GetHashCode() 
                   ^ Contrast.GetHashCode() 
                   ^ WarmColor.GetHashCode() 
                   ^ ToneColor.GetHashCode();
        }
    }
}
