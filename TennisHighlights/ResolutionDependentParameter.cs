using System;

namespace TennisHighlights
{
    /// <summary>
    /// The resolution dependent parameter. Automatically converts itself to the target resolution.
    /// </summary>
    public class ResolutionDependentParameter
    {
        /// <summary>
        /// The reference resolution height. Most parameters were created when using this height (from 1280x720). This should 
        /// not be changed.
        /// </summary>
        private const double _referenceResolutionHeight = 720d;
        /// <summary>
        /// The scale factor
        /// </summary>
        private static double _scaleFactor = double.NaN;

        /// <summary>
        /// The dependency exponent. This tells us how much this parameter depends on the height (1 for length and 2 for area, for example)
        /// (it's the length dimension of the parameter)
        /// </summary>
        private readonly double _dependencyExponent;
        /// <summary>
        /// The reference value. This is the value this parameter should have when the processed image has the reference resolution height
        /// </summary>
        private readonly double _referenceValue;
        /// <summary>
        /// Gets the cached value.
        /// </summary>
        public double Value => _referenceValue * Math.Pow(_scaleFactor, _dependencyExponent);
        /// <summary>
        /// Gets the int value.
        /// </summary>
        public int IntValue => (int)Math.Round(Value);

        /// <summary>
        /// Sets the target resolutionheight.
        /// </summary>
        /// <param name="targetResolutionHeight">Height of the target resolution.</param>
        public static void SetTargetResolutionheight(double targetResolutionHeight) => _scaleFactor = targetResolutionHeight / _referenceResolutionHeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResolutionDependentParameter" /> class.
        /// </summary>
        /// <param name="referenceValue">The reference value.</param>
        /// <param name="dependendyExponent">The dependency exponent.</param>
        /// <exception cref="System.Exception">Tried to create resolution dependent parameter before the class was initialized</exception>
        public ResolutionDependentParameter(double referenceValue, double dependencyExponent)
        {
            _referenceValue = referenceValue;
            _dependencyExponent = dependencyExponent;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString() => $"Value: {Value}, Int: {IntValue}, Reference: {_referenceValue}";
    }
}
