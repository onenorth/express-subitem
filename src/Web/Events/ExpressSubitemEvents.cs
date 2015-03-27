using OneNorth.ExpressSubitem.FieldTypes;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sitecore.SecurityModel;
using Sitecore.Sites;
using System;
using System.Linq;

namespace OneNorth.ExpressSubitem.Events
{
    public class ExpressSubitemEvents
    {
        /// <summary>
        /// The saved event will propagate publishing settings down to the express subitems
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnItemSaved(object sender, EventArgs args)
        {
            if (args == null) 
                return;

            var savedItem = (Item) Event.ExtractParameter(args, 0);

            //Skip save rules if the item isn't being edited in the content database
            if (SiteContext.Current == null || SiteContext.Current.ContentDatabase == null ||
                savedItem.Database != SiteContext.Current.ContentDatabase) 
                return;

            // Find all fields on the current item that are Express Subitem fields.
            var expressSubitemFields = (from f in savedItem.Fields
                                        where f.Type == "Express Subitem"
                                        select f).ToList();

            //If there are no Express Subitem fields, exit
            if (!expressSubitemFields.Any()) 
                return;

            var changes = (ItemChanges)Event.ExtractParameter(args, 1);

            //Collect the fields we want to copy down to the subitems.
            var fieldsToCopy = (from c in changes.FieldChanges.OfType<FieldChange>()
                                where ExpressSubitemField.PublishingFields.Contains(GetPublishingFieldName(c))
                                select new { c.FieldID, c.Value }).ToList();

            //Find each subitem field
            foreach (var expressSubitemField in expressSubitemFields)
            {
                // Each express subitem field is a multilist.
                MultilistField multilistField = savedItem.Fields[expressSubitemField.ID];

                //Find the related items
                foreach (var itemId in multilistField.Items)
                {
                    ID subitemId;
                    if (!ID.TryParse(itemId, out subitemId))
                        continue;

                    var subitem = savedItem.Database.GetItem(subitemId, savedItem.Language);

                    if (subitem == null) // sub item does not exist yet due to installing items from package. (race condition)
                        continue;

                    // Create the correct version of the subitem if it does not already exist.
                    var hasVersion = subitem.Versions.GetVersionNumbers().Any(v => v.Number == savedItem.Version.Number);
                    if (!hasVersion)
                    {
                        while (subitem.Versions.Count == 0 || subitem.Versions.GetVersionNumbers().Max(v => v.Number) < savedItem.Version.Number)
                        {
                            subitem = subitem.Versions.AddVersion();
                        }
                    }

                    subitem = savedItem.Database.GetItem(subitemId, savedItem.Language, savedItem.Version);

                    if (subitem != null)
                    {
                        //Copy the new values into the field
                        using (new SecurityDisabler())
                        {
                            using (new EditContext(subitem))
                            {
                                fieldsToCopy.ForEach(fieldToCopy =>
                                    {
                                        subitem[fieldToCopy.FieldID] = fieldToCopy.Value;
                                    });
                            }
                        }
                    }
                }
            }
        }

        private static string GetPublishingFieldName(FieldChange fieldChange)
        {
            if (fieldChange != null)
            {
                if (fieldChange.Definition != null)
                {
                    if (!string.IsNullOrEmpty(fieldChange.Definition.Name))
                    {
                        return fieldChange.Definition.Name.ToLower();
                    }
                }
            }

            return "";
        }
    }
}