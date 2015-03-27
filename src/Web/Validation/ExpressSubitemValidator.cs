using Newtonsoft.Json;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Web.UI;

namespace OneNorth.ExpressSubitem.Validation
{
    [Serializable]
    public class ExpressSubitemValidator : StandardValidator
    {
        #region Validation Results
        class InvalidField
        {
            public string FieldId { get; set; }
            public string Result { get; set; }
        }

        class InvalidExpressSubitem
        {
            public string ItemId { get; set; }
            public InvalidField[] Fields { get; set; }
            public string Result { get; set; }
        }
        #endregion

        public ExpressSubitemValidator()
        {
        }

        public ExpressSubitemValidator(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        protected override ValidatorResult Evaluate()
        {
            string[] itemIdsToValidate = ControlValidationValue.Split(new [] {"|"}, StringSplitOptions.RemoveEmptyEntries);

            if (itemIdsToValidate.Any())
            {
                Item ownerItem = GetItem();

                //Holds results of the validation
                var result = ValidatorResult.Valid;
                var validationErrors = new StringBuilder();
                var invalidItems = new List<InvalidExpressSubitem>();

                foreach (var itemIdToValidate in itemIdsToValidate)
                {
                    Guid itemId;

                    //Filter out bogus item ID's. This is needed because we may put a bogus value on the end of the updated value to indicate a change in the child
                    if (Guid.TryParse(itemIdToValidate, out itemId))
                    {
                        var itemResult = ValidatorResult.Valid;

                        //Find the item to validate
                        var itemToValidate = ownerItem.Database.GetItem(new ID(itemId), ownerItem.Language);

                        if (itemIdsToValidate == null)
                        {
                            continue;
                        }

                        //Build the list of Validators
                        var validators = ValidatorManager.BuildValidators(ValidatorsMode.ValidatorBar, itemToValidate);
                        ValidatorManager.Validate(validators, new ValidatorOptions(true));

                        var invalidFields = new List<InvalidField>();

                        foreach (BaseValidator validator in validators)
                        {
                            if (validator.FieldID != (ID)null)
                            {
                                if (!validator.IsValid)
                                {
                                    if (validator.Result > itemResult)
                                    {
                                        itemResult = validator.Result;
                                    }

                                    if (validator.Result > result)
                                    {
                                        result = validator.Result;
                                    }

                                    validationErrors.AppendFormat("{0}: {1} ", itemToValidate.Name, validator.Text);

                                    //Collect invalid fields
                                    invalidFields.Add(new InvalidField
                                    {
                                        FieldId = validator.FieldID.Guid.ToString("B").ToUpper(),
                                        Result = validator.Result.ToString()
                                    });
                                }
                            }
                        }

                        if (invalidFields.Any())
                        {
                            invalidItems.Add(new InvalidExpressSubitem
                            {
                                ItemId = itemToValidate.ID.Guid.ToString("N"),
                                Result = itemResult.ToString(),
                                Fields = invalidFields.ToArray()
                            });
                        }
                    }
                }

                Text = result != ValidatorResult.Valid ? validationErrors.ToString() : "Valid.";

                var jsonValidationInfo = JsonConvert.SerializeObject(invalidItems);

                if (Sitecore.Context.ClientPage.IsEvent)
                {
                    SheerResponse.Eval(string.Format("ExpressSubitem_UpdateValidation('{0}', {1});", ControlToValidate, jsonValidationInfo));
                }
                else
                {
                    Sitecore.Context.ClientPage.FindControl("ContentEditorForm").Controls.Add(
                        new LiteralControl(
                            string.Format("<script type=\"text/javascript\" language=\"javascript\">window.setTimeout('ExpressSubitem_UpdateValidation(\"{0}\", {1});', {2})</script>",
                                ControlToValidate,
                                jsonValidationInfo,
                                Settings.Validators.UpdateFrequency)));
                }

                return GetFailedResult(result);
            }

            return ValidatorResult.Valid;
        }

        protected override ValidatorResult GetMaxValidatorResult()
        {
            return ValidatorResult.CriticalError;
        }

        public override string Name
        {
            get
            {
                return "Express Subitem Validator";
            }
        }
    }
}