# CDKey-CodeReward
A Unturned Rocket Plugin about CDKey. Support MySQL

# Permssion
Command:CDK Permission CDK

# Database editor
Give Items<br>
Items id,id,id ----one by one<br>
Amount number,number,number ---one by one<br> 
<br>
Example: <br>
method 1 Items:253,253,253 => item 253x3<br>

method 2 Items:263. Amount:3 => item 253x3

Give Permission<br>

GrantPermissionGroup:vip<br> 
ValidUntil:use Navicat to modify it.It determines the validity period of the permission group
tip1: Permission Group ID,NOT DisplayName
tip2:change "UsePermissionSync" to 1,
it can let my another plugin "PermissionSync" to manager permission to give<br>

change EnableRenew to 1.it can Enables CDK to be redeemed twice to extend the validity period of the permission group<br>
ValidUntil needs to be modified synchronously to decide the extension time
