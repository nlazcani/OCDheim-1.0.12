namespace OCDheim
{

    public static class HeightClipboard
    {
        public static bool hasSavedHeight { get; private set; }
        public static float savedHeight { get; private set; }

        public static void Save(float groundHeight)
        {
            savedHeight = groundHeight;
            hasSavedHeight = true;
        }

        public static float MissingTo(float groundHeight) => savedHeight - groundHeight;
    }
}
