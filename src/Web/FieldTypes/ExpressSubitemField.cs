using OneNorth.ExpressSubitem.Common;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.HtmlControls.Data;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.HtmlControls;

namespace OneNorth.ExpressSubitem.FieldTypes
{
    public class ExpressSubitemField : Input, IContentField
    {
        public static HashSet<string> PublishingFields = new HashSet<string>(
            new [] 
            { 
                "__valid to", 
                "__hide version", 
                "__valid from", 
                "__publish", 
                "__never publish", 
                "__unpublish", 
                "__workflow state",
                "__workflow",
                "__lock" 
            });
        static readonly ID FOLDER_ITEM_TEMPLATE_ID = new ID("{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}");

        public ExpressSubitemField()
        {
            Activation = true;
        }

        #region Properties
        //These properties are set by Sitecore
        public string ItemID
        {
            get
            {
                return (ViewState["ItemID"] ?? "").ToString();
            }
            set
            {
                ViewState["ItemID"] = value;
            }
        }

        public string ItemVersion
        {
            get
            {
                return (ViewState["ItemVersion"] ?? "").ToString();
            }
            set
            {
                ViewState["ItemVersion"] = value;
            }
        }

        public string ItemLanguage
        {
            get
            {
                return (ViewState["ItemLanguage"] ?? "").ToString();
            }
            set
            {
                ViewState["ItemLanguage"] = value;
            }
        }

        public string FieldID
        {
            get
            {
                return (ViewState["FieldID"] ?? "").ToString();
            }
            set
            {
                ViewState["FieldID"] = value;
            }
        }

        public string Source
        {
            get
            {
                return (ViewState["Source"] ?? "").ToString();
            }
            set
            {
                ViewState["Source"] = value;
            }
        }

        #endregion

        #region Internal Properties
        Language _currentLanguage;
        Language CurrentLanguage
        {
            get { return _currentLanguage ?? (_currentLanguage = LanguageManager.GetLanguage(ItemLanguage)); }
        }

        Item _currentItem;
        Item CurrentItem
        {
            get { return _currentItem ?? (_currentItem = Sitecore.Context.ContentDatabase.GetItem(new ID(ItemID), CurrentLanguage)); }
        }

        string TemplatePath
        {
            get
            {
                return (ViewState["TemplatePath"] ?? "").ToString();
            }
            set
            {
                ViewState["TemplatePath"] = value;
            }
        }

        string NameFormatString
        {
            get
            {
                return (ViewState["NameFormatString"] ?? "").ToString();
            }
            set
            {
                ViewState["NameFormatString"] = value;
            }
        }

        ID ContainerId
        {
            get
            {
                return ViewState["ContainerId"] as ID;
            }
            set
            {
                ViewState["ContainerId"] = value;
            }
        }
        #endregion

        protected override void OnLoad(EventArgs e)
        {
            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                ParseSource();
                BuildControl();
            }

            base.OnLoad(e);
        }

        protected override bool LoadPostData(string value)
        {
            if (Value != value)
            {
                Value = value;
            }

            return base.LoadPostData(value);
        }

        private void ParseSource()
        {
            var parsedSource = new UrlString(Source);

            TemplatePath = parsedSource["template"];
            NameFormatString = parsedSource["name"];
        }

        /// <summary>
        /// Builds the control in the output stream
        /// </summary>
        private void BuildControl()
        {
            if (!IsContentItem())
            {
                var baseDiv = new HtmlGenericControl("div")
                    {
                        InnerText = "Express Subitems can not be edited on __Standard Values"
                    };

                Controls.Add(baseDiv);
            }
            else
            {
                //Create the containing DIV
                var baseDiv = new HtmlGenericControl("div");

                var baseClass = "express-subitem";

                if (ReadOnly)
                {
                    baseDiv.Attributes.Add("disabled", "disabled");
                    baseClass += " express-subitem-disabled";
                }

                baseDiv.ID = ID + "_Wrapper";
                baseDiv.Attributes.Add("class", baseClass);
                baseDiv.Attributes.Add("controlID", ID);
                baseDiv.Attributes.Add("language", ItemLanguage);
                baseDiv.Attributes.Add("nameFormatString", NameFormatString);

                Controls.Add(baseDiv);

                //Add a container for each item
                var subitems = GetSubitems();

                foreach (var subitem in subitems)
                {
                    var row = new Literal
                        {
                            Text = string.Format((Disabled) ? Prototypes.ItemRowDisabled : Prototypes.ItemRow, subitem.ID.Guid, subitem.DisplayName, "", "", false, ItemVersion)
                        };

                    baseDiv.Controls.Add(row);
                }

                if (!Disabled)
                {
                    var addNew = new Literal {Text = string.Format(Prototypes.AddExpressSubitem, ID)};
                    baseDiv.Controls.Add(addNew);
                }

                var hiddenField = new Literal(string.Format("<input ID='{0}' type='hidden' name='{0}' value='{1}'/>", ID, GetValue()));
                baseDiv.Controls.Add(hiddenField);
            }
        }

