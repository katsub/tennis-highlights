namespace TennisHighlights.Utils
{
    /// <summary>
    /// The conditional compilation variables
    /// </summary>
    public static class ConditionalCompilation
    {
        /// <summary>
        /// The debug
        /// </summary>
        public static bool Debug;

        /// <summary>
        /// Initializes the <see cref="ConditionalCompilation"/> class.
        /// </summary>
        static ConditionalCompilation()
        {
#if DEBUG
            Debug = true;
#endif 
        }
    }
}
