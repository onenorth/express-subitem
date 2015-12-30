function ExpressSubitem_toggleSubItem(event) {
    var subItem = $sc(scForm.browser.getSrcElement(event)).parents(".sub-item");
    var control = subItem.parent();

    if (subItem.hasClass("disabled")) {
        return;
    }

    if (subItem.hasClass("expanded")) {
        subItem.removeClass("expanded");
        subItem.find(".expander").attr("title", "Expand");

        ExpressSubitem_SaveSubitem(subItem);

        scContent.startValidators();
    }
    else {
        subItem.addClass("expanded");
        subItem.find(".expander").attr("title", "Collapse");

        if (!subItem.hasClass("downloaded")) {
            scForm.postEvent(this, event, control.attr('controlID') + '.LoadContent("' + subItem.attr("itemid") + '")');

            subItem.addClass("downloaded");

            if (!control.attr("disabled")) {
                ExpressSubitem_HookupSubitemChange(subItem);
                ExpressSubitem_RefreshValidations(control);
            }
        }
    }
}

function ExpressSubitem_doubleClick(event) {
    ExpressSubitem_toggleSubItem(event);

    event.cancelBubble = true;
    return false;
}

function ExpressSubitem_SaveField(fieldControlId) {
    var fieldToSave = $sc("#" + fieldControlId + "_Wrapper");
    var expandedItems = fieldToSave.find(".expanded");

    expandedItems.each(function (index) {
        ExpressSubitem_SaveSubitem($sc(this));
    });
}

function ExpressSubitem_SaveSubitem(subItem) {
    var control = subItem.parent();

    if (control.attr("disabled")) {
        return;
    }

    var itemDetails = {
        itemId: subItem.attr("itemid"),
        language: control.attr("language"),
        version: subItem.attr("version"),
        nameFormatString: control.attr("nameFormatString"),
        fields: ExpressSubitem_getFieldValues(subItem)
    };

    $sc.ajax({
        url: "/sitecore/Shell/OneNorth/Service/SaveExpressSubitem.ashx",
        type: "post",
        data: JSON.stringify(itemDetails),
        dataType: "json",
        async: false,
        success: function (data) {
            subItem.find(".title").text(data.displayName);
        },
        error: function (jqXHR, textStatus) {
            alert("Save Failed. Reason: " + jqXHR.responseText);
        }
    });
}

function ExpressSubitem_getFieldValues(subItem) {
    var fields = new Array();
    var control = subItem.parent();

    subItem.find(".dropdown select, .textbox input").each(function () {
        var ele = $sc(this);

        //This "name" of the input is really the fieldId
        fields.push({ fieldId: ele.attr("name"), value: ele.val() });
    });

    subItem.find(".datetime").each(function () {
        var ele = $sc(this);
        var values = ele.find("input.scComboboxEdit");

        var value = "";

        values.each(function () {
            if (value != "") {
                value += " " + $sc(this).val();
            }
            else {
                value += $sc(this).val();
            }
        });

        fields.push({ fieldId: ele.attr("fieldId"), value: value });
    });

    subItem.find(".tree").each(function () {
        var ele = $sc(this);
        var valueHolder = ele.find("input.scComboboxEdit");

        var value = ele.attr("sourceId") + "|" + valueHolder.val();

        fields.push({ fieldId: ele.attr("fieldId"), value: value });
    });

    subItem.find(".file, .image, .multilist").each(function () {
        var ele = $sc(this);
        var valueHolder = ele.find("input.scContentControl, input.scContentControlImage, input[type='hidden']");

        fields.push({ fieldId: ele.attr("fieldId"), value: valueHolder.val() });
    });

    subItem.find(".treelist").each(function () {
        var ele = $sc(this);
        var selectList = ele.find("select.scContentControlMultilistBox");

        var selectedItems = "";

        selectList.find("option").each(function () {
            var option = $sc(this);
            var value = option.val();

            value = value.substring(value.indexOf("|") + 1);

            if (selectedItems.length != 0) {
                selectedItems += "|";
            }

            selectedItems += value;
        });

        fields.push({ fieldId: ele.attr("fieldId"), value: selectedItems });
    });

    subItem.find(".richtext").each(function () {
        var ele = $sc(this);
        var field = $sc("#" + ele.attr("rtControlId"));
        var val = field[0].contentWindow.scGetFrameValue(null, null);

        fields.push({ fieldId: ele.attr("fieldId"), value: val });
    });

    subItem.find(".checkbox input").each(function () {
        var ele = $sc(this);

        //This "name" of the input is really the fieldId
        fields.push({ fieldId: ele.attr("name"), value: ((ele[0].checked) ? ele.val() : "") });
    });

    return fields;
}

function ExpressSubitem_AddNewItem(event, ownerId) {
    ExpressSubitem_SubitemChange(false);

    var addItemElement = $sc(scForm.browser.getSrcElement(event)).parents(".add-new-item");
    var newItemRow = scForm.postEvent(this, event, ownerId + '.AddNewItem()');

    addItemElement.before(newItemRow);

    event.cancelBubble = true;

    var owner = addItemElement.parent(".express-subitem");
    ExpressSubitem_HookupSubitemChange(owner.find(".sub-item").last());

    return false;
}

