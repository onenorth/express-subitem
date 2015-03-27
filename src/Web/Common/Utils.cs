namespace OneNorth.ExpressSubitem.Common
{
    public static class Utils
    {
        /// <summary>
        /// Parses a string into an int. If it fails, it returns 0
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        internal static int SafeParseInt(string number)
        {
            int retVal;

            if (int.TryParse(number, out retVal))
            {
                return retVal;
            }
            else
            {
                return 0;
            }
        }

    }
}