        private bool IsContentItem()
        {
            return CurrentItem.Name != "__Standard Values";
        }

        /// <summary>
        /// The aggrigate items under the folder are the definitive copy of what should go into the list of aggrigate items.
        /// </summary>
        /// <returns></returns>
        private List<Item> GetSubitems()
        {
            var fieldName = CurrentItem.Fields[new ID(FieldID)].Name;

            var containerFolder = (from c in CurrentItem.Children
                                    where c.Name == fieldName
                                    select c).FirstOrDefault();

            //Create the container
            if (containerFolder == null)
            {
                TemplateItem folderTemplate = CurrentItem.Database.GetItem(FOLDER_ITEM_TEMPLATE_ID);

                if (folderTemplate == null)
                {
                    throw new ApplicationException("Can't find folder template");
                }

                using (new SecurityDisabler())
                {
                    using (new EventDisabler())
                    {
                        containerFolder = ItemManager.CreateItem(fieldName, CurrentItem, folderTemplate.ID);
                    }

                    //Hide the folder
                    using (new EditContext(containerFolder, false, false))
                    {
                        containerFolder["__Hidden"] = "1";
                        containerFolder["__Display name"] = CurrentItem.Fields[new ID(FieldID)].Title;
                    }
                }
            }
            else
            {
                //See if the existing folder is hidden. If not, hide it
                if (containerFolder["__Hidden"] != "1")
                {
                    using (new SecurityDisabler())
                    {
                        //Hide the folder
                        using (new EditContext(containerFolder, false, false))
                        {
                            containerFolder["__Hidden"] = "1";
                        }
                    }
                }
            }

            //Make sure the folder has a version for publishing
            if (containerFolder.Versions.Count == 0)
            {
                containerFolder.Versions.AddVersion();
            }

            //Make sure all child items have a version. This will ensure the item exists in the language of the parent
            foreach (Item childItem in containerFolder.Children)
            {
                if (childItem.Versions.Count == 0)
                {
                    childItem.Versions.AddVersion();
                }
            }

            ContainerId = containerFolder.ID;

            return containerFolder.Children.ToList();
        }

        #region Client Functions
        /// <summary>
        /// Builds the content for the control. This is called from JavaScript on the client
        /// </summary>
        /// <param name="id"></param>
        public void LoadContent(string id)
        {
            var subitem = CurrentItem.Database.GetItem(new ID(id), CurrentLanguage, new Sitecore.Data.Version(ItemVersion));

            //See if this version exists
            if (!(from i in subitem.Versions.GetVersions()
                  where i.Version == subitem.Version
                  select i).Any())
            {
                //If the version doesn't exist, copy the field values from the most recent version
                var latestVersion = subitem.Versions.GetLatestVersion(CurrentLanguage);

                using (new EditContext(subitem, false, false))
                {
                    foreach (Field field in latestVersion.Fields)
                    {
                        if (!field.Name.StartsWith("__") && !field.Shared && !field.Unversioned)
                        {
                            subitem[field.ID] = latestVersion[field.ID];
                        }
                    }
                }
            }

            var resultText = new StringBuilder();

            InternalFieldContentBuilder(subitem, resultText);

            SheerResponse.Insert("ExpressSubitemContent_" + subitem.ID.Guid.ToString("N"), "append", resultText.ToString());
        }

