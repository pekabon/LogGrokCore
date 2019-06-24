namespace LogGrokCore.Data
{
    public static class Align
    {
        public static int Get(int value, int alignment)
        {
            var modulo = value % alignment;
            return modulo == 0 ? value : value + alignment- modulo;
        }
    }
}