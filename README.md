# CDKey-CodeReward
A Unturned Rocket Plugin about CDKey. Support MySQL

# Permssion
**Command** | **Permission**
---|---
`cdk` | `cdk`

# Database editing

The database editing section describes the format of the data fields in the database.

## Items and Amount fields

The Items and Amount fields are used to store a list of items and their corresponding quantities.

**Items field**

When the Amount field is empty, the Items field must be a comma-separated list of ushort values, where each value represents an item id.

**Examples**

* When the Items field is `1,2,3` and the Amount field is ``, then the database stores a list of three items, each with a quantity of 1.
* When the Items field is `1,2,3` and the Amount field is `1,2,3`, then the database stores a list of three items, where the first item has a quantity of 1, the second item has a quantity of 2, and the third item has a quantity of 3.

**Notes**

* The Items field is required.
* The Amount field is optional.
* When the Amount field is empty, the quantity for each item is assumed to be 1.

**Additional notes:**

* It is recommended to write both the Items and Amount fields together. This will make it easier to read and understand the data.


# Permission group

This document describes the format of the permission group data structure.


The following fields are used to represent a permission group:

* **GrantPermissionGroup:** The ID of the permission group.
* **ValidUntil:** The expiration date of the permission group.
* **EnableRenew:** Whether the permission group can be renewed.
* **UsePermissionSync:** Whether the permission group management is delegated to another plugin.

## Explanation

* **GrantPermissionGroup:** The permission group ID is a unique string that identifies a permission group.
* **ValidUntil:** The expiration date of a permission group is the date and time after which the permission group will be automatically revoked.
* **EnableRenew:** When set to `1`, the permission group can be renewed. When set to `0`, the permission group cannot be renewed.
* **UsePermissionSync:** When set to `1`, the permission group management is delegated to another plugin. When set to `0`, the permission group management is handled by this plugin.

## Notes

* **GrantPermissionGroup:** This is the ID of the permission group, **not** the display name.
* **UsePermissionSync:** When set to `1`, the permission group management is delegated to another plugin. When set to `0`, the permission group management is handled by this plugin.
* When `EnableRenew` is set to `1`, the `ValidUntil` field must also be set to a valid date and time.

## CDK-related fields

The following fields are used to represent CDK-related information about a key:

* **CDKKey:** The CDK key used to redeem the permission group. This is the primary key.
* **MaxRedeem:** The maximum number of times that this key can be redeemed.
* **RedeemedTimes:** The number of times that this key has been redeemed.
* **Owner:** The Steam digital ID of the owner of the key, if any.