        /// <summary>
        /// Builds content for internal fields
        /// </summary>
        /// <param name="subitem"></param>
        /// <param name="resultText"></param>
        private void InternalFieldContentBuilder(Item subitem, StringBuilder resultText)
        {
            //Get all the fields for an item
            var fields = subitem.Fields;
            fields.ReadAll();
            fields.Sort();

            foreach (Field field in fields)
            {
                if (!field.Name.StartsWith("__"))
                {
                    string fieldSource = DetermineFieldSource(field);

                    switch (field.Type)
                    {
                        case "Droptree":
                            var treeCtrl = new Tree();

                            treeCtrl.ItemID = subitem.ID.ToString();
                            treeCtrl.ID = string.Format("ExpressSubitem_{0:N}{1:N}", field.ID.Guid, subitem.ID.Guid);
                            treeCtrl.Source = field.Source;
                            treeCtrl.Disabled = Disabled;

                            var sources = GetSources(field);
                            var sourceId = Guid.Empty;

                            if (sources.Any())
                            {
                                sourceId = sources[0].ID.Guid;
                            }

                            Sitecore.Context.ClientPage.AddControl(this, treeCtrl);
                            treeCtrl.Value = field.Value;

                            resultText.AppendFormat(Prototypes.Tree, field.DisplayName, treeCtrl.RenderAsText(), treeCtrl.ID, field.ID, sourceId, fieldSource);

                            break;
                        case "File":
                            var fileCtrl = new File();

                            fileCtrl.ID = string.Format("ExpressSubitem_{0:N}{1:N}", field.ID.Guid, subitem.ID.Guid);

                            Sitecore.Context.ClientPage.AddControl(this, fileCtrl);
                            fileCtrl.SetValue(field.Value);
                            fileCtrl.Disabled = Disabled;

                            resultText.AppendFormat(Prototypes.StandardWrapper,
                                field.DisplayName,
                                fileCtrl.RenderAsText(),
                                fileCtrl.ID,
                                field.ID,
                                RenderMenuButtons(fileCtrl.ID, field, ReadOnly),
                                "file",
                                fieldSource);

                            break;
                        case "Image":
                            var imageCtrl = new Sitecore.Shell.Applications.ContentEditor.Image();

                            imageCtrl.ID = string.Format("ExpressSubitem_{0:N}{1:N}", field.ID.Guid, subitem.ID.Guid);

                            Sitecore.Context.ClientPage.AddControl(this, imageCtrl);
                            imageCtrl.ItemLanguage = ItemLanguage;
                            imageCtrl.ItemVersion = ItemVersion;
                            imageCtrl.SetValue(field.Value);
                            imageCtrl.Disabled = Disabled;

                            resultText.AppendFormat(Prototypes.StandardWrapper,
                                field.DisplayName,
                                imageCtrl.RenderAsText(),
                                imageCtrl.ID,
                                field.ID,
                                RenderMenuButtons(imageCtrl.ID, field, ReadOnly),
                                "image",
                                fieldSource);

                            break;
                        case "Multilist":
                            var multiListCtrl = new MultilistEx();

                            multiListCtrl.ItemID = subitem.ID.ToString();
                            multiListCtrl.ID = string.Format("ExpressSubitem_{0:N}{1:N}", field.ID.Guid, subitem.ID.Guid);
                            multiListCtrl.SetValue(field.Value);
                            multiListCtrl.Source = field.Source;
                            Sitecore.Context.ClientPage.AddControl(this, multiListCtrl);
                            multiListCtrl.Disabled = Disabled;
                            multiListCtrl.ItemLanguage = subitem.Language.Name;

                            resultText.AppendFormat(Prototypes.StandardWrapper,
                                field.DisplayName,
                                multiListCtrl.RenderAsText(),
                                multiListCtrl.ID,
                                field.ID,
                                RenderMenuButtons(multiListCtrl.ID, field, ReadOnly),
                                "multilist",
                                fieldSource);

                            break;
                        case "Treelist":
                            var treelistCtrl = new TreeList();

                            treelistCtrl.ItemID = subitem.ID.ToString();
                            treelistCtrl.ID = string.Format("ExpressSubitem_{0:N}{1:N}", field.ID.Guid, subitem.ID.Guid);
                            treelistCtrl.ItemLanguage = ItemLanguage;
                            treelistCtrl.SetValue(field.Value);
                            treelistCtrl.Source = field.Source;
                            treelistCtrl.Disabled = Disabled;

                            Sitecore.Context.ClientPage.AddControl(this, treelistCtrl);

                            resultText.AppendFormat(Prototypes.StandardWrapper,
                                field.DisplayName,
                                treelistCtrl.RenderAsText(),
                                treelistCtrl.ID,
                                field.ID,
                                RenderMenuButtons(treelistCtrl.ID, field, ReadOnly),
                                "treelist",
                                fieldSource);

                            break;
                        case "Datetime":
                        case "Date":
                            var dateCtrl = field.Type == "Date" ? new Date() : new Sitecore.Shell.Applications.ContentEditor.DateTime();

                            dateCtrl.ItemID = subitem.ID.ToString();
                            dateCtrl.ID = string.Format("ExpressSubitem_{0:N}{1:N}", field.ID.Guid, subitem.ID.Guid);
                            dateCtrl.Disabled = Disabled;

                            Sitecore.Context.ClientPage.AddControl(this, dateCtrl);
                            dateCtrl.SetValue(field.Value);

                            resultText.AppendFormat(Prototypes.Date, 
                                field.DisplayName, 
                                dateCtrl.RenderAsText(), 
                                dateCtrl.ID,
                                field.ID,
                                RenderMenuButtons(dateCtrl.ID, field, ReadOnly),
                                fieldSource);

                            break;
                        case "Rich Text":
                            var richTextCtrl = new RichText();
                            richTextCtrl.ItemID = subitem.ID.ToString();
                            richTextCtrl.ItemLanguage = ItemLanguage;
                            richTextCtrl.ItemVersion = subitem.Version.ToString();
                            richTextCtrl.FieldID = field.ID.ToString();
                            richTextCtrl.ID = string.Format("ExpressSubitem_{0:N}{1:N}", field.ID.Guid, subitem.ID.Guid);
                            richTextCtrl.Disabled = Disabled;

                            Sitecore.Context.ClientPage.AddControl(this, richTextCtrl);
							// Fixing Sitecore bug for no value field
                            if (field.Value == "__#!$No value$!#__")
                            {
                                using (new SecurityDisabler())
                                {
                                    using (new EditContext(subitem))
                                    {
                                        field.Value = "";
                                    }
                                }
                            }
                            richTextCtrl.Value = field.Value;

                            resultText.AppendFormat(Prototypes.RichText,
                                field.DisplayName,
                                richTextCtrl.RenderAsText(),
                                richTextCtrl.ID,
                                field.ID,
                                RenderMenuButtons(richTextCtrl.ID, field, ReadOnly),
                                fieldSource);

                            break;
                        case "Single-Line Text":
                        case "Integer":
                            resultText.AppendFormat(Prototypes.TextBox, field.DisplayName, field.ID, field.Value, Disabled ? "disabled='disabled'" : "", fieldSource);

                            break;
                        case "Droplink":
                            bool selectionInList;

                            string options = BuildDropdownOptions(field, out selectionInList);
                            string errorMessage = "";

                            if (!selectionInList)
                            {
                                errorMessage = @"<DIV style=""PADDING-BOTTOM: 0px; PADDING-LEFT: 0px; PADDING-RIGHT: 0px; COLOR: #999999; PADDING-TOP: 2px"">The field contains a value that is not in the selection list.</DIV>";
                            }

                            resultText.AppendFormat(Prototypes.Dropdown, field.DisplayName, field.ID, options, Disabled ? "disabled='disabled'" : "", errorMessage, fieldSource);

                            break;
                        case "General Link":
                            var link = new Link();
                            
                            link.ID = string.Format("ExpressSubitem_{0:N}{1:N}", field.ID.Guid, subitem.ID.Guid);
                            link.Source = field.Source;
                            link.Disabled = Disabled;
                            link.SetValue(field.Value);

                            Sitecore.Context.ClientPage.AddControl(this, link);

                            link.ItemLanguage = subitem.Language.Name;
                            resultText.AppendFormat(Prototypes.StandardWrapper,
                                field.DisplayName,
                                link.RenderAsText(), 
                                link.ID,
                                field.ID,
                                RenderMenuButtons(link.ID, field, ReadOnly),
                                "generallink", 
                                fieldSource);
                            break;
                        default:
                            resultText.AppendFormat("<div class=\"unknown-field\">Unknown field type of \"{0}\" for field \"{1}\"</div>", field.Type, field.Name);

                            break;
                    }
                }
            }
        }