function ExpressSubitem_deleteSubItem(event) {
    if (confirm("Are you sure you want to delete this item?")) {
        ExpressSubitem_SubitemChange(false);

        var subItem = $sc(scForm.browser.getSrcElement(event)).parents(".sub-item");
        var control = subItem.parent();

        var itemId = subItem.attr("itemid");

        subItem.remove();

        scForm.postEvent(this, event, control.attr('controlID') + '.DeleteSubItem("' + itemId + '")');

        scContent.startValidators();
    }
}

function ExpressSubitem_resetSubItem(event) {
    var conf = confirm("Warning: Clicking OK will open the reset screen for resetting fields to their standard language fallback values.\nContinuing to the reset screen will disable any manual changes to the fields in this section until this item is reopened.");
    if (conf) {
        ExpressSubitem_SubitemChange(false);

        var subItem = $sc(scForm.browser.getSrcElement(event)).parents(".sub-item");
        var control = subItem.parent();

        var itemId = subItem.attr("itemid");

        subItem.removeClass("expanded");
        subItem.addClass("disabled");

        scForm.postEvent(this, event, control.attr('controlID') + '.ResetSubItem("' + itemId + '")');

        scContent.startValidators();
    }
}

function ExpressSubitem_moveUp(event) {
    ExpressSubitem_SubitemChange(false);

    var subItem = $sc(scForm.browser.getSrcElement(event)).parents(".sub-item");
    var control = subItem.parent();
    var itemId = subItem.attr("itemid");

    var previousSubItem = subItem.prev(".sub-item");
    if (previousSubItem.length != 0) {
        subItem.insertBefore(previousSubItem);

        scForm.postEvent(this, event, control.attr('controlID') + '.MoveUpSubItem("' + itemId + '")');
    }
}

function ExpressSubitem_moveDown(event) {
    ExpressSubitem_SubitemChange(false);

    var subItem = $sc(scForm.browser.getSrcElement(event)).parents(".sub-item");
    var control = subItem.parent();
    var itemId = subItem.attr("itemid");

    var nextSubItem = subItem.next(".sub-item");
    if (nextSubItem.length != 0) {
        subItem.insertAfter(nextSubItem);

        scForm.postEvent(this, event, control.attr('controlID') + '.MoveDownSubItem("' + itemId + '")');
    }
}

function ExpressSubitem_UpdateValidation(controlId, invalidItems) {
    var control = $sc("div.express-subitem[controlID=\"" + controlId + "\"]");

    if (control.length == 0) {
        return;
    }

    control[0].invalidItems = invalidItems;

    ExpressSubitem_RefreshValidations(control);
}

function ExpressSubitem_RefreshValidations(control) {
    if (control.length == 0) {
        return;
    }

    control.find(".red-sub-item-validation, .yellow-sub-item-validation").each(function () {
        var ele = $sc(this);

        ele.removeClass("red-sub-item-validation");
        ele.removeClass("yellow-sub-item-validation");
    });

    control.find(".scEditorFieldMarkerBarCellRed, .scEditorFieldMarkerBarCellYellow").each(function () {
        var fieldMarker = $sc(this);

        fieldMarker.removeClass("scEditorFieldMarkerBarCellRed");
        fieldMarker.removeClass("scEditorFieldMarkerBarCellYellow");
        fieldMarker.addClass("scEditorFieldMarkerBarCell");
    });

    var currentInvalidItems = control[0].invalidItems;

    if (currentInvalidItems == undefined) {
        return;
    }

    currentInvalidItems.each(function (item) {
        var subItem = $sc("#ExpressSubitem_" + item.ItemId);

        if (item.Result == "Warning") {
            subItem.find(".sub-item-validation").addClass("yellow-sub-item-validation");
        }
        else {
            subItem.find(".sub-item-validation").addClass("red-sub-item-validation");
        }

        item.Fields.each(function (field) {
            var fieldMarker = subItem.find('.field-container[fieldId="' + field.FieldId + '"] .field-marker');

            fieldMarker.removeClass("scEditorFieldMarkerBarCell");

            if (field.Result == "Warning") {
                fieldMarker.addClass("scEditorFieldMarkerBarCellYellow");
            }
            else {
                fieldMarker.addClass("scEditorFieldMarkerBarCellRed");
            }
        });
    });
}

function ExpressSubitem_HookupSubitemChange(subItem) {
    subItem.find("input, select, .scContentButton").change(function () {
        ExpressSubitem_SubitemChange(true);
    });
}

function ExpressSubitem_SubitemChange(doSave) {
    scForm.setModified(true);

    if (doSave) {
        var subItem = $sc(scForm.browser.getSrcElement(event)).parents(".sub-item");

        ExpressSubitem_SaveSubitem(subItem);
        scContent.startValidators();
    }
}