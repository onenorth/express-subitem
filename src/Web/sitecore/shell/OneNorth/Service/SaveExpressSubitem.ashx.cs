using Newtonsoft.Json;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Resources.Media;
using Sitecore.Shell.Applications.ContentEditor;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace OneNorth.ExpressSubitem.sitecore.shell.OneNorth.Service
{
    /// <summary>
    /// Summary description for SaveExpressSubitem
    /// </summary>
    public class SaveExpressSubitem : IHttpHandler
    {
        public class ItemDetails
        {
            public Guid ItemId { get; set; }
            public string Language { get; set; }
            public string Version { get; set; }
            public string NameFormatString { get; set; }
            public FieldValue[] Fields { get; set; }
        }

        public class FieldValue
        {
            public Guid FieldId { get; set; }
            public string Value { get; set; }
        }

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                ItemDetails itemDetails;

                //Parse the JSON
                using (var sr = new StreamReader(context.Request.InputStream))
                {
                    itemDetails = JsonConvert.DeserializeObject<ItemDetails>(sr.ReadToEnd());
                }

                var editingLanguage = !string.IsNullOrEmpty(itemDetails.Language) ? LanguageManager.GetLanguage(itemDetails.Language) : LanguageManager.DefaultLanguage;

                var itemVersion = !string.IsNullOrEmpty(itemDetails.Version) ? new Sitecore.Data.Version(itemDetails.Version) : new Sitecore.Data.Version(1);

                //Load the item
                var currentItem = Context.ContentDatabase.GetItem(new ID(itemDetails.ItemId), editingLanguage, itemVersion);

                using (new EditContext(currentItem, false, false))
                {
                    currentItem["__Updated"] = DateUtil.IsoNow;
                    currentItem.Fields["__Updated by"].SetValue(Context.User.Name, true);

                    foreach (var clientFieldValue in itemDetails.Fields)
                    {
                        var field = currentItem.Fields[new ID(clientFieldValue.FieldId)];

                        if (!string.IsNullOrEmpty(clientFieldValue.Value))
                        {
                            switch (field.Type)
                            {
                                case "Date":
                                case "Datetime":
                                    field.Value = DateUtil.ToIsoDate(DateUtil.ParseDateTime(clientFieldValue.Value, System.DateTime.MinValue));
                                    break;
                                case "Droptree":
                                    //The value for the tree is: [Source ID]|[Path to the item]
                                    var values = clientFieldValue.Value.Split(new [] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                                    var sourceItem = currentItem.Database.GetItem(new ID(values[0]));

                                    if (sourceItem != null)
                                    {
                                        if (values.Length == 1)
                                        {
                                            field.Reset();
                                        }
                                        else
                                        {
                                            var selectedItemName = values[1].Split('/').LastOrDefault();

                                            //Get Source Path then add the Path to the item
                                            var itemPath = sourceItem.Parent.Parent.Paths.Path + "/" + values[1];

                                            //Remove the display name from the item path
                                            itemPath = itemPath.Replace(selectedItemName, "");

                                            //Get The Parent Tree Item
                                            var dropTreeSelectedItemParent = sourceItem.Database.GetItem(itemPath);

                                            var childItem = (from i in dropTreeSelectedItemParent.Children
                                                              where i.DisplayName == selectedItemName
                                                              select i).FirstOrDefault();

                                            if (childItem != null)
                                            {
                                                field.Value = childItem.ID.ToString();
                                            }

                                        }
                                    }

                                    break;
                                case "File":
                                case "Image":
                                    var mediaPath = "/sitecore/Media Library" + clientFieldValue.Value;
                                    MediaItem mediaItem = currentItem.Database.GetItem(mediaPath);

                                    var xmlValue = new XmlValue("", field.Type.ToLower());

                                    if (mediaItem != null)
                                    {
                                        var shellOptions = MediaUrlOptions.GetShellOptions();
                                        var mediaUrl = MediaManager.GetMediaUrl(mediaItem, shellOptions);

                                        xmlValue.SetAttribute("mediaid", mediaItem.ID.ToString());
                                        xmlValue.SetAttribute("mediapath", mediaItem.MediaPath);
                                        xmlValue.SetAttribute("src", mediaUrl);
                                    }
                                    else
                                    {
                                        xmlValue.SetAttribute("mediaid", string.Empty);
                                        xmlValue.SetAttribute("mediapath", string.Empty);
                                        xmlValue.SetAttribute("src", string.Empty);
                                    }

                                    field.Value = xmlValue.ToString();

                                    break;
                                default:
                                    field.Value = clientFieldValue.Value;
                                    break;
                            }
                        }
                        else
                        {
                            field.Reset();
                        }
                    }

                    if (!string.IsNullOrEmpty(itemDetails.NameFormatString))
                    {
                        var displayName = BuildName(itemDetails.NameFormatString, currentItem).Trim();
                        var name = ItemUtil.ProposeValidItemName(displayName, "Unnamed item");

                        string uniqueName = ItemUtil.GetUniqueName(currentItem.Parent, name);

                        if (!string.IsNullOrEmpty(name))
                        {
                            currentItem["__Display name"] = displayName;
                            currentItem.Name = uniqueName;
                        }
                    }
                }

                context.Response.Write(JsonConvert.SerializeObject(new
                {
                    displayName = string.IsNullOrEmpty(currentItem["__Display name"]) ? currentItem.Name : currentItem["__Display name"]
                }));
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Fatal("Error saving sub-item", ex, this);

                context.Response.StatusCode = 500;
                context.Response.Write(string.Format("Exception {0}({1}):\n{2}", ex.Message, ex.GetType().Name, ex.StackTrace));
            }
        }

        private static string BuildName(string nameFormatString, Item currentItem)
        {
            return Regex.Replace(nameFormatString, "{([^}]*)}", delegate(Match m)
            {
                var field = currentItem.Fields[m.Value.Substring(1, m.Value.Length - 2)];

                switch (field.Type)
                {
                    case "Date":
                    case "Datetime":
                    case "Single-Line Text":
                    case "Integer":
                        return field.Value;
                    case "Droptree":
                    case "Droplink":
                        var lookupField = (LookupField)field;

                        if (lookupField == null || lookupField.TargetItem == null)
                        {
                            return "";
                        }

                        return lookupField.TargetItem.DisplayName;
                }

                return "?";
            });
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}