        private static string DetermineFieldSource(Field field)
        {
            var fieldSource = new StringBuilder();
            var addCommaFlag = false;

            if (field.Unversioned)
            {
                fieldSource.Append(Translate.Text("unversioned"));
                addCommaFlag = true;
            }

            if (field.Shared)
            {
                if (addCommaFlag)
                {
                    fieldSource.Append(",");
                }

                fieldSource.Append(Translate.Text("shared"));
                addCommaFlag = true;
            }

            if (field.InheritsValueFromOtherItem)
            {
                if (addCommaFlag)
                {
                    fieldSource.Append(",");
                }

                fieldSource.Append(Translate.Text("original value"));
                addCommaFlag = true;
            }

            if (field.ContainsStandardValue)
            {
                if (addCommaFlag)
                {
                    fieldSource.Append(",");
                }

                fieldSource.Append(Translate.Text("standard value"));
            }

            if (fieldSource.Length > 0)
            {
                fieldSource.Insert(0, "<span class=scEditorFieldLabelAdministrator>[");
                fieldSource.Append("]</span>");
            }

            return fieldSource.ToString();
        }

        private object RenderMenuButtons(string controlId, Field field, bool readOnly)
        {
            var menuControls = new StringBuilder();

            var fieldTypeItem = FieldTypeManager.GetFieldTypeItem(field.Type);
            var menu = fieldTypeItem.Children["Menu"];

