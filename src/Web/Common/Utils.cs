using System;
using System.Linq;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;

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

        /// <summary>
        /// Recalculate and update Fields
        /// </summary>
        /// <param name="item"></param>
        public static void RefreshFieldValues(this Item item)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(item, "item");

            //Get fields from item
            //Use item.Template.Fields instead of item.Fields because cloned item.Fields
            //is incomplete (only contains Fields overriden from the source item)
            var fields = item.Template.Fields.Where(x => x.Type == "Express Subitem");

            //for each Express Subitem
            foreach (var field in fields)
            {
                //recalculate value
                var recalculatedValue = RecalculateFieldValue(item, field.Name) ?? String.Empty;
                //compare to current value
                if (item.Fields[field.ID] != null && item.Fields[field.ID].Value != recalculatedValue)
                {
                    //update value if differs
                    using (new SecurityDisabler())
                    {
                        using (new EditContext(item))
                        {
                            item.Fields[field.ID].Value = recalculatedValue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculate what the value of an Express Subitem Field should be from the appropriate child\grandchildren.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private static string RecalculateFieldValue(Item item, string fieldName)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(item, "item");
            Sitecore.Diagnostics.Assert.ArgumentNotNullOrEmpty(fieldName, "fieldName");

            //find child having same name as ItemAggregate Field
            var containerFolder = (item.Children.Where(c => c.Name == fieldName)).FirstOrDefault();
            if (containerFolder == null) { return String.Empty; }

            //return IDs of children of containerFolder 
            return string.Join("|", containerFolder.Children.Select(x => x.ID.ToString()).ToArray());
        } 
    }
}