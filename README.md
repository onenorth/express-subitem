
# Sitecore: Express Subitem Module Readme

## Overview

The Express Subitem Module allows content editors to edit multiple child Sitecore items at the same time within the context of the parent item. This field is commonly used to manage lists of related items that exist only within the context of the parent.  A few examples are as follows:

 1. Addresses related to a Person
 2. Education History related to a Person
 3. Employment History related to a Person
 3. Schedule related to an Event

This field improves the content editing experience because the content administrator does not need to navigate to the child items to edit related information.

### Express Subitem - Collapsed
![Express Subitem - Collapsed](https://raw.github.com/onenorth/express-subitem/master/img/ExpressSubitemCollapsed.jpg)

### Express Subitem - Expanded
![Express Subitem - Expanded](https://raw.github.com/onenorth/express-subitem/master/img/ExpressSubitemExpanded.jpg)

### Supported Fields

The following field types are currently supported by the Express Subitem module:

 - Droptree
 - File
 - Image
 - Multilist
 - Treelist
 - Datetime
 - Date
 - Rich Text
 - Single-Line Text
 - Multi-Line Text
 - Integer
 - Droplink

Additional field types can be added by modifying the source code.

> Note: This module supports Sitecore 7.0, 7.1, 7.2, 7.5.  It has not been tested with Sitecore 8.0

Please see the related [blog post](http://www.onenorth.com/blog/post/sitecore-express-subitem-module) for further information.

## Installation

Install the update packages located here: https://github.com/OneNorth/express-subitem/tree/master/release

## Configuration

### Sitecore Items

Below, you will find a list of the Sitecore items used to configure the Express Subitem Module.

 - *Core* Items
	 - /sitecore/system/Field types/List Types/Express Subitem
 - *Master* Items
	 - /sitecore/system/Settings/Validation Rules/Field Types/Express Subitem
	 - /sitecore/system/Settings/Validation Rules/Field Rules/External/Express Subitem Validation

### Configuration Files

The Express Subitem Module requires a special change to the Sitecore configuration files. Normally, all configuration updates are done in files located in **/app_config/include**. The Express Subitem Module requires a change to the **/app_config/FieldTypes.config** file.

A new field type needs to be added to **/app_config/FieldTypes.config**. This field type is named **Express Subitem** and should be added to the **< configuration>** element of the **FieldTypes.Config** file.

    <!-- Custom Types-->
    <fieldType name="Express Subitem" type="Sitecore.Data.Fields.MultilistField,Sitecore.Kernel" />

There are also some configuration changes needed to be made in the **/app_config/include** folder.

    <configuration xmlns:patch="http://www.sitecore.net/xmlconfig/">
      <sitecore>
        <pipelines>
          <renderContentEditor>
            <processor type="OneNorth.ExpressSubitem.Pipelines.ExpressSubitemCustomizationsPipeline, OneNorth.ExpressSubitem"
                   patch:before="*[1]" />
          </renderContentEditor>
        </pipelines>
        <events>
          <event name="item:saved">
            <handler type="OneNorth.ExpressSubitem.Events.ExpressSubitemEvents, OneNorth.ExpressSubitem" 
                 method="OnItemSaved"/>
          </event>
        </events>
      </sitecore>
    </configuration>

### Field Configuration

To create an Express Subitem field in a template, you must set the field type column to "Express Subitem" in the template builder. 

The Source entry contains two parameters, **template** and **name**. These parameters are entered much like a http query string. An example of a source parameter for an express subitem field looks like:

    template=/sitecore/templates/Legal/Content/Entities/Professionals/Education&name={School}, {Year}

The **template** is the path of the sub-item template. This template must exist.

The **name** specifies the format of the created sub-items name. The values in braces '{}' are field names on the sub-item and are used to determine the name. If the value of one of these field changes, the name of the sub-item will be updated.

![Express Subitem - Template](https://raw.github.com/onenorth/express-subitem/master/img/ExpressSubitemTemplate.jpg)

#License

The associated code is released under the terms of the [MIT license](http://onenorth.mit-license.org).