            if (menu != null)
            {
                menuControls.Append("<div class=\"scContentButtons\">");

                bool seperatorFlag = false;

                foreach (Item menuItem in menu.Children)
                {
                    var callbackEvent = Sitecore.Context.ClientPage.GetClientEvent(menuItem["Message"]).Replace("$Target", controlId);

                    if (seperatorFlag)
                    {
                        menuControls.Append("&#183;");
                    }

                    seperatorFlag = true;

                    if (readOnly)
                    {
                        menuControls.AppendFormat("<span class=\"scContentButtonDisabled\">{0}</span>", menuItem["Display Name"]);
                    }
                    else
                    {
                        menuControls.AppendFormat("<a href=\"#\" class=\"scContentButton\" onclick=\"{0}\">{1}</a>", callbackEvent, menuItem["Display Name"]);
                    }
                }

                menuControls.Append("</div>");
            }

            return menuControls.ToString();
        }

        /// <summary>
        /// Creates a new item in the container
        /// </summary>
        /// <returns></returns>
        public void AddNewItem()
        {
            TemplateItem newItemTemplate = CurrentItem.Database.GetItem(TemplatePath, CurrentLanguage);

            if (newItemTemplate == null)
            {
                throw new ApplicationException("Can't find new item template");
            }

            var container = CurrentItem.Database.GetItem(ContainerId);
            Item newItem;

            using (new EventDisabler())
            {
                newItem = container.Add("New " + newItemTemplate.Name, newItemTemplate);

                if (newItem.Version != CurrentItem.Version)
                {
                    newItem = CurrentItem.Database.GetItem(newItem.ID, CurrentItem.Language, CurrentItem.Version);

                    using (new EditContext(newItem, false, false))
                    {
                        newItem["__Created"] = DateUtil.IsoNow;
                        newItem["__Created by"] = Sitecore.Context.User.Name;
                    }
                }
            }

            //Figure out the sort order value to put the item last
            int? newSortOrder = (from i in container.Children
                                 select Utils.SafeParseInt(i["__sortorder"])).Max();

            if (newSortOrder.HasValue)
            {
                newSortOrder = newSortOrder + 100;
            }
            else
            {
                newSortOrder = 100;
            }

            using (new EditContext(newItem, false, false))
            {
                newItem["__Sortorder"] = newSortOrder.Value.ToString();

                //Copy the publishing Fields
                foreach (string publishingFieldName in PublishingFields)
                {
                    if (CurrentItem.Fields[publishingFieldName].HasValue)
                    {
                        newItem[publishingFieldName] = CurrentItem[publishingFieldName];
                    }
                }
            }

            var itemFieldContent = new StringBuilder();
            InternalFieldContentBuilder(newItem, itemFieldContent);

            SheerResponse.SetReturnValue(string.Format(Prototypes.ItemRow, newItem.ID.Guid, newItem.DisplayName, " expanded downloaded", itemFieldContent, true, ItemVersion));
        }

        /// <summary>
        /// Opens the reset dialog
        /// </summary>
        /// <param name="itemId"></param>
        public void ResetSubItem(string itemId)
        {
            var resetDialog = new UrlString(UIUtil.GetUri("control:ResetFields"));
            resetDialog.Add("id", itemId);
            resetDialog.Add("la", CurrentLanguage.ToString());
            resetDialog.Add("vs", ItemVersion);

            SheerResponse.ShowModalDialog(resetDialog.ToString(), false);            
        }

        /// <summary>
        /// Deletes a subitem from the express subitem control
        /// </summary>
        /// <param name="itemId"></param>
        public void DeleteSubItem(string itemId)
        {
            var itemToRemove = CurrentItem.Database.GetItem(new ID(itemId), CurrentLanguage);

            if (Sitecore.Configuration.Settings.RecycleBinActive)
            {
                itemToRemove.Recycle();
            }
            else
            {
                itemToRemove.Delete();
            }
        }

        public void MoveUpSubItem(string itemId)
        {
            var itemToMoveUp = CurrentItem.Database.GetItem(new ID(itemId), CurrentLanguage);

            Sorting.MoveUp(new [] { itemToMoveUp });
        }

        public void MoveDownSubItem(string itemId)
        {
            var itemToMoveDown = CurrentItem.Database.GetItem(new ID(itemId), CurrentLanguage);

            Sorting.MoveDown(new [] { itemToMoveDown });
        }
        #endregion

        /// <summary>
        /// Constructs a list of options for a dropdown item
        /// </summary>
        /// <param name="field"></param>
        /// <param name="selectionInList"></param>
        /// <returns></returns>
        private string BuildDropdownOptions(Field field, out bool selectionInList)
        {
            var fieldChoices = GetSources(field);

            var dropdownOptions = new StringBuilder();

            dropdownOptions.AppendFormat("<option value=''{0}></option>", string.IsNullOrEmpty(field.Value) ? " selected" : "");

            selectionInList = false;

            foreach (var item in fieldChoices)
            {
                selectionInList |= item.ID.ToString() == field.Value;

                dropdownOptions.AppendFormat("<option value='{0}'{2}>{1}</option>", item.ID, item.DisplayName, item.ID.ToString() == field.Value ? " selected" : "");
            }

            if (!selectionInList)
            {
                dropdownOptions.AppendFormat(@"<OPTGROUP label=""Value not in the selection list.""><OPTION selected value={0}>{0}</OPTION></OPTGROUP>", field.Value);
            }

            return dropdownOptions.ToString();
        }

        private Item[] GetSources(Field field)
        {
            using (new LanguageSwitcher(CurrentLanguage))
            {
                return LookupSources.GetItems(CurrentItem, field.Source);
            }
        }

        #region IContentField
        public string GetValue()
        {
            if (IsContentItem())
            {
                var page = Sitecore.Context.ClientPage;
                var isValidation = page.ClientRequest.Parameters != "contenteditor:save" && page.ClientRequest.Parameters != "contenteditor:saveandclose" && page.ClientRequest.Parameters != "item:save";

                //If we are saving, then save the actual calculated value

                var ids = new StringBuilder();

                foreach (var i in GetSubitems())
                {
                    if (ids.Length != 0)
                    {
                        ids.Append("|");
                    }

                    ids.Append(i.ID);
                }

                if (isValidation)
                {
                    ids.Append("|");
                    ids.Append(System.DateTime.Now.Ticks);
                }

                Value = ids.ToString();

                return ids.ToString();
            }
            else
            {
                Value = "";

                return "";
            }
        }

        public void SetValue(string value)
        {
        }
        #endregion

        #region IMessageHandler
        public override void HandleMessage(Message message)
        {
            if (message.Name == "item:save")
            {
                SheerResponse.Eval("ExpressSubitem_SaveField('" + ID + "');");
            }
        }
        #endregion

        public Item newItem { get; set; }
    }